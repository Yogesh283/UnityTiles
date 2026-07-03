# Verify Nakama Docker stack for Match IQ Phase 1.5 POC
param(
    [string]$ComposeFile = "Nakama/docker-compose.nakama.yml"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "=== Match IQ Phase 1.5 — Nakama Docker Check ===" -ForegroundColor Cyan

function Test-Step($num, $name, $pass, $detail) {
    $verdict = if ($pass) { "PASS" } else { "FAIL" }
    $color = if ($pass) { "Green" } else { "Red" }
    $msg = "[NakamaPoc] STEP $num $name => $verdict"
    if ($detail) { $msg += " | $detail" }
    Write-Host $msg -ForegroundColor $color
    return $pass
}

$allPass = $true

# 1. Docker available
$dockerOk = $false
try {
    docker version *> $null
    $dockerOk = $LASTEXITCODE -eq 0
} catch { $dockerOk = $false }
$allPass = (Test-Step 1 "Docker available" $dockerOk) -and $allPass

if (-not $dockerOk) {
    Write-Host "=== OVERALL: FAIL (install Docker Desktop) ===" -ForegroundColor Red
    exit 1
}

# 2. Compose file exists
$composePath = Join-Path $repoRoot $ComposeFile
$composeExists = Test-Path $composePath
$allPass = (Test-Step 2 "Compose file exists" $composeExists $composePath) -and $allPass

# 3. Containers running
$ps = docker compose -f $ComposeFile ps --format json 2>$null | ConvertFrom-Json
$nakamaUp = $false
$postgresUp = $false
if ($ps) {
    foreach ($svc in $ps) {
        if ($svc.Service -eq "nakama" -and $svc.State -match "running") { $nakamaUp = $true }
        if ($svc.Service -eq "postgres" -and $svc.State -match "running") { $postgresUp = $true }
    }
}
$allPass = (Test-Step 3 "Postgres container running" $postgresUp) -and $allPass
$allPass = (Test-Step 4 "Nakama container running" $nakamaUp) -and $allPass

# 5. HTTP health (Nakama console/API port)
$httpOk = $false
try {
    $resp = Invoke-WebRequest -Uri "http://127.0.0.1:7351/" -UseBasicParsing -TimeoutSec 5
    $httpOk = $resp.StatusCode -ge 200
} catch {
    $httpOk = $false
}
$allPass = (Test-Step 5 "Nakama HTTP port 7351" $httpOk) -and $allPass

# 6. Runtime loaded in logs
$runtimeLoaded = $false
if ($nakamaUp) {
    $logs = docker compose -f $ComposeFile logs nakama 2>$null
    if ($logs -match "MatchIQ Phase 1.5 POC Nakama runtime loaded") {
        $runtimeLoaded = $true
    }
}
$allPass = (Test-Step 6 "POC runtime loaded in logs" $runtimeLoaded) -and $allPass

Write-Host ""
if ($allPass) {
    Write-Host "=== OVERALL PHASE 1.5 (Docker): PASS ===" -ForegroundColor Green
    Write-Host "Next: Unity -> Match IQ -> Nakama POC -> Run Phase 1.5 Test (Play Mode)"
    exit 0
} else {
    Write-Host "=== OVERALL PHASE 1.5 (Docker): FAIL ===" -ForegroundColor Red
    Write-Host "Start stack: docker compose -f $ComposeFile up"
    exit 1
}
