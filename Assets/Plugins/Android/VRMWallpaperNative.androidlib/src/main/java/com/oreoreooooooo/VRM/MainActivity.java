package com.oreoreooooooo.VRM;

import android.app.Activity;
import android.content.res.Configuration;
import android.os.Bundle;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

public class MainActivity extends Activity {
    private static final String TAG = "VRMMainActivity";
    private UnityPlayer unityPlayer;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        ContextHolder.setContext(this);
        Log.d(TAG, "onCreate");
        super.onCreate(savedInstanceState);

        unityPlayer = new UnityPlayer(this);
        setContentView(unityPlayer);
        unityPlayer.requestFocus();
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (unityPlayer != null) {
            unityPlayer.resume();
        }
    }

    @Override
    protected void onPause() {
        if (unityPlayer != null) {
            unityPlayer.pause();
        }
        super.onPause();
    }

    @Override
    protected void onDestroy() {
        if (unityPlayer != null) {
            unityPlayer.quit();
            unityPlayer = null;
        }
        super.onDestroy();
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        if (unityPlayer != null) {
            unityPlayer.configurationChanged(newConfig);
        }
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (unityPlayer != null) {
            unityPlayer.windowFocusChanged(hasFocus);
        }
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        if (unityPlayer != null) {
            unityPlayer.lowMemory();
        }
    }
}
