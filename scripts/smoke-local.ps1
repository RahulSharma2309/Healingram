# Local V1 smoke: gateway :5000 if up, otherwise API :5080.
# Exit non-zero if any step fails. Run from the repo root after API (and optionally gateway) are up.

$ErrorActionPreference = "Stop"

function Test-SmokeBase([string]$Base) {
    try {
        & curl.exe -sS -f --max-time 3 "$Base/api/health" | Out-Null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

function Invoke-SmokeGet([string]$Path, [string]$Token = "") {
    Write-Host "GET  $Path"
    $headers = @(
        "-sS", "-f", "--max-time", "20",
        "-H", "Accept: application/json"
    )
    if ($Token) {
        $headers += @("-H", "Authorization: Bearer $Token")
    }
    & curl.exe @headers "$script:BaseUrl$Path"
    if ($LASTEXITCODE -ne 0) {
        throw "GET $Path failed (exit $LASTEXITCODE)"
    }
    Write-Host ""
}

function Invoke-SmokePost([string]$Path, [string]$Body, [string]$Token = "") {
    Write-Host "POST $Path"
    $headers = @(
        "-sS", "-f", "--max-time", "20",
        "-H", "Accept: application/json",
        "-H", "Content-Type: application/json",
        "-d", $Body
    )
    if ($Token) {
        $headers += @("-H", "Authorization: Bearer $Token")
    }
    $raw = & curl.exe @headers "$script:BaseUrl$Path"
    if ($LASTEXITCODE -ne 0) {
        throw "POST $Path failed (exit $LASTEXITCODE)"
    }
    Write-Host $raw
    Write-Host ""
    return $raw
}

$script:BaseUrl = $null
if (Test-SmokeBase "http://localhost:5000") {
    $script:BaseUrl = "http://localhost:5000"
}
elseif (Test-SmokeBase "http://localhost:5080") {
    $script:BaseUrl = "http://localhost:5080"
}
else {
    throw "Neither gateway http://localhost:5000 nor API http://localhost:5080 answered /api/health."
}

Write-Host "Smoke against $script:BaseUrl"
Write-Host ""

Invoke-SmokeGet "/api/health"
Invoke-SmokeGet "/api/catalog/needs"
Invoke-SmokeGet "/api/catalog/places"
Invoke-SmokeGet "/api/catalog/retreats"

$loginJson = Invoke-SmokePost "/api/auth/login" '{"email":"guest@local.test","password":"Local123!"}'
$login = $loginJson | ConvertFrom-Json
if (-not $login.accessToken) {
    throw "POST /api/auth/login did not return accessToken."
}

Invoke-SmokeGet "/api/users/me" $login.accessToken
Invoke-SmokePost "/api/matching/sessions" '{"answers":{"q1":["calm-mind"],"q2":["yoga-meditation"],"q3":"few-days","q4":["anywhere"]}}'
Invoke-SmokeGet "/api/leads/ready"
Invoke-SmokeGet "/api/payment/ready"
Invoke-SmokeGet "/api/availability/ready"

Write-Host "Smoke passed against $script:BaseUrl"
