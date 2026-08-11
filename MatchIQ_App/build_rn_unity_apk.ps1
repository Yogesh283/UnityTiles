$ErrorActionPreference = 'Continue'
$root = 'D:\UnityProjects\matchiq'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe'
$project = Join-Path $root 'Unity_Game'
$android = Join-Path $root 'MatchIQ_App\android'
$outDir = Join-Path $root 'MatchIQ_App\Builds\Android'
$exportLog = Join-Path $project 'rn-export.log'
$gradleLog = Join-Path $root 'MatchIQ_App\rn-gradle.log'

function Show-FreeRam {
  $os = Get-CimInstance Win32_OperatingSystem
  $free = [math]::Round($os.FreePhysicalMemory / 1MB, 2)
  Write-Output ("FreeRAM_GB=" + $free)
  return $free
}

Write-Output '=== MatchIQ RN + Unity APK ==='
Show-FreeRam | Out-Null

# --- Free RAM (safe: Gradle daemons + leftover Unity batch + Edge/Ollama if present) ---
Write-Output 'Freeing RAM...'
foreach ($n in @('Unity', 'Unity Hub', 'ollama app', 'ollama', 'msedge', 'msedgewebview2', 'java')) {
  Get-Process -Name $n -ErrorAction SilentlyContinue | ForEach-Object {
    try {
      Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
      Write-Output ("stopped " + $_.Name + " " + $_.Id)
    } catch {}
  }
}

New-Item -ItemType Directory -Force -Path 'C:\g' | Out-Null
$env:GRADLE_USER_HOME = 'C:\g'
$env:JAVA_HOME = 'C:\Program Files\Android\Android Studio\jbr'
$env:Path = "$env:JAVA_HOME\bin;" + $env:Path

Push-Location $android
try { & .\gradlew.bat --stop 2>&1 | Out-Null } catch {}
Pop-Location

Start-Sleep -Seconds 3
[gc]::Collect()
Show-FreeRam | Out-Null

# Unity Editor must not hold the project lock
if (Test-Path (Join-Path $project 'Temp\UnityLockFile')) {
  Write-Output 'WARN: UnityLockFile present — closing Unity processes again'
  Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  Start-Sleep -Seconds 2
}

# --- 1) Unity export as Android library ---
Write-Output 'STEP1: Unity ExportAndroidLibraryBatch...'
Remove-Item -Force $exportLog -ErrorAction SilentlyContinue
$p = Start-Process -FilePath $unity -ArgumentList @(
  '-batchmode', '-nographics', '-quit',
  '-projectPath', $project,
  '-executeMethod', 'MatchIQReactNativeExport.ExportAndroidLibraryBatch',
  '-logFile', $exportLog
) -PassThru -WindowStyle Hidden
Write-Output ("UNITY_PID=" + $p.Id)
Wait-Process -Id $p.Id
$unityCode = $p.ExitCode
Write-Output ("UNITY_EXIT_CODE=" + $unityCode)
if ($unityCode -ne 0) {
  Write-Output 'Unity export FAILED — see rn-export.log'
  if (Test-Path $exportLog) { Get-Content $exportLog -Tail 40 }
  exit $unityCode
}

$lib = Join-Path $root 'MatchIQ_App\unity\builds\android\unityLibrary'
if (-not (Test-Path $lib)) {
  Write-Output 'unityLibrary folder missing after export'
  exit 2
}
Write-Output 'Unity library export OK'

Show-FreeRam | Out-Null

# --- 2) RN assembleRelease ---
Write-Output 'STEP2: gradlew assembleRelease...'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Remove-Item -Force $gradleLog -ErrorAction SilentlyContinue

Push-Location $android
& .\gradlew.bat assembleRelease --no-daemon 2>&1 | Tee-Object -FilePath $gradleLog
$gradleCode = $LASTEXITCODE
Pop-Location
Write-Output ("GRADLE_EXIT_CODE=" + $gradleCode)

$apkCandidates = @(
  (Join-Path $android 'app\build\outputs\apk\release\app-release.apk'),
  (Join-Path $android 'app\build\outputs\apk\release\app-release-unsigned.apk')
)
$built = $apkCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $built) {
  Write-Output 'APK not found after assembleRelease'
  if (Test-Path $gradleLog) { Get-Content $gradleLog -Tail 50 }
  exit 3
}

$dest = Join-Path $outDir 'MatchIQ-RN-Unity-1.0.0.apk'
Copy-Item -Force $built $dest
$sizeMb = [math]::Round((Get-Item $dest).Length / 1MB, 1)
Write-Output ("APK_READY=" + $dest)
Write-Output ("APK_SIZE_MB=" + $sizeMb)

# --- 3) USB install if a device is connected ---
$adb = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
if (Test-Path $adb) {
  Write-Output 'STEP3: adb install...'
  & $adb devices -l
  & $adb install -r $dest
  Write-Output ("ADB_INSTALL_EXIT=" + $LASTEXITCODE)
} else {
  Write-Output 'ADB not found — skip install'
}

Show-FreeRam | Out-Null
exit 0
