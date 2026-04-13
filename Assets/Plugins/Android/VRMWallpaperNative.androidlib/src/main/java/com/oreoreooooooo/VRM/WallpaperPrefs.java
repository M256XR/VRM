package com.oreoreooooooo.VRM;

import android.content.Context;
import android.content.SharedPreferences;

final class WallpaperPrefs {
    static final String PREFS_NAME = "VRMWallpaperPrefs";
    private static final int PREFS_MODE = 0x0004;

    private static final String KEY_VRM_PATH = "vrmPath";
    private static final String KEY_CAMERA_DISTANCE = "cameraDistance";
    private static final String KEY_CAMERA_HEIGHT = "cameraHeight";
    private static final String KEY_CAMERA_ANGLE = "cameraAngle";
    private static final String KEY_BG_COLOR_R = "bgColorR";
    private static final String KEY_BG_COLOR_G = "bgColorG";
    private static final String KEY_BG_COLOR_B = "bgColorB";
    private static final String KEY_BG_MODE = "bgMode";
    private static final String KEY_BG_IMAGE_PATH = "bgImagePath";
    private static final String KEY_BG_IMAGE_FIT_MODE = "bgImageFitMode";
    private static final String KEY_BG_IMAGE_OFFSET_X = "bgImageOffsetX";
    private static final String KEY_BG_IMAGE_OFFSET_Y = "bgImageOffsetY";
    private static final String KEY_BG_IMAGE_SCALE = "bgImageScale";

    private WallpaperPrefs() {
    }

    private static SharedPreferences prefs(Context context) {
        return context.getSharedPreferences(PREFS_NAME, PREFS_MODE);
    }

    static String getVrmPath(Context context) {
        return prefs(context).getString(KEY_VRM_PATH, "");
    }

    static void setVrmPath(Context context, String path) {
        prefs(context).edit().putString(KEY_VRM_PATH, path).commit();
    }

    static float getCameraDistance(Context context) {
        return prefs(context).getFloat(KEY_CAMERA_DISTANCE, 3.0f);
    }

    static void setCameraDistance(Context context, float value) {
        prefs(context).edit().putFloat(KEY_CAMERA_DISTANCE, value).commit();
    }

    static float getCameraHeight(Context context) {
        return prefs(context).getFloat(KEY_CAMERA_HEIGHT, 0.5f);
    }

    static void setCameraHeight(Context context, float value) {
        prefs(context).edit().putFloat(KEY_CAMERA_HEIGHT, value).commit();
    }

    static float getCameraAngle(Context context) {
        return prefs(context).getFloat(KEY_CAMERA_ANGLE, 0.0f);
    }

    static void setCameraAngle(Context context, float value) {
        prefs(context).edit().putFloat(KEY_CAMERA_ANGLE, value).commit();
    }

    static int getBackgroundMode(Context context) {
        return prefs(context).getInt(KEY_BG_MODE, 0);
    }

    static void setBackgroundMode(Context context, int value) {
        prefs(context).edit().putInt(KEY_BG_MODE, value).commit();
    }

    static float getBackgroundColorRed(Context context) {
        return prefs(context).getFloat(KEY_BG_COLOR_R, 0.2f);
    }

    static void setBackgroundColorRed(Context context, float value) {
        prefs(context).edit().putFloat(KEY_BG_COLOR_R, value).commit();
    }

    static float getBackgroundColorGreen(Context context) {
        return prefs(context).getFloat(KEY_BG_COLOR_G, 0.2f);
    }

    static void setBackgroundColorGreen(Context context, float value) {
        prefs(context).edit().putFloat(KEY_BG_COLOR_G, value).commit();
    }

    static float getBackgroundColorBlue(Context context) {
        return prefs(context).getFloat(KEY_BG_COLOR_B, 0.2f);
    }

    static void setBackgroundColorBlue(Context context, float value) {
        prefs(context).edit().putFloat(KEY_BG_COLOR_B, value).commit();
    }

    static String getBackgroundImagePath(Context context) {
        return prefs(context).getString(KEY_BG_IMAGE_PATH, "");
    }

    static void setBackgroundImagePath(Context context, String path) {
        prefs(context).edit().putString(KEY_BG_IMAGE_PATH, path).commit();
    }

    static int getBackgroundImageFitMode(Context context) {
        return prefs(context).getInt(KEY_BG_IMAGE_FIT_MODE, 1);
    }

    static void setBackgroundImageFitMode(Context context, int value) {
        prefs(context).edit().putInt(KEY_BG_IMAGE_FIT_MODE, value).commit();
    }

    static float getBackgroundImageOffsetX(Context context) {
        return prefs(context).getFloat(KEY_BG_IMAGE_OFFSET_X, 0.0f);
    }

    static void setBackgroundImageOffsetX(Context context, float value) {
        prefs(context).edit().putFloat(KEY_BG_IMAGE_OFFSET_X, value).commit();
    }

    static float getBackgroundImageOffsetY(Context context) {
        return prefs(context).getFloat(KEY_BG_IMAGE_OFFSET_Y, 0.0f);
    }

    static void setBackgroundImageOffsetY(Context context, float value) {
        prefs(context).edit().putFloat(KEY_BG_IMAGE_OFFSET_Y, value).commit();
    }

    static float getBackgroundImageScale(Context context) {
        return prefs(context).getFloat(KEY_BG_IMAGE_SCALE, 1.0f);
    }

    static void setBackgroundImageScale(Context context, float value) {
        prefs(context).edit().putFloat(KEY_BG_IMAGE_SCALE, value).commit();
    }
}
