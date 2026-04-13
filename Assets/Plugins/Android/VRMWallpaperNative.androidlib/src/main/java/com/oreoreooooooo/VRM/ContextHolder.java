package com.oreoreooooooo.VRM;

import android.content.Context;

public final class ContextHolder {
    private static Context applicationContext;

    private ContextHolder() {
    }

    public static void setContext(Context context) {
        if (context != null) {
            applicationContext = context.getApplicationContext();
        }
    }

    public static Context getContext() {
        return applicationContext;
    }
}
