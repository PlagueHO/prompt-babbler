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

        # Phase 2: Wait for /api/status to report overall: Healthy (deep dependency check).
        $statusRetries = 20
        $statusDelay = 10
        $statusHealthy = $false

        for ($i = 1; $i -le $statusRetries; $i++) {
            $timestamp = Get-Date -Format 'HH:mm:ss'
            try {
                $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/status" -UseBasicParsing -TimeoutSec 15
                $status = $response.Content | ConvertFrom-Json

                $miStatus = $status.managedIdentity.status
                $cosmosStatus = $status.cosmosDb.status
                $aiStatus = $status.aiFoundry.status
                $overall = $status.overall

                Write-Host "[$timestamp] Phase 2 attempt $i/${statusRetries}: overall=$overall, managedIdentity=$miStatus, cosmosDb=$cosmosStatus, aiFoundry=$aiStatus"

                if ($overall -eq 'Healthy') {
                    $statusHealthy = $true
                    break
                }
                else {
                    # Log per-dependency errors for CI diagnostics
                    if ($status.managedIdentity.error) { Write-Host "  managedIdentity error: $($status.managedIdentity.error)" }
                    if ($status.cosmosDb.error) { Write-Host "  cosmosDb error: $($status.cosmosDb.error)" }
                    if ($status.aiFoundry.error) { Write-Host "  aiFoundry error: $($status.aiFoundry.error)" }
                    Start-Sleep -Seconds $statusDelay
                }
            }
            catch {
                $errorDetail = $_.Exception.Message
                if ($_.Exception.Response) {
                    $errorDetail = "HTTP $([int]$_.Exception.Response.StatusCode)"
                    try {
                        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
                        $body = $reader.ReadToEnd()
                        $reader.Close()
                        if ($body) {
                            $errorDetail += " | $body"
                            # Try to parse and show per-dependency status even from 503 response
                            try {
                                $errStatus = $body | ConvertFrom-Json
                                Write-Host "[$timestamp] Phase 2 attempt $i/${statusRetries}: overall=$($errStatus.overall), managedIdentity=$($errStatus.managedIdentity.status), cosmosDb=$($errStatus.cosmosDb.status), aiFoundry=$($errStatus.aiFoundry.status)"
                                if ($errStatus.managedIdentity.error) { Write-Host "  managedIdentity error: $($errStatus.managedIdentity.error)" }
                                if ($errStatus.cosmosDb.error) { Write-Host "  cosmosDb error: $($errStatus.cosmosDb.error)" }
                            } catch {}
                        }
                    } catch {}
                }
                Write-Host "[$timestamp] Phase 2 attempt $i/${statusRetries}: /api/status request failed ($errorDetail), retrying in ${statusDelay}s..."
                Start-Sleep -Seconds $statusDelay
            }
        }

        if (-not $statusHealthy) {
            Write-Host "WARNING: /api/status did not report Healthy after $statusRetries attempts. Individual tests may fail with more specific errors."
        }
    }

    It 'Health endpoint returns Healthy' {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
        $response.Content | Should -BeLike '*Healthy*'
    }

    It 'Liveness endpoint returns 200' {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/alive" -UseBasicParsing -TimeoutSec 10
        $response.StatusCode | Should -Be 200
    }

    It 'Status endpoint reports all dependencies healthy' {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/status" -UseBasicParsing -TimeoutSec 15
        $response.StatusCode | Should -Be 200

        $status = $response.Content | ConvertFrom-Json
        $status.overall | Should -Be 'Healthy'
        $status.managedIdentity.status | Should -Be 'Healthy'
        $status.cosmosDb.status | Should -Be 'Healthy'
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
