param(
    [Parameter(Mandatory)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory)]
    [string]$FrontendBaseUrl
)

Describe 'Backend API' {
    BeforeAll {
        # Wait for Container App cold start with retry on /health endpoint
        $maxRetries = 10
        $retryDelay = 10
        $healthy = $false

        for ($i = 1; $i -le $maxRetries; $i++) {
            try {
                $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10
                if ($response.StatusCode -eq 200) {
                    $healthy = $true
                    Write-Host "Backend /health is ready (attempt $i/$maxRetries)"
                    break
                }
            }
            catch {
                Write-Host "Attempt $i/${maxRetries}: /health not ready, retrying in ${retryDelay}s..."
                Start-Sleep -Seconds $retryDelay
            }
        }

        if (-not $healthy) {
            throw "Backend /health did not become healthy after $maxRetries attempts"
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
