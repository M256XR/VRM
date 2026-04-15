# VRM Wallpaper

Unity 2022.3.22f1 Android live wallpaper project.

## Current setup

- Unity runtime scenes:
  - `Assets/Scenes/MainScene.unity`
  - `Assets/Scenes/SettingsScene.unity`
- Native Android settings UI:
  - `Assets/Plugins/Android/VRMWallpaperNative.androidlib`
- Main Android manifest override:
  - `Assets/Plugins/Android/AndroidManifest.xml`
- Smoke export helper:
  - `Assets/Editor/BuildSmokeTest.cs`
- Build script:
  - `Scripts/build_android.ps1`

The launcher entry point is `com.oreoreooooooo.VRM.MainActivity`.
Unity is used for VRM rendering and wallpaper runtime.
Android native UI is used for settings and file picking.
The old `com.justzht.unilwp.droid.free` package is no longer used.

## Native settings features

- Pick VRM file
- Pick background image
- Switch between solid color and image background
- Edit background RGB color
- Edit image fit mode, offset X/Y, and scale
- Edit camera distance, height, and angle
- Open Unity preview
- Set live wallpaper
- Reload wallpaper

## Important files

- `Assets/script/VRMLoader.cs`
- `Assets/script/PrefsHelper.cs`
- `Assets/script/BackgroundManager.cs`
- `Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/SettingsActivity.java`
- `Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/WallpaperActivity.java`
- `Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/WallpaperPrefs.java`

## Smoke build

Recommended command:

`powershell -ExecutionPolicy Bypass -File D:\Projects\VRM\Scripts\build_android.ps1`

The script handles export, Gradle build, Unity bundled JDK, and APK copy.

Batch export method:

`BuildSmokeTest.ExportAndroidProject`

File:

`Assets/Editor/BuildSmokeTest.cs`

Recent smoke-build output location:

- Exported Gradle project:
  - `BuildSmoke/AndroidExport_<timestamp>` or a named export folder
- Debug APK:
  - `Exportapk/wallpaper.apk`

Recent verification:

- Unity batch export: success
- Gradle `:launcher:assembleDebug`: success
- APK path:
  - `Exportapk/wallpaper.apk`

## Notes

- `Exports/` contains older experimental output and is not the source of truth.
- The native Android plugin under `Assets/Plugins/Android` should survive Unity re-export.
- Logs were moved away from public `Download` storage to app persistent storage where possible.
- Remaining build warnings are minor:
  - `NativeFilePicker` still contributes legacy storage permission merge noise.
  - Unity's generated `UnityPlayerActivity.java` still emits a deprecated API note during compile.
