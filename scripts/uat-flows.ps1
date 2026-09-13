# Commerce UAT against the gateway (or API if the gateway is down).
# Run from the repo root after Healingram.Api and Healingram.Gateway are up.
# Does not mark payment paid from a browser URL — uses the fake webhook secret.

$ErrorActionPreference = "Stop"

function Test-Base([string]$Base, [string]$Path) {
    try {
        & curl.exe -sS -f --max-time 4 "$Base$Path" | Out-Null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

$script:BaseUrl = $null
if (Test-Base "http://localhost:5000" "/api/health") {
    $script:BaseUrl = "http://localhost:5000"
}
elseif (Test-Base "http://localhost:5000" "/api/gateway/health") {
    # Gateway is up but /api/health should still proxy. Prefer API if proxy is broken.
    if (Test-Base "http://localhost:5080" "/api/health") {
        Write-Host "WARN: gateway :5000 is up but /api/health does not proxy. Using API :5080."
        $script:BaseUrl = "http://localhost:5080"
    }
}
elseif (Test-Base "http://localhost:5080" "/api/health") {
    Write-Host "WARN: gateway :5000 did not answer /api/health. Using API :5080."
    $script:BaseUrl = "http://localhost:5080"
}
else {
    throw "Neither gateway :5000 nor API :5080 answered /api/health. Stop Docker infra-gateway-1 if it owns :5000."
}

Write-Host "UAT against $script:BaseUrl"
Write-Host ""

function Invoke-Get([string]$Path, [string]$Token = "") {
    $curlArgs = @("-sS", "-w", "`nHTTP:%{http_code}", "--max-time", "25", "-H", "Accept: application/json")
    if ($Token) { $curlArgs += @("-H", "Authorization: Bearer $Token") }
    $raw = & curl.exe @curlArgs "$script:BaseUrl$Path"
    Write-Host "GET  $Path"
    Write-Host $raw
    Write-Host ""
    return $raw
}

function Invoke-Post([string]$Path, [string]$Body, [string]$Token = "", [hashtable]$ExtraHeaders = @{}) {
    $tmp = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tmp, $Body)
    $curlArgs = @(
        "-sS", "-w", "`nHTTP:%{http_code}", "--max-time", "25",
        "-H", "Accept: application/json",
        "-H", "Content-Type: application/json",
        "-d", "@$tmp"
    )
    if ($Token) { $curlArgs += @("-H", "Authorization: Bearer $Token") }
    foreach ($headerName in $ExtraHeaders.Keys) {
        $curlArgs += @("-H", ("{0}: {1}" -f $headerName, $ExtraHeaders[$headerName]))
    }
    $raw = & curl.exe @curlArgs "$script:BaseUrl$Path"
    Remove-Item $tmp -ErrorAction SilentlyContinue
    Write-Host "POST $Path"
    Write-Host $raw
    Write-Host ""
    return $raw
}

function Get-Json([string]$Raw) {
    $json = [regex]::Replace($Raw, "\s*HTTP:\d+\s*$", "").Trim()
    if (-not $json) { return $null }
    return $json | ConvertFrom-Json
}

function Get-Code([string]$Raw) {
    if ($Raw -match "HTTP:(\d+)\s*$") { return [int]$Matches[1] }
    return 0
}

$script:failed = @()

function Assert-Code([string]$Id, [string]$Raw, [int[]]$Ok) {
    $code = Get-Code $Raw
    if ($Ok -notcontains $code) {
        $script:failed += "$Id expected $($Ok -join '/') got $code"
        Write-Host "FAIL $Id (HTTP $code)"
    }
    else {
        Write-Host "PASS $Id (HTTP $code)"
    }
}

# --- Flow 01 catalog ---
$needs = Invoke-Get "/api/catalog/needs"
Assert-Code "TC-01-needs" $needs @(200)
$discovery = Invoke-Get "/api/catalog/discovery"
Assert-Code "TC-01-discovery" $discovery @(200)
$places = Invoke-Get "/api/catalog/places"
Assert-Code "TC-01-places" $places @(200)
$retreats = Invoke-Get "/api/catalog/retreats"
Assert-Code "TC-01-retreats" $retreats @(200)
$missing = Invoke-Get "/api/catalog/retreats/not-a-real-slug"
Assert-Code "TC-01-unpublished" $missing @(404)
$list = Get-Json $retreats
$slug = "shathayu"
if ($list.items -and $list.items.Count -gt 0) {
    $slug = [string]$list.items[0].slug
}
$listing = Invoke-Get "/api/catalog/retreats/$slug"
Assert-Code "TC-01-listing" $listing @(200)
$programme = "ayurveda"
$listingObj = Get-Json $listing
if ($listingObj.programmes -and $listingObj.programmes.Count -gt 0) {
    $programme = [string]$listingObj.programmes[0].slug
}

# --- Flow 02 login ---
$guestLogin = Invoke-Post "/api/auth/login" '{"email":"guest@local.test","password":"Local123!"}'
Assert-Code "TC-02-guest" $guestLogin @(200)
$guest = Get-Json $guestLogin
$guestToken = [string]$guest.accessToken
$me = Invoke-Get "/api/users/me" $guestToken
Assert-Code "TC-02-me" $me @(200)
$bad = Invoke-Post "/api/auth/login" '{"email":"guest@local.test","password":"wrong-pass"}'
Assert-Code "TC-02-bad-password" $bad @(401)
$partnerLogin = Invoke-Post "/api/auth/login" '{"email":"partner@local.test","password":"Local123!"}'
Assert-Code "TC-02-partner" $partnerLogin @(200)
$partnerToken = [string](Get-Json $partnerLogin).accessToken
$adminLogin = Invoke-Post "/api/auth/login" '{"email":"admin@local.test","password":"Local123!"}'
Assert-Code "TC-02-admin" $adminLogin @(200)
$adminToken = [string](Get-Json $adminLogin).accessToken

# --- Flow 03 matching ---
$options = Invoke-Get "/api/matching/options"
Assert-Code "TC-03-options" $options @(200)
$matchBody = '{"answers":{"q1":["calm-mind"],"q2":["yoga-meditation"],"q3":"few-days","q4":["anywhere"]}}'
$match = Invoke-Post "/api/matching/sessions" $matchBody
Assert-Code "TC-03-match" $match @(200, 201)

# --- Flow 04 request ---
$checkIn = (Get-Date).AddDays(21).ToString("yyyy-MM-dd")
$key = [guid]::NewGuid().ToString()
$reqBody = @"
{"idempotencyKey":"$key","retreatSlug":"$slug","programmeSlug":"$programme","durationNights":7,"occupancy":"double","guests":2,"checkIn":"$checkIn","customerName":"UAT Guest","email":"guest@local.test","phone":"9876543210"}
"@
$created = Invoke-Post "/api/availability/requests" $reqBody $guestToken
Assert-Code "TC-04-create" $created @(201, 200)
$publicId = [string](Get-Json $created).publicId
$got = Invoke-Get "/api/availability/requests/$publicId" $guestToken
Assert-Code "TC-04-get" $got @(200)
$anon = Invoke-Get "/api/availability/requests/$publicId"
Assert-Code "TC-04-anonymous-blocked" $anon @(401)
$earlyPay = Invoke-Post "/api/payment/intents" (@{ publicId = $publicId; idempotencyKey = [guid]::NewGuid().ToString() } | ConvertTo-Json -Compress) $guestToken
Assert-Code "TC-06-too-early" $earlyPay @(400, 404, 409, 422)

# --- Flow 05 confirm ---
$confirm = Invoke-Post "/api/availability/requests/$publicId/confirm" '{"finalAmountInr":45000}' $partnerToken
if ((Get-Code $confirm) -eq 403) {
    Write-Host "Partner confirm 403 - retrying as admin"
    $confirm = Invoke-Post "/api/availability/requests/$publicId/confirm" '{"finalAmountInr":45000}' $adminToken
}
Assert-Code "TC-05-confirm" $confirm @(200)

# --- Flow 06 payment ---
$payKey = [guid]::NewGuid().ToString()
$intent = Invoke-Post "/api/payment/intents" (@{ publicId = $publicId; idempotencyKey = $payKey } | ConvertTo-Json -Compress) $guestToken
Assert-Code "TC-06-intent" $intent @(201, 200)
$intentId = [string](Get-Json $intent).id
$read = Invoke-Get "/api/payment/intents/$intentId" $guestToken
Assert-Code "TC-06-get" $read @(200)
$intentObj = Get-Json $read
$amount = $intentObj.amountInr
if (-not $amount) { $amount = 45000 }
$eventId = [guid]::NewGuid().ToString()
$hookBody = (@{ intentId = $intentId; providerEventId = $eventId; amountInr = $amount; currency = "INR" } | ConvertTo-Json -Compress)
$hook = Invoke-Post "/api/payment/webhooks/fake" $hookBody "" @{ "X-Webhook-Secret" = "local-dev-webhook-secret" }
Assert-Code "TC-06-webhook" $hook @(200)
$hook2 = Invoke-Post "/api/payment/webhooks/fake" $hookBody "" @{ "X-Webhook-Secret" = "local-dev-webhook-secret" }
Assert-Code "TC-06-webhook-idemp" $hook2 @(200)
$badHook = Invoke-Post "/api/payment/webhooks/fake" (@{ intentId = $intentId; providerEventId = [guid]::NewGuid().ToString(); amountInr = $amount; currency = "INR" } | ConvertTo-Json -Compress)
Assert-Code "TC-06-webhook-nosecret" $badHook @(401)

# --- Flow 07 trips / wishlist ---
$trips = Invoke-Get "/api/trips" $guestToken
Assert-Code "TC-07-trips" $trips @(200)
$wish = Invoke-Post "/api/wishlist" (@{ slug = $slug } | ConvertTo-Json -Compress) $guestToken
Assert-Code "TC-07-wish" $wish @(200, 201, 204)

# --- Flow 08 leads ---
$leadBody = '{"fullName":"UAT Guest","phone":"9876543210","email":"guest@local.test","helpType":"dates","need":"ayurveda","travelWindow":"next-month","whatsappConsent":true,"source":"contact"}'
$lead = Invoke-Post "/api/leads" $leadBody
Assert-Code "TC-08-lead" $lead @(201, 200)
$leadId = [string](Get-Json $lead).id
if ($leadId) {
    $asGuest = Invoke-Get "/api/leads/$leadId" $guestToken
    Assert-Code "TC-08-guest-forbidden" $asGuest @(401, 403)
    $asAdmin = Invoke-Get "/api/leads/$leadId" $adminToken
    Assert-Code "TC-08-admin-get" $asAdmin @(200)
}

# --- Flow 10 admin ---
$adminQ = Invoke-Get "/api/admin/availability" $adminToken
Assert-Code "TC-10-queue" $adminQ @(200)
$partnerOnAdmin = Invoke-Get "/api/admin/availability" $partnerToken
Assert-Code "TC-10-partner-forbidden" $partnerOnAdmin @(403)

Write-Host ""
if ($script:failed.Count -gt 0) {
    Write-Host "UAT failed:"
    $script:failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "UAT script passed against $script:BaseUrl (publicId=$publicId intent=$intentId)"
