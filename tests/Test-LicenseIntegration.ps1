$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/S3D.EquipmentPartArray.csproj') -Raw
$command = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/EquipmentPartArrayCommand.cs') -Raw
$guard = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/Services/LicenseGuard.cs') -Raw
if ($project -notmatch 'DISABLE_LICENSE_CHECK' -or $project -notmatch 'YYB.Licensing') { throw 'License project integration missing' }
if ($command -notmatch '#if !DISABLE_LICENSE_CHECK' -or $command -notmatch 'LicenseGuard.Validate') { throw 'License entry hook missing' }
if ($guard -notmatch 'S3D.EquipmentPartArray.lic' -or $guard -notmatch 'S3D.EquipmentPartArray') { throw 'License product configuration missing' }
Write-Output 'License integration contract passed'
