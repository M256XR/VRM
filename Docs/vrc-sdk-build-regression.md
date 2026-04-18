# VRC SDK Build Regression Notes

## Summary

ALCOM re-apply after the VRC SDK install left the project in a state where Android export failed before manifest generation finished.

The visible symptom was:

- `BuildPipeline.BuildPlayer` returned `Failed`
- exported project was missing `launcher/src/main/AndroidManifest.xml`
- later Gradle failed, but that was only a downstream symptom

The actual causes were in VRC SDK runtime plugin import settings and legacy/runtime dependencies.

## Confirmed failure chain

### 1. `VRCCore-Editor.dll` was treated as a player assembly

File:

- [VRCCore-Editor.dll.meta](D:/Projects/VRM/Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/VRCCore-Editor.dll.meta)

Broken-state characteristics:

- `Any` platform was enabled
- Android was not excluded in the broad platform block

Observed error:

- `UnityEditor.dll assembly is referenced by user code, but this is not allowed.`

### 2. `WorldValidation.cs` depended on `VRC.Core.Logger`

File:

- [WorldValidation.cs](D:/Projects/VRM/Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Scripts/Validation/WorldValidation.cs)

After removing the `VRCCore-Editor.dll` leak, compile then failed on:

- `VRC.Core.Logger` not found

For this project, direct logger dependency was replaced with plain `Debug.LogWarning(...)`.

### 3. `SDKBase-Legacy.dll` pulled editor-only dependency during linking

File:

- [SDKBase-Legacy.dll.meta](D:/Projects/VRM/Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/SDKBase-Legacy.dll.meta)

Observed linker failure:

- `SDKBase-Legacy.dll`
- `Failed to resolve assembly: 'VRCCore-Editor'`

For this wallpaper app, `SDKBase-Legacy.dll` is not needed in player builds, so it was removed from player import targets.

## Files changed to recover build

- [VRCCore-Editor.dll.meta](D:/Projects/VRM/Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/VRCCore-Editor.dll.meta)
- [SDKBase-Legacy.dll.meta](D:/Projects/VRM/Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/SDKBase-Legacy.dll.meta)
- [WorldValidation.cs](D:/Projects/VRM/Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Scripts/Validation/WorldValidation.cs)
- [BuildSmokeTest.cs](D:/Projects/VRM/Assets/Editor/BuildSmokeTest.cs)

## Diagnostics added

`BuildSmokeTest` was extended to emit detailed build diagnostics:

- [BuildSmokeTest.cs](D:/Projects/VRM/Assets/Editor/BuildSmokeTest.cs)
- [buildsmoke_diagnostics.log](D:/Projects/VRM/BuildSmoke/buildsmoke_diagnostics.log)

This was necessary because the normal Unity batch log only showed:

- `executeMethod method BuildSmokeTest.ExportAndroidProject threw exception`

The detailed report exposed the real failure steps.

## Broken-state comparison

Backup used for comparison:

- `D:\Projects\VRM_WorkspaceBackups\20260415_161941`

Most useful comparison points:

1. broken backup vs current
   - `Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/VRCCore-Editor.dll.meta`
   - `Packages/com.vrchat.base/Runtime/VRCSDK/Plugins/SDKBase-Legacy.dll.meta`
2. build diagnostics before vs after fix
   - `BuildSmoke/buildsmoke_diagnostics.log`

## Practical rule

If Android export suddenly fails after VRC SDK / ALCOM changes:

1. check `*Editor.dll.meta` under `Packages/com.vrchat.base/Runtime/VRCSDK/Plugins`
2. confirm editor DLLs are not active for player builds
3. check whether legacy VRCSDK2 DLLs are entering the player build
4. only after that look at Gradle / manifest output

## ALCOM Re-apply Notes

ALCOM re-apply was worth doing for this project, but it did not leave the repo in a directly buildable state.

After re-apply, these regressions came back again:

1. `VRCCore-Editor.dll.meta`
   - `Any` was re-enabled
   - result: `UnityEditor.dll assembly is referenced by user code`
2. `SDKBase-Legacy.dll.meta`
   - player exclusion was lost
   - result: legacy/editor dependency could re-enter the build path
3. `WorldValidation.cs`
   - direct `VRC.Core.Logger` dependency came back
   - result: player compile failed because the logger type was unavailable

So the practical flow is:

1. re-apply with ALCOM if package state seems inconsistent
2. immediately restore the known-safe build fixes above
3. then verify Android export again

## PhysBone Runtime Notes

After the ALCOM re-apply and build-fix restore, PhysBone runtime behavior also recovered.

Useful checkpoints from `physbone_touch.log` when things are healthy:

- `hasAfterAdd=53`
- `chains=53`
- `rootChainCount=53`

Those indicate:

- PhysBone components were accepted by `PhysBoneManager`
- runtime chains were constructed
- the avatar-side root was linked correctly

If PhysBone breaks again after VRC SDK package changes:

1. fix build regressions first
2. rebuild
3. only then look at touch / scheduler / runtime bootstrap logs

Otherwise you end up debugging runtime behavior on top of a broken package state.
