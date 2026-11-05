$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/S3D.EquipmentPartArray.csproj') -Raw
$config = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/obfuscar.xml') -Raw
if ($project -notmatch 'ObfuscateReleaseOutput' -or $project -notmatch 'dotnet obfuscar.console obfuscar.xml') { throw 'Release obfuscation target missing' }
if ($config -notmatch 'S3D.EquipmentPartArray.dll' -or $config -notmatch 'KeepPublicApi') { throw 'Obfuscation module/configuration missing' }
if (-not (Test-Path -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/.config/dotnet-tools.json'))) { throw 'Tool manifest missing' }
Write-Output 'Obfuscation contract passed'
