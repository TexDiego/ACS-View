param([string]$OutputRoot = 'tmp/windows-release')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    [xml]$project = Get-Content 'ACS View.csproj'
    $windows = $project.Project.PropertyGroup | Where-Object { $_.WindowsAppSDKSelfContained -eq 'true' }
    $version = $windows.ApplicationDisplayVersion
    if (-not $version) { throw 'Windows version is missing.' }
    $output = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
    $app = Join-Path $output ('app-' + [guid]::NewGuid().ToString('N'))
    dotnet publish 'ACS View.csproj' -f net10.0-windows10.0.19041.0 -c Release -p:TargetFrameworks=net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true --self-contained true -o $app
    if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
    if (-not (Test-Path (Join-Path $app 'ACSView.exe'))) { throw 'Executable missing.' }
    $private = Get-ChildItem $app -Recurse -File | Where-Object { $_.Extension -in '.db','.sqlite','.sqlite3','.jks','.keystore','.p12','.pfx' }
    if ($private) { throw 'Private material found in package.' }
    Copy-Item 'docs/windows-download.md' (Join-Path $app 'LEIA-ME.md')
    $zip = Join-Path $output "ACSView-Windows-v$version-win-x64.zip"
    Compress-Archive -Path (Join-Path $app '*') -DestinationPath $zip -Force
    $hash = Get-FileHash $zip -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($zip))" | Set-Content (Join-Path $output 'SHA256SUMS.txt') -Encoding ascii
    Write-Output $zip
} finally { Pop-Location }
