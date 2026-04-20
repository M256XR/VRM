package com.oreoreooooooo.VRM;

import android.annotation.TargetApi;
import android.app.WallpaperColors;
import android.content.res.Configuration;
import android.graphics.Color;
import android.os.Build;
import android.service.wallpaper.WallpaperService;
import android.util.Log;
import android.view.MotionEvent;
import android.view.SurfaceHolder;
import android.view.WindowInsets;

public class WallpaperActivity extends WallpaperService {
    private static final String TAG = "VRMWallpaper";

    private UnityRuntimeHost unityHost;
    private int visibleSurfaces;

    @Override
    public void onCreate() {
        ContextHolder.setContext(this);
        super.onCreate();

        unityHost = UnityRuntimeHost.get(this);
        Log.d(TAG, "Wallpaper service created");
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        Log.d(TAG, "Wallpaper service destroyed");
    }

    @Override
    public Engine onCreateEngine() {
        return new MyEngine();
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        if (unityHost != null) {
            unityHost.lowMemory();
        }
    }

    @Override
    public void onTrimMemory(int level) {
        super.onTrimMemory(level);
        if (level == TRIM_MEMORY_RUNNING_CRITICAL && unityHost != null) {
            unityHost.lowMemory();
        }
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        if (unityHost != null) {
            unityHost.configurationChanged(newConfig);
        }
    }

    private int getWallpaperColor() {
        float r = WallpaperPrefs.getBackgroundColorRed(this);
        float g = WallpaperPrefs.getBackgroundColorGreen(this);
        float b = WallpaperPrefs.getBackgroundColorBlue(this);
        return Color.rgb(
                Math.round(r * 255.0f),
                Math.round(g * 255.0f),
                Math.round(b * 255.0f)
        );
    }

    class MyEngine extends Engine {
        private SurfaceHolder holder;

        @Override
        public void onCreate(SurfaceHolder surfaceHolder) {
            super.onCreate(surfaceHolder);
            setTouchEventsEnabled(true);
            setOffsetNotificationsEnabled(false);
        }

        @Override
        public void onApplyWindowInsets(WindowInsets insets) {
            super.onApplyWindowInsets(insets);
        }

        @TargetApi(Build.VERSION_CODES.O_MR1)
        @Override
        public WallpaperColors onComputeColors() {
            Color color = Color.valueOf(getWallpaperColor());
            return new WallpaperColors(color, color, color);
        }

        @Override
        public void onSurfaceCreated(SurfaceHolder surfaceHolder) {
            super.onSurfaceCreated(surfaceHolder);
            holder = surfaceHolder;
            UnityRuntimeHost.get(WallpaperActivity.this).setWallpaperSurface(holder.getSurface());
        }

        @Override
        public void onSurfaceChanged(SurfaceHolder surfaceHolder, int format, int width, int height) {
            super.onSurfaceChanged(surfaceHolder, format, width, height);
            holder = surfaceHolder;
            UnityRuntimeHost.get(WallpaperActivity.this).setWallpaperSurface(holder.getSurface());
        }

        @Override
        public void onSurfaceDestroyed(SurfaceHolder surfaceHolder) {
            UnityRuntimeHost.get(WallpaperActivity.this).clearWallpaperSurface(surfaceHolder.getSurface());
            holder = null;
            super.onSurfaceDestroyed(surfaceHolder);
        }

        @Override
        public void onVisibilityChanged(boolean visible) {
            super.onVisibilityChanged(visible);

            if (visible) {
                visibleSurfaces++;
                if (holder != null) {
                    UnityRuntimeHost.get(WallpaperActivity.this).setWallpaperSurface(holder.getSurface());
                }
                UnityRuntimeHost.get(WallpaperActivity.this).setWallpaperVisible(true);
                return;
            }

            visibleSurfaces = Math.max(visibleSurfaces - 1, 0);
            if (visibleSurfaces == 0) {
                UnityRuntimeHost.get(WallpaperActivity.this).setWallpaperVisible(false);
            }
        }

        @Override
        public void onTouchEvent(MotionEvent event) {
            super.onTouchEvent(event);
            UnityRuntimeHost.get(WallpaperActivity.this).injectEvent(event);
        }
    }
}
