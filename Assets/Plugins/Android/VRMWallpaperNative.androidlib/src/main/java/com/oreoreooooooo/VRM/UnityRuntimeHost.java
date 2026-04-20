package com.oreoreooooooo.VRM;

import android.content.Context;
import android.content.res.Configuration;
import android.util.Log;
import android.view.MotionEvent;
import android.view.Surface;

import com.unity3d.player.UnityPlayer;

final class UnityRuntimeHost {
    private static final String TAG = "VRMUnityHost";
    private static UnityRuntimeHost instance;

    static synchronized UnityRuntimeHost get(Context context) {
        if (instance == null) {
            instance = new UnityRuntimeHost(context.getApplicationContext());
        }
        return instance;
    }

    private final UnityPlayer unityPlayer;

    private Surface wallpaperSurface;
    private Surface appSurface;
    private Surface currentSurface;
    private boolean wallpaperVisible;
    private boolean appVisible;
    private boolean resumed;

    private UnityRuntimeHost(Context context) {
        ContextHolder.setContext(context);
        unityPlayer = new UnityPlayer(context);
        Log.d(TAG, "Unity runtime host created");
    }

    synchronized void setWallpaperSurface(Surface surface) {
        wallpaperSurface = surface;
        updateDisplayLocked("wallpaper surface");
    }

    synchronized void clearWallpaperSurface(Surface surface) {
        if (surface == null || surface == wallpaperSurface) {
            wallpaperSurface = null;
        }
        updateDisplayLocked("wallpaper surface cleared");
    }

    synchronized void setWallpaperVisible(boolean visible) {
        wallpaperVisible = visible;
        updateDisplayLocked("wallpaper visible=" + visible);
    }

    synchronized void setAppSurface(Surface surface) {
        appSurface = surface;
        updateDisplayLocked("app surface");
    }

    synchronized void clearAppSurface(Surface surface) {
        if (surface == null || surface == appSurface) {
            appSurface = null;
        }
        updateDisplayLocked("app surface cleared");
    }

    synchronized void setAppVisible(boolean visible) {
        appVisible = visible;
        updateDisplayLocked("app visible=" + visible);
    }

    synchronized void injectEvent(MotionEvent event) {
        unityPlayer.injectEvent(event);
    }

    synchronized void lowMemory() {
        unityPlayer.lowMemory();
    }

    synchronized void configurationChanged(Configuration configuration) {
        unityPlayer.configurationChanged(configuration);
    }

    synchronized void quit() {
        unityPlayer.quit();
        instance = null;
    }

    void sendMessage(String method, String message) {
        if (method == null || method.trim().isEmpty()) {
            Log.w(TAG, "Unity message skipped: method is empty");
            return;
        }
        UnityPlayer.UnitySendMessage("VRMLoader", method, message == null ? "" : message);
    }

    private void updateDisplayLocked(String reason) {
        Surface target = null;
        String owner = "none";

        if (appVisible && appSurface != null && appSurface.isValid()) {
            target = appSurface;
            owner = "app";
        } else if (wallpaperVisible && wallpaperSurface != null && wallpaperSurface.isValid()) {
            target = wallpaperSurface;
            owner = "wallpaper";
        }

        if (target != currentSurface) {
            unityPlayer.displayChanged(0, target);
            currentSurface = target;
            Log.d(TAG, "Display target changed: " + owner + " reason=" + reason);
        }

        boolean shouldRun = target != null;
        if (shouldRun && !resumed) {
            unityPlayer.windowFocusChanged(true);
            unityPlayer.resume();
            resumed = true;
            Log.d(TAG, "Unity resumed: " + owner);
            return;
        }

        if (!shouldRun && resumed) {
            unityPlayer.displayChanged(0, null);
            unityPlayer.windowFocusChanged(false);
            unityPlayer.pause();
            resumed = false;
            Log.d(TAG, "Unity paused: " + reason);
        }
    }
}
