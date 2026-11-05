# S3D Equipment Part Array

用户操作与安装说明见 [docs/USER_MANUAL.md](docs/USER_MANUAL.md)。首个发布说明见 [docs/RELEASE_NOTES_v0.1.0.md](docs/RELEASE_NOTES_v0.1.0.md)。

Smart 3D .NET Framework 4.6.2 x86 graphic command: `S3D.EquipmentPartArray.EquipmentPartArrayCommand` in `S3D.EquipmentPartArray.dll`. The command entry has COM-visible `Guid` and `ProgId` metadata.

Start the command with or without a preselection. The always-on-top panel defaults to **环形 / Circular** and shows Chinese and English captions together; **矩形 / Rectangular** remains selectable. Select one or more `EquipmentComponent` objects or equipment-level `GenericShape` objects from the same equipment, then click **使用所选部件/形体 / Use selection**. The selection button, mode selector, status, and Create button stay visible while parameter fields scroll. A preselection is captured automatically when available. A graphic pick returning a `GenericShape` or `PrismaticShape` whose immediate parent is an `EquipmentComponent` uses that component as the source; multiple shapes under one component count as one source. A `GenericShape` whose immediate parent is `Equipment` is copied as a shape. Directly selected equipment objects, equipment-level `PrismaticShape`, ports, and nozzles are rejected with their actual type in the copyable status area. Rectangular counts are rows × columns × layers; circular count includes the original selected group. Preview shows yellow line markers at planned source origins and the number of new items. The original group remains in place. The first selected source is the group reference. The base point can be picked in the view as a reference for the original group; rectangular translation is independent of that point. For the circular mode, pick a center and two points defining the axis. The `Fill angle` value is in degrees. Coordinate and pitch fields use Smart 3D's distance unit parser and formatter; axis components and angle are dimensionless numbers.

For `EquipmentComponent`, creation uses the source's catalog `EquipmentComponentPart`. For equipment-level `GenericShape`, creation uses a fresh `GenericShape(source.Part, equipment)` occurrence. The earlier `GenericShape(source)` copy constructor failed in the Smart 3D host with `PrivateBusinessObject is already initialized` before placement. The new path transfers writable, non-complex `IJUA*` instance property values and then sets the source's world matrix and the planned origin. It does not traverse nozzles, ports, or external relationships; before commit, it aborts if newly created ports appear under the equipment or a new component. This is not a complete property or relationship clone. Verify dimensions and other required instance attributes on a disposable test-model copy before production use. Smart 3D assigns names under its rules. The command requires read and update access to sources and create access to the equipment. Invalid sources or mixed equipment are rejected.

Build locally:

```powershell
dotnet run --project tests/S3D.EquipmentPartArray.Tests/S3D.EquipmentPartArray.Tests.csproj
dotnet build src/S3D.EquipmentPartArray/S3D.EquipmentPartArray.csproj -c Debug
dotnet build src/S3D.EquipmentPartArray/S3D.EquipmentPartArray.csproj -c Release
dotnet run --project tests/S3D.EquipmentPartArray.PanelTests/S3D.EquipmentPartArray.PanelTests.csproj
```

The Release build invokes Obfuscar. Run `./tools/New-DeploymentPackage.ps1` to assemble `dist/S3D_EquipmentPartArray/` with the command DLL and its license library. The command DLL contains the array math and needs no separate Core DLL. The compile guard `DISABLE_LICENSE_CHECK` leaves license enforcement off until explicitly activated.

Register the exact class name `S3D.EquipmentPartArray.EquipmentPartArrayCommand`. When the Smart 3D command registration uses a relative assembly identifier, use `S3D_EquipmentPartArray\S3D.EquipmentPartArray` and place the two packaged DLLs in the corresponding `S3D_EquipmentPartArray` command folder. Replace the previously deployed files after closing the Smart 3D client so the old assembly is not still loaded. This package has not been installed into the user's Smart 3D environment by the local build.

If Smart 3D still reports "class not found," verify that the DLL in the registered folder matches the packaged DLL and that the class spelling uses `Array` throughout. Also inspect the host's assembly-load error for a missing dependency; the same message can hide a type-loading failure.

## Required Smart 3D host checks

1. Register the class as a custom command on a disposable test plant. Confirm both empty and nonempty preselection launch, in-command Fence selection, selection capture, panel scrolling, and mouse picking operate in the graphic view.
2. Place two distinct component parts and an equipment-level GenericShape on one equipment. Select each source type, then a mixed group; check local/global rectangular directions, relative offsets, world orientation, ownership, and names.
3. Check 360° and open arc spacing, with `Rotate copies` on and off; inspect instance orientation and names.
4. Confirm preview changes and cancellation leave no persisted component or temporary graphics.
5. Force a batch failure after at least one copy and confirm the entire operation rolls back. Check access denial. For GenericShape copies, inspect any source nozzle/port references and confirm no unexpected child or external relation was copied.

Local build and math checks cannot prove these host behaviors.
