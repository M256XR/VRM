package com.oreoreooooooo.VRM;

import android.annotation.TargetApi;
import android.app.WallpaperColors;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.res.Configuration;
import android.graphics.Color;
import android.os.Build;
import android.service.wallpaper.WallpaperService;
import android.util.Log;
import android.view.MotionEvent;
import android.view.SurfaceHolder;
import android.view.WindowInsets;

import com.unity3d.player.UnityPlayer;

class MyUnityPlayer extends UnityPlayer {
    MyUnityPlayer(Context context) {
        super(context);
    }
}

public class WallpaperActivity extends WallpaperService {
    private static final String TAG = "VRMWallpaper";
    private static final String ACTION_RELOAD_VRM = "com.oreoreooooooo.VRM.RELOAD_VRM";

    private MyUnityPlayer unityPlayer;
    private BroadcastReceiver reloadReceiver;
    private int visibleSurfaces;

    @Override
    public void onCreate() {
        ContextHolder.setContext(this);
        super.onCreate();

        unityPlayer = new MyUnityPlayer(getApplicationContext());
        registerReloadReceiver();
        Log.d(TAG, "Wallpaper service created");
    }

    @Override
    public void onDestroy() {
        if (reloadReceiver != null) {
            try {
                unregisterReceiver(reloadReceiver);
            } catch (Exception exception) {
                Log.w(TAG, "Failed to unregister reload receiver", exception);
            }
        }

        if (unityPlayer != null) {
            unityPlayer.quit();
        }

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
        if (unityPlayer != null) {
            unityPlayer.lowMemory();
        }
    }

    @Override
    public void onTrimMemory(int level) {
        super.onTrimMemory(level);
        if (level == TRIM_MEMORY_RUNNING_CRITICAL && unityPlayer != null) {
            unityPlayer.lowMemory();
        }
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        if (unityPlayer != null) {
            unityPlayer.configurationChanged(newConfig);
        }
    }

    private void registerReloadReceiver() {
        reloadReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                if (intent == null || !ACTION_RELOAD_VRM.equals(intent.getAction())) {
                    return;
                }

                android.os.Handler handler = new android.os.Handler(android.os.Looper.getMainLooper());
                handler.post(() -> UnityPlayer.UnitySendMessage("VRMLoader", "ReloadVRM", ""));
            }
        };

        IntentFilter filter = new IntentFilter(ACTION_RELOAD_VRM);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            registerReceiver(reloadReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
            return;
        }

        registerReceiver(reloadReceiver, filter);
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
        }

        @Override
        public void onSurfaceChanged(SurfaceHolder surfaceHolder, int format, int width, int height) {
            super.onSurfaceChanged(surfaceHolder, format, width, height);
            holder = surfaceHolder;
            unityPlayer.displayChanged(0, holder.getSurface());
        }

        @Override
        public void onVisibilityChanged(boolean visible) {
            super.onVisibilityChanged(visible);

            if (visible) {
                visibleSurfaces++;
                if (holder != null) {
                    unityPlayer.displayChanged(0, holder.getSurface());
                }
                unityPlayer.windowFocusChanged(true);
                unityPlayer.resume();
                return;
            }

            visibleSurfaces = Math.max(visibleSurfaces - 1, 0);
            if (visibleSurfaces == 0) {
                unityPlayer.displayChanged(0, null);
                unityPlayer.windowFocusChanged(false);
                unityPlayer.pause();
            }
        }

        @Override
        public void onTouchEvent(MotionEvent event) {
            super.onTouchEvent(event);
            unityPlayer.injectEvent(event);
        }
    }
}
