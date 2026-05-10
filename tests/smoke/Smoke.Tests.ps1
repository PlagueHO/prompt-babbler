param(
    [Parameter(Mandatory)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory)]
    [string]$McpServerBaseUrl,

    [Parameter(Mandatory)]
    [string]$FrontendBaseUrl,

    [Parameter()]
    [string]$CustomFrontendBaseUrl = '',

    [Parameter()]
    [string]$AccessCode = ''
)

function Get-SmokeHeaders {
    param([string]$Code)

    $headers = @{}
    if ($Code) {
        $headers['X-Access-Code'] = $Code
    }

    return $headers
}

Describe 'Backend API' {
    BeforeAll {
        # Phase 1: Wait for /health endpoint (basic ASP.NET health check).
        # ACA cold-start from zero replicas + GHCR image pull can take 2+ minutes.
        # ACA ingress propagation can also return 403 for up to ~3 minutes after provisioning.
        $healthRetries = 15
        $retryDelay = 10
        $healthy = $false

        for ($i = 1; $i -le $healthRetries; $i++) {
            $timestamp = Get-Date -Format 'HH:mm:ss'
            try {
                $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
                if ($response.StatusCode -eq 200) {
                    $healthy = $true
                    Write-Host "[$timestamp] Phase 1: /health is ready (attempt $i/$healthRetries) - Body: $($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))"
                    break
                }
                else {
                    Write-Host "[$timestamp] Phase 1 attempt $i/${healthRetries}: /health returned status $($response.StatusCode), retrying in ${retryDelay}s..."
                    Start-Sleep -Seconds $retryDelay
                }
            }
            catch {
                $statusCode = $null
                $errorDetail = $_.Exception.Message
                if ($_.Exception.Response) {
                    $statusCode = [int]$_.Exception.Response.StatusCode
                    $errorDetail = "HTTP $statusCode - $($_.Exception.Message)"
                }
                Write-Host "[$timestamp] Phase 1 attempt $i/${healthRetries}: /health not ready ($errorDetail), retrying in ${retryDelay}s..."
                Start-Sleep -Seconds $retryDelay
            }
        }

        if (-not $healthy) {
            # Capture final state for diagnostics
            $finalError = "Unknown"
            try {
                $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
                $finalError = "Status $($response.StatusCode): $($response.Content)"
            }
            catch {
                $finalError = $_.Exception.Message
                if ($_.Exception.Response) {
                    $finalError = "HTTP $([int]$_.Exception.Response.StatusCode): $($_.Exception.Message)"
                    try {
                        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
                        $body = $reader.ReadToEnd()
                        $reader.Close()
                        if ($body) { $finalError += " | Body: $body" }
                    } catch {}
                }
            }
            throw "Backend /health did not become healthy after $healthRetries attempts. Last error: $finalError"
        }

        # Phase 2: Wait for /health to report all dependency checks as Healthy.
        # The detailed health response includes per-check status for Cosmos DB, managed identity, and AI Foundry.
        $statusRetries = 20
        $statusDelay = 10
        $statusHealthy = $false

        for ($i = 1; $i -le $statusRetries; $i++) {
            $timestamp = Get-Date -Format 'HH:mm:ss'
            try {
                # Use -SkipHttpErrorCheck so we can read the response body even on 503
                $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 30 -SkipHttpErrorCheck
                $statusJson = $response.Content | ConvertFrom-Json

                Write-Host "[$timestamp] Health check (attempt $i/$statusRetries) - HTTP $($response.StatusCode):"
                Write-Host "  Overall:          $($statusJson.status)"

                # Log individual check statuses when available
                if ($statusJson.entries) {
                    $statusJson.entries.PSObject.Properties | ForEach-Object {
                        Write-Host "  $($_.Name): $($_.Value.status) - $($_.Value.description)"
                    }
                }

                if ($statusJson.status -eq 'Healthy') {
                    $statusHealthy = $true
                    break
                }

                Start-Sleep -Seconds $statusDelay
            }
            catch {
                Write-Host "[$timestamp] Attempt $i/${statusRetries}: /health request failed - $($_.Exception.Message)"
                Start-Sleep -Seconds $statusDelay
            }
        }

        if (-not $statusHealthy) {
            Write-Host "WARNING: Dependencies not fully healthy after $statusRetries attempts. Individual tests may fail with more specific errors."
        }
    }

    It 'Health endpoint returns Healthy' {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200

        $status = $response.Content | ConvertFrom-Json
        $status.status | Should -Be 'Healthy'
    }

    It 'Liveness endpoint returns 200' {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/alive" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
    }

    It 'Health endpoint returns detailed dependency status' {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 30
        $response.StatusCode | Should -Be 200

        $status = $response.Content | ConvertFrom-Json
        $status.status | Should -Be 'Healthy'
        $status.entries.'cosmosdb'.status | Should -Be 'Healthy'
        $status.entries.'managed-identity'.status | Should -Be 'Healthy'
    }

    It 'Babbles API returns 200' {
        $headers = Get-SmokeHeaders -Code $AccessCode
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/babbles" -Headers $headers -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
    }

    It 'Templates API returns 200' {
        $headers = Get-SmokeHeaders -Code $AccessCode
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/templates?pageSize=1" -Headers $headers -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200

        $payload = $response.Content | ConvertFrom-Json
        $payload.PSObject.Properties.Name | Should -Contain 'items'
    }

    It 'Transcription WebSocket starts without session error' {
        $query = if ($AccessCode) {
            '?access_code=' + [Uri]::EscapeDataString($AccessCode)
        }
        else {
            ''
        }
        $wsUri = [Uri]::new("$($ApiBaseUrl.Replace('https://', 'wss://'))/api/transcribe/stream$query")

        $socket = [System.Net.WebSockets.ClientWebSocket]::new()
        $timeoutCts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(30))

        try {
            $socket.ConnectAsync($wsUri, $timeoutCts.Token).GetAwaiter().GetResult()
            $socket.State | Should -Be ([System.Net.WebSockets.WebSocketState]::Open)

            $silentFrame = [byte[]](0..319 | ForEach-Object { 0 })
            $sendSegment = [ArraySegment[byte]]::new($silentFrame)
            $socket.SendAsync($sendSegment, [System.Net.WebSockets.WebSocketMessageType]::Binary, $true, $timeoutCts.Token).GetAwaiter().GetResult()

            $receiveBuffer = New-Object byte[] 4096
            $receiveSegment = [ArraySegment[byte]]::new($receiveBuffer)
            $receiveTask = $socket.ReceiveAsync($receiveSegment, $timeoutCts.Token)
            $completedTask = [System.Threading.Tasks.Task]::WhenAny($receiveTask, [System.Threading.Tasks.Task]::Delay(12000)).GetAwaiter().GetResult()

            if ($completedTask -eq $receiveTask) {
                $result = $receiveTask.GetAwaiter().GetResult()

                if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Text) {
                    $text = [System.Text.Encoding]::UTF8.GetString($receiveBuffer, 0, $result.Count)
                    $parsed = $text | ConvertFrom-Json -ErrorAction SilentlyContinue
                    if ($null -ne $parsed -and $parsed.PSObject.Properties.Name -contains 'error') {
                        throw "Transcription WebSocket returned startup error: $($parsed.error)"
                    }
                }
                elseif ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
                    throw "Transcription WebSocket closed during startup. CloseStatus=$($socket.CloseStatus) Description=$($socket.CloseStatusDescription)"
                }
            }

            # If no startup message arrives within the observation window and socket stays open,
            # treat that as healthy startup waiting for additional audio.
            $socket.State | Should -Be ([System.Net.WebSockets.WebSocketState]::Open)
        }
        finally {
            if ($socket.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
                $socket.CloseAsync(
                    [System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure,
                    'Smoke test completed',
                    [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
            }

            $socket.Dispose()
            $timeoutCts.Dispose()
        }
    }
}

Describe 'Frontend SPA' {
    It 'Returns 200 and contains root div' {
        $response = Invoke-WebRequest -Uri $FrontendBaseUrl -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
        $response.Content | Should -Match '<div id="root"'
    }

    It 'Serves JavaScript assets' {
        $html = (Invoke-WebRequest -Uri $FrontendBaseUrl -UseBasicParsing -TimeoutSec 10).Content
        $assetMatch = [regex]::Match($html, 'src="(/assets/[^"]+\.js)"')

        if (-not $assetMatch.Success) {
            Set-ItResult -Inconclusive -Because 'No JS asset reference found in HTML'
            return
        }

        $assetUrl = "$FrontendBaseUrl$($assetMatch.Groups[1].Value)"
        $response = Invoke-WebRequest -Uri $assetUrl -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
    }

    It 'Custom domain serves frontend when configured' {
        if (-not $CustomFrontendBaseUrl) {
            Set-ItResult -Inconclusive -Because 'Custom frontend domain is not configured for this environment'
            return
        }

        $response = Invoke-WebRequest -Uri $CustomFrontendBaseUrl -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
        $response.Content | Should -Match '<div id="root"'
    }
}

Describe 'MCP Server' {
    BeforeAll {
        # Wait for /health endpoint — same cold-start considerations as the API.
        $healthRetries = 15
        $retryDelay = 10
        $healthy = $false

        for ($i = 1; $i -le $healthRetries; $i++) {
            $timestamp = Get-Date -Format 'HH:mm:ss'
            try {
                $response = Invoke-WebRequest -Uri "$McpServerBaseUrl/health" -UseBasicParsing -TimeoutSec 10
                if ($response.StatusCode -eq 200) {
                    $healthy = $true
                    Write-Host "[$timestamp] MCP Server /health is ready (attempt $i/$healthRetries)"
                    break
                }
                else {
                    Write-Host "[$timestamp] MCP Server Phase 1 attempt $i/${healthRetries}: /health returned status $($response.StatusCode), retrying in ${retryDelay}s..."
                    Start-Sleep -Seconds $retryDelay
                }
            }
            catch {
                $errorDetail = $_.Exception.Message
                if ($_.Exception.Response) {
                    $errorDetail = "HTTP $([int]$_.Exception.Response.StatusCode) - $($_.Exception.Message)"
                }
                Write-Host "[$timestamp] MCP Server Phase 1 attempt $i/${healthRetries}: /health not ready ($errorDetail), retrying in ${retryDelay}s..."
                Start-Sleep -Seconds $retryDelay
            }
        }

        if (-not $healthy) {
            $finalError = "Unknown"
            try {
                $response = Invoke-WebRequest -Uri "$McpServerBaseUrl/health" -UseBasicParsing -TimeoutSec 10
                $finalError = "Status $($response.StatusCode): $($response.Content)"
            }
            catch {
                $finalError = $_.Exception.Message
                if ($_.Exception.Response) {
                    $finalError = "HTTP $([int]$_.Exception.Response.StatusCode): $($_.Exception.Message)"
                }
            }
            throw "MCP Server /health did not become healthy after $healthRetries attempts. Last error: $finalError"
        }
    }

    It 'Health endpoint returns Healthy' {
        $response = Invoke-WebRequest -Uri "$McpServerBaseUrl/health" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200

        $status = $response.Content | ConvertFrom-Json
        $status.status | Should -Be 'Healthy'
    }

    It 'Liveness endpoint returns 200' {
        $response = Invoke-WebRequest -Uri "$McpServerBaseUrl/alive" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
    }

    It 'Health endpoint reports prompt-babbler-api dependency as Healthy' {
        $response = Invoke-WebRequest -Uri "$McpServerBaseUrl/health" -UseBasicParsing -TimeoutSec 30
        $response.StatusCode | Should -Be 200

        $status = $response.Content | ConvertFrom-Json
        $status.status | Should -Be 'Healthy'
        $status.entries.'prompt-babbler-api'.status | Should -Be 'Healthy'
    }
}
