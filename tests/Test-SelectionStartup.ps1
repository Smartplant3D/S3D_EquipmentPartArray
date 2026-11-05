$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$command = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/EquipmentPartArrayCommand.cs') -Raw
$panel = Get-Content -LiteralPath (Join-Path $root 'src/S3D.EquipmentPartArray/ArrayPanel.cs') -Raw
if ($command -notmatch 'class EquipmentPartArrayCommand : BaseStepCommand') { throw 'Command must provide in-command selection' }
if ($command -match 'EnableUIFlags[^;]+NonEmptySelectSet') { throw 'Command must start with empty SelectSet' }
if ($command -notmatch 'StepDefinition.LocateBehaviors.Fence') { throw 'Selection step missing' }
if ($command -notmatch 'StepDefinition.LocateBehaviors.SmartSketch3D') { throw 'Point-pick step missing' }
if ($panel -notmatch 'CaptureRequested') { throw 'Selection capture control missing' }
Write-Output 'Empty-selection startup contract passed'
