param(
    [Parameter(Mandatory)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory)]
    [string]$FrontendBaseUrl
)

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
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/babbles" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
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
}
