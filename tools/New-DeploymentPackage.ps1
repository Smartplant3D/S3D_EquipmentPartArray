$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$release = Join-Path $root 'src/S3D.EquipmentPartArray/bin/Release/obfuscated'
$destination = Join-Path $root 'dist/S3D_EquipmentPartArray'
& dotnet build (Join-Path $root 'src/S3D.EquipmentPartArray/S3D.EquipmentPartArray.csproj') -c Release -p:NuGetAudit=false -v:q
if ($LASTEXITCODE -ne 0) { throw 'Release build failed; deployment package was not updated' }
& (Join-Path $root 'tests/Test-CommandEntry.ps1') | Out-Null
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $release 'S3D.EquipmentPartArray.dll') -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $release 'YYB.Licensing.dll') -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $root 'docs/USER_MANUAL.md') -Destination $destination -Force
if (Test-Path -LiteralPath (Join-Path $destination 'S3D.EquipmentPartArray.Core.dll')) {
    Remove-Item -LiteralPath (Join-Path $destination 'S3D.EquipmentPartArray.Core.dll')
}
Get-ChildItem -LiteralPath $destination -File | Select-Object Name,Length
