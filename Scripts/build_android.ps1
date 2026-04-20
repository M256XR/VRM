param(
    [string]$ProjectRoot = "D:\Projects\VRM",
    [string]$UnityExe = "D:\2022.3.22f1\Editor\Unity.exe",
    [string]$ExportFolderName = "",
    [string]$ApkName = "wallpaper.apk",
    [switch]$MinimalScene,
    [switch]$SkipExport,
    [switch]$SkipGradle
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message =="
}

function Resolve-GradlePath {
    $candidates = @(
        "C:\Users\yuu\.gradle\wrapper\dists\gradle-7.3.3-bin\6a41zxkdtcxs8rphpq6y0069z\gradle-7.3.3\bin\gradle.bat",
        "C:\Users\yuu\.gradle\wrapper\dists\gradle-7.2-bin\2dnblmf4td7x66yl1d74lt32g\gradle-7.2\bin\gradle.bat"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $discovered = Get-ChildItem -Path "C:\Users\yuu\.gradle\wrapper\dists" -Recurse -Filter "gradle.bat" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName

    if ($discovered) {
        return $discovered
    }

    throw "gradle.bat was not found."
}

function Invoke-CheckedProcess {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [string]$LogPath,
        [hashtable]$EnvironmentOverrides = @{},
        [switch]$PreserveExistingLog
    )

    $escapedArguments = $Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_ -replace '"', '\"') + '"'
        }
        else {
            $_
        }
    }
    $argumentString = [string]::Join(" ", $escapedArguments)
    $stdoutPath = "$LogPath.stdout"
    $stderrPath = "$LogPath.stderr"

    $originalEnv = @{}
    foreach ($key in $EnvironmentOverrides.Keys) {
        $originalEnv[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
        [Environment]::SetEnvironmentVariable($key, [string]$EnvironmentOverrides[$key], "Process")
    }

    try {
        $process = Start-Process `
            -FilePath $FilePath `
            -ArgumentList $argumentString `
            -WorkingDirectory $WorkingDirectory `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath `
            -PassThru `
            -Wait `
            -NoNewWindow
    }
    finally {
        foreach ($key in $EnvironmentOverrides.Keys) {
            [Environment]::SetEnvironmentVariable($key, $originalEnv[$key], "Process")
        }
    }

    $allLines = @()
    if (Test-Path $stdoutPath) {
        $allLines += Get-Content -Path $stdoutPath
    }
    if (Test-Path $stderrPath) {
        $allLines += Get-Content -Path $stderrPath
    }
    if ($PreserveExistingLog -and (Test-Path $LogPath) -and $allLines.Count -eq 0) {
        # Unity writes directly to -logFile, so there may be nothing on stdout/stderr.
    }
    else {
        [System.IO.File]::WriteAllLines($LogPath, $allLines)
    }
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -ErrorAction SilentlyContinue

    if ($process.ExitCode -ne 0) {
        $tail = $allLines | Select-Object -Last 80
        if ($tail) {
            $tail | ForEach-Object { Write-Host $_ }
        }
        throw "Process failed: $FilePath (exit code $($process.ExitCode))"
    }
}

if ([string]::IsNullOrWhiteSpace($ExportFolderName)) {
    $ExportFolderName = "AndroidExport_" + (Get-Date -Format "yyyyMMdd_HHmmss")
}

$projectRoot = (Resolve-Path $ProjectRoot).Path
$buildSmokeDir = Join-Path $projectRoot "BuildSmoke"
$exportApkDir = Join-Path $projectRoot "Exportapk"
$exportDir = Join-Path $buildSmokeDir $ExportFolderName
$unityLog = Join-Path $buildSmokeDir ("unity_export_{0}.log" -f $ExportFolderName)
$gradleLog = Join-Path $buildSmokeDir ("gradle_{0}.log" -f $ExportFolderName)
$unityJdk = "D:\2022.3.22f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK"

if (!(Test-Path $UnityExe)) {
    throw "Unity.exe was not found: $UnityExe"
}

if (!(Test-Path $unityJdk)) {
    throw "Unity bundled JDK was not found: $unityJdk"
}

if (!(Test-Path $buildSmokeDir)) {
    New-Item -ItemType Directory -Path $buildSmokeDir | Out-Null
}

if (!(Test-Path $exportApkDir)) {
    New-Item -ItemType Directory -Path $exportApkDir | Out-Null
}

if (!$SkipExport) {
    Write-Step "Unity export"
    $env:BUILD_SMOKE_EXPORT_FOLDER = $ExportFolderName
    if ($MinimalScene) {
        $env:BUILD_SMOKE_MINIMAL_SCENE = "1"
    } else {
        Remove-Item Env:BUILD_SMOKE_MINIMAL_SCENE -ErrorAction SilentlyContinue
    }
    Invoke-CheckedProcess `
        -FilePath $UnityExe `
        -Arguments @(
            "-batchmode",
            "-quit",
            "-projectPath", $projectRoot,
            "-executeMethod", "BuildSmokeTest.ExportAndroidProject",
            "-logFile", $unityLog
        ) `
        -WorkingDirectory $projectRoot `
        -LogPath $unityLog `
        -PreserveExistingLog
}

if (!(Test-Path $exportDir)) {
    throw "Export folder was not found: $exportDir"
}

if (!$SkipGradle) {
    Write-Step "Gradle assembleDebug"
    $gradle = Resolve-GradlePath
    Invoke-CheckedProcess `
        -FilePath $gradle `
        -Arguments @("assembleDebug") `
        -WorkingDirectory $exportDir `
        -LogPath $gradleLog `
        -EnvironmentOverrides @{
            JAVA_HOME = $unityJdk
            PATH = "$unityJdk\bin;$env:PATH"
        }
}

$builtApk = Join-Path $exportDir "launcher\build\outputs\apk\debug\launcher-debug.apk"
if (!(Test-Path $builtApk)) {
    throw "APK was not found: $builtApk"
}

$finalApk = Join-Path $exportApkDir $ApkName
Copy-Item -LiteralPath $builtApk -Destination $finalApk -Force

Write-Step "Done"
Write-Host "ExportFolder : $ExportFolderName"
Write-Host "UnityLog     : $unityLog"
Write-Host "GradleLog    : $gradleLog"
Write-Host "APK          : $finalApk"
