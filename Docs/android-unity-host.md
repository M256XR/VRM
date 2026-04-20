# Android Unity Host

## Current Direction

The Android app and live wallpaper now share one Unity runtime through `UnityRuntimeHost`.

The app no longer creates its own `UnityPlayer`. Instead:

- `UnityRuntimeHost` owns one `UnityPlayer`.
- `WallpaperActivity` passes the wallpaper `Surface` to the host.
- `MainActivity` passes an in-app `SurfaceView` surface to the host.
- The host chooses the app surface while the app is visible, otherwise the wallpaper surface.

This avoids the previous failure mode where two Unity runtimes in one process fought over Unity native state and graphics resources.

## Why This Exists

The earlier designs had tradeoffs:

- Separate wallpaper process plus app Unity: stable, but duplicated Unity startup and splash.
- Same process plus two Unity players: unstable. One side would steal or kill the other.
- Transparent native overlay: stable and light, but showed launcher icons behind the UI and was not a clean preview.

The current host model keeps one Unity runtime and switches render surfaces instead.

## Important Files

- `Assets/Plugins/Android/AndroidManifest.xml`
  - The wallpaper service no longer uses `android:process=":wallpaper"`.
  - `MainActivity` uses the preview theme.
- `Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/UnityRuntimeHost.java`
  - Owns the single `UnityPlayer`.
  - Tracks app/wallpaper surfaces and visibility.
  - Calls `displayChanged`, `resume`, and `pause`.
- `Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/WallpaperActivity.java`
  - Sends wallpaper surface and touch events to the host.
- `Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/MainActivity.java`
  - Sends the preview `SurfaceView` surface to the host.
  - Sends settings changes to Unity through the host.

## Settings Flow

Settings changes are written to `WallpaperPrefs` first, then sent directly to the current Unity runtime through `UnityRuntimeHost`.

There is no longer a broadcast bridge for normal settings changes. Keep this direct path unless the wallpaper service must be controlled from another process again.

## Render Scale

Render scale uses Unity dynamic resolution:

- `MainActivity` stores `renderScale`.
- `VRMLoaderV2` reads it through `PrefsHelper`.
- `VRMLoaderV2` enables `allowDynamicResolution` for cameras.
- `ScalableBufferManager.ResizeBuffers(scale, scale)` applies the runtime buffer scale.

The supported UI range is `0.5` to `1.0`. Values above `1.0` are intentionally not exposed because dynamic resolution is used to reduce buffer cost, not supersample.

If the scale appears ineffective, check `vrm_loader_v2.log` for the applied camera count and `ScalableBufferManager` scale factors before changing the Android surface lifecycle.

## Checks Before Changing This

When touching the host or lifecycle code, test:

- Start wallpaper, then open the app.
- Close the app and return to the home screen.
- Open another app, then return home.
- Turn the screen off and on.
- Re-set the live wallpaper from Android settings.
- Check that PhysBone and model animations continue after each transition.

If Unity appears black, crashes, or stops updating, first inspect `VRMUnityHost`, `VRMWallpaper`, and `VRMMainActivity` logs.
