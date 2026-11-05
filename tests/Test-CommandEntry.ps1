$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$path = Join-Path $root 'src/S3D.EquipmentPartArray/bin/Release/obfuscated/S3D.EquipmentPartArray.dll'
$packageRoot = Join-Path $env:USERPROFILE '.nuget/packages/fody'
$cecil = Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter Mono.Cecil.dll |
    Where-Object FullName -match 'netstandard2.0' | Select-Object -First 1 -ExpandProperty FullName
if (-not $cecil) { throw 'Mono.Cecil metadata reader was not found' }
Add-Type -Path $cecil
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($path)
$entry = $assembly.MainModule.Types | Where-Object FullName -eq 'S3D.EquipmentPartArray.EquipmentPartArrayCommand'
if ($null -eq $entry) { throw 'Custom command entry was renamed or removed' }
$attributes = $entry.CustomAttributes | ForEach-Object AttributeType | ForEach-Object FullName
if ($attributes -notcontains 'System.Runtime.InteropServices.ComVisibleAttribute') { throw 'Command entry lacks ComVisible(true)' }
if ($attributes -notcontains 'System.Runtime.InteropServices.GuidAttribute') { throw 'Command entry lacks COM Guid' }
if ($attributes -notcontains 'System.Runtime.InteropServices.ProgIdAttribute') { throw 'Command entry lacks ProgId' }
$visible = $entry.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'System.Runtime.InteropServices.ComVisibleAttribute' }
if (-not $visible.ConstructorArguments[0].Value) { throw 'Command entry is not COM-visible' }
$progId = $entry.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'System.Runtime.InteropServices.ProgIdAttribute' }
if ($progId.ConstructorArguments[0].Value -ne 'S3D.EquipmentPartArray.EquipmentPartArrayCommand') { throw 'Command ProgId differs from registration' }
if ($assembly.MainModule.AssemblyReferences.Name -contains 'S3D.EquipmentPartArray.Core') { throw 'Command DLL still requires a separate Core DLL' }
Write-Output $entry.FullName
