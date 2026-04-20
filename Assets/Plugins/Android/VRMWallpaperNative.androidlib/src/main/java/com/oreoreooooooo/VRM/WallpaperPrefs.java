package com.oreoreooooooo.VRM;

import android.content.Context;
import android.content.SharedPreferences;

import java.util.Map;

final class WallpaperPrefs {
    static final String PREFS_NAME = "VRMWallpaperPrefs";
    private static final int PREFS_MODE = 0x0004;

    private static final String KEY_VRM_PATH = "vrmPath";
    private static final String KEY_VRM_DISPLAY_NAME = "vrmDisplayName";
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
    private static final String KEY_RENDER_SCALE = "renderScale";
    private static final String KEY_TARGET_FPS = "targetFps";
    private static final String KEY_TOUCH_ENABLED = "touchEnabled";
    private static final String KEY_PHYSBONE_ENABLED = "physBoneEnabled";
    private static final String KEY_LOG_LEVEL = "logLevel";
    private static final String KEY_FPS_OVERLAY_ENABLED = "fpsOverlayEnabled";
    private static final String KEY_MODEL_OFFSET_X = "modelOffsetX";
    private static final String KEY_MODEL_OFFSET_Y = "modelOffsetY";
    private static final String KEY_MODEL_OFFSET_Z = "modelOffsetZ";
    private static final String KEY_MODEL_SCALE = "modelScale";

    private static final String PROFILE_PREFIX = "profile.";

    private static final String[] SNAPSHOT_KEYS = new String[] {
            KEY_VRM_PATH,
            KEY_VRM_DISPLAY_NAME,
            KEY_CAMERA_DISTANCE,
            KEY_CAMERA_HEIGHT,
            KEY_CAMERA_ANGLE,
            KEY_BG_COLOR_R,
            KEY_BG_COLOR_G,
            KEY_BG_COLOR_B,
            KEY_BG_MODE,
            KEY_BG_IMAGE_PATH,
            KEY_BG_IMAGE_FIT_MODE,
            KEY_BG_IMAGE_OFFSET_X,
            KEY_BG_IMAGE_OFFSET_Y,
            KEY_BG_IMAGE_SCALE,
            KEY_RENDER_SCALE,
            KEY_TARGET_FPS,
            KEY_TOUCH_ENABLED,
            KEY_PHYSBONE_ENABLED,
            KEY_LOG_LEVEL,
            KEY_FPS_OVERLAY_ENABLED,
            KEY_MODEL_OFFSET_X,
            KEY_MODEL_OFFSET_Y,
            KEY_MODEL_OFFSET_Z,
            KEY_MODEL_SCALE
    };

    private WallpaperPrefs() {
    }

    static final class ProfileSnapshot {
        final String vrmPath;
        final String vrmDisplayName;
        final float cameraDistance;
        final float cameraHeight;
        final float cameraAngle;
        final float backgroundColorRed;
        final float backgroundColorGreen;
        final float backgroundColorBlue;
        final int backgroundMode;
        final String backgroundImagePath;
        final float renderScale;
        final int targetFps;
        final float modelOffsetX;
        final float modelOffsetY;
        final float modelOffsetZ;
        final float modelScale;

        private ProfileSnapshot(
                String vrmPath,
                String vrmDisplayName,
                float cameraDistance,
                float cameraHeight,
                float cameraAngle,
                float backgroundColorRed,
                float backgroundColorGreen,
                float backgroundColorBlue,
                int backgroundMode,
                String backgroundImagePath,
                float renderScale,
                int targetFps,
                float modelOffsetX,
                float modelOffsetY,
                float modelOffsetZ,
                float modelScale) {
            this.vrmPath = vrmPath;
            this.vrmDisplayName = vrmDisplayName;
            this.cameraDistance = cameraDistance;
            this.cameraHeight = cameraHeight;
            this.cameraAngle = cameraAngle;
            this.backgroundColorRed = backgroundColorRed;
            this.backgroundColorGreen = backgroundColorGreen;
            this.backgroundColorBlue = backgroundColorBlue;
            this.backgroundMode = backgroundMode;
            this.backgroundImagePath = backgroundImagePath;
            this.renderScale = renderScale;
            this.targetFps = targetFps;
            this.modelOffsetX = modelOffsetX;
            this.modelOffsetY = modelOffsetY;
            this.modelOffsetZ = modelOffsetZ;
            this.modelScale = modelScale;
        }
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

    static String getVrmDisplayName(Context context) {
        return prefs(context).getString(KEY_VRM_DISPLAY_NAME, "");
    }

    static void setVrmDisplayName(Context context, String value) {
        prefs(context).edit().putString(KEY_VRM_DISPLAY_NAME, value).commit();
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

    static float getRenderScale(Context context) {
        return prefs(context).getFloat(KEY_RENDER_SCALE, 1.0f);
    }

    static void setRenderScale(Context context, float value) {
        prefs(context).edit().putFloat(KEY_RENDER_SCALE, value).commit();
    }

    static int getTargetFps(Context context) {
        return prefs(context).getInt(KEY_TARGET_FPS, 30);
    }

    static void setTargetFps(Context context, int value) {
        prefs(context).edit().putInt(KEY_TARGET_FPS, value).commit();
    }

    static boolean getTouchEnabled(Context context) {
        return prefs(context).getBoolean(KEY_TOUCH_ENABLED, true);
    }

    static void setTouchEnabled(Context context, boolean value) {
        prefs(context).edit().putBoolean(KEY_TOUCH_ENABLED, value).commit();
    }

    static boolean getPhysBoneEnabled(Context context) {
        return prefs(context).getBoolean(KEY_PHYSBONE_ENABLED, true);
    }

    static void setPhysBoneEnabled(Context context, boolean value) {
        prefs(context).edit().putBoolean(KEY_PHYSBONE_ENABLED, value).commit();
    }

    static int getLogLevel(Context context) {
        return prefs(context).getInt(KEY_LOG_LEVEL, 0);
    }

    static void setLogLevel(Context context, int value) {
        prefs(context).edit().putInt(KEY_LOG_LEVEL, value).commit();
    }

    static boolean getFpsOverlayEnabled(Context context) {
        return prefs(context).getBoolean(KEY_FPS_OVERLAY_ENABLED, true);
    }

    static void setFpsOverlayEnabled(Context context, boolean value) {
        prefs(context).edit().putBoolean(KEY_FPS_OVERLAY_ENABLED, value).commit();
    }

    static float getModelOffsetX(Context context) {
        return prefs(context).getFloat(KEY_MODEL_OFFSET_X, 0.0f);
    }

    static void setModelOffsetX(Context context, float value) {
        prefs(context).edit().putFloat(KEY_MODEL_OFFSET_X, value).commit();
    }

    static float getModelOffsetY(Context context) {
        return prefs(context).getFloat(KEY_MODEL_OFFSET_Y, 0.0f);
    }

    static void setModelOffsetY(Context context, float value) {
        prefs(context).edit().putFloat(KEY_MODEL_OFFSET_Y, value).commit();
    }

    static float getModelOffsetZ(Context context) {
        return prefs(context).getFloat(KEY_MODEL_OFFSET_Z, 0.0f);
    }

    static void setModelOffsetZ(Context context, float value) {
        prefs(context).edit().putFloat(KEY_MODEL_OFFSET_Z, value).commit();
    }

    static float getModelScale(Context context) {
        return prefs(context).getFloat(KEY_MODEL_SCALE, 1.0f);
    }

    static void setModelScale(Context context, float value) {
        prefs(context).edit().putFloat(KEY_MODEL_SCALE, value).commit();
    }

    static boolean hasProfileSlot(Context context, int slot) {
        SharedPreferences sharedPreferences = prefs(context);
        for (String key : SNAPSHOT_KEYS) {
            if (sharedPreferences.contains(profileKey(slot, key))) {
                return true;
            }
        }
        return false;
    }

    static void saveProfileSlot(Context context, int slot) {
        SharedPreferences.Editor editor = prefs(context).edit();
        editor.putString(profileKey(slot, KEY_VRM_PATH), getVrmPath(context));
        editor.putString(profileKey(slot, KEY_VRM_DISPLAY_NAME), getVrmDisplayName(context));
        editor.putFloat(profileKey(slot, KEY_CAMERA_DISTANCE), getCameraDistance(context));
        editor.putFloat(profileKey(slot, KEY_CAMERA_HEIGHT), getCameraHeight(context));
        editor.putFloat(profileKey(slot, KEY_CAMERA_ANGLE), getCameraAngle(context));
        editor.putFloat(profileKey(slot, KEY_BG_COLOR_R), getBackgroundColorRed(context));
        editor.putFloat(profileKey(slot, KEY_BG_COLOR_G), getBackgroundColorGreen(context));
        editor.putFloat(profileKey(slot, KEY_BG_COLOR_B), getBackgroundColorBlue(context));
        editor.putInt(profileKey(slot, KEY_BG_MODE), getBackgroundMode(context));
        editor.putString(profileKey(slot, KEY_BG_IMAGE_PATH), getBackgroundImagePath(context));
        editor.putInt(profileKey(slot, KEY_BG_IMAGE_FIT_MODE), getBackgroundImageFitMode(context));
        editor.putFloat(profileKey(slot, KEY_BG_IMAGE_OFFSET_X), getBackgroundImageOffsetX(context));
        editor.putFloat(profileKey(slot, KEY_BG_IMAGE_OFFSET_Y), getBackgroundImageOffsetY(context));
        editor.putFloat(profileKey(slot, KEY_BG_IMAGE_SCALE), getBackgroundImageScale(context));
        editor.putFloat(profileKey(slot, KEY_RENDER_SCALE), getRenderScale(context));
        editor.putInt(profileKey(slot, KEY_TARGET_FPS), getTargetFps(context));
        editor.putBoolean(profileKey(slot, KEY_TOUCH_ENABLED), getTouchEnabled(context));
        editor.putBoolean(profileKey(slot, KEY_PHYSBONE_ENABLED), getPhysBoneEnabled(context));
        editor.putInt(profileKey(slot, KEY_LOG_LEVEL), getLogLevel(context));
        editor.putBoolean(profileKey(slot, KEY_FPS_OVERLAY_ENABLED), getFpsOverlayEnabled(context));
        editor.putFloat(profileKey(slot, KEY_MODEL_OFFSET_X), getModelOffsetX(context));
        editor.putFloat(profileKey(slot, KEY_MODEL_OFFSET_Y), getModelOffsetY(context));
        editor.putFloat(profileKey(slot, KEY_MODEL_OFFSET_Z), getModelOffsetZ(context));
        editor.putFloat(profileKey(slot, KEY_MODEL_SCALE), getModelScale(context));
        editor.commit();
    }

    static ProfileSnapshot getProfileSnapshot(Context context, int slot) {
        if (!hasProfileSlot(context, slot)) {
            return null;
        }

        SharedPreferences sharedPreferences = prefs(context);
        return new ProfileSnapshot(
                sharedPreferences.getString(profileKey(slot, KEY_VRM_PATH), ""),
                sharedPreferences.getString(profileKey(slot, KEY_VRM_DISPLAY_NAME), ""),
                sharedPreferences.getFloat(profileKey(slot, KEY_CAMERA_DISTANCE), 3.0f),
                sharedPreferences.getFloat(profileKey(slot, KEY_CAMERA_HEIGHT), 0.5f),
                sharedPreferences.getFloat(profileKey(slot, KEY_CAMERA_ANGLE), 0.0f),
                sharedPreferences.getFloat(profileKey(slot, KEY_BG_COLOR_R), 0.2f),
                sharedPreferences.getFloat(profileKey(slot, KEY_BG_COLOR_G), 0.2f),
                sharedPreferences.getFloat(profileKey(slot, KEY_BG_COLOR_B), 0.2f),
                sharedPreferences.getInt(profileKey(slot, KEY_BG_MODE), 0),
                sharedPreferences.getString(profileKey(slot, KEY_BG_IMAGE_PATH), ""),
                sharedPreferences.getFloat(profileKey(slot, KEY_RENDER_SCALE), 1.0f),
                sharedPreferences.getInt(profileKey(slot, KEY_TARGET_FPS), 30),
                sharedPreferences.getFloat(profileKey(slot, KEY_MODEL_OFFSET_X), 0.0f),
                sharedPreferences.getFloat(profileKey(slot, KEY_MODEL_OFFSET_Y), 0.0f),
                sharedPreferences.getFloat(profileKey(slot, KEY_MODEL_OFFSET_Z), 0.0f),
                sharedPreferences.getFloat(profileKey(slot, KEY_MODEL_SCALE), 1.0f));
    }

    static boolean loadProfileSlot(Context context, int slot) {
        if (!hasProfileSlot(context, slot)) {
            return false;
        }

        SharedPreferences sharedPreferences = prefs(context);
        SharedPreferences.Editor editor = sharedPreferences.edit();
        Map<String, ?> allValues = sharedPreferences.getAll();
        for (String key : SNAPSHOT_KEYS) {
            putSnapshotValue(editor, key, allValues.get(profileKey(slot, key)));
        }
        return editor.commit();
    }

    static void resetRuntimeSettings(Context context) {
        SharedPreferences.Editor editor = prefs(context).edit();
        editor.putFloat(KEY_CAMERA_DISTANCE, 3.0f);
        editor.putFloat(KEY_CAMERA_HEIGHT, 0.5f);
        editor.putFloat(KEY_CAMERA_ANGLE, 0.0f);
        editor.putFloat(KEY_BG_COLOR_R, 0.2f);
        editor.putFloat(KEY_BG_COLOR_G, 0.2f);
        editor.putFloat(KEY_BG_COLOR_B, 0.2f);
        editor.putInt(KEY_BG_IMAGE_FIT_MODE, 1);
        editor.putFloat(KEY_BG_IMAGE_OFFSET_X, 0.0f);
        editor.putFloat(KEY_BG_IMAGE_OFFSET_Y, 0.0f);
        editor.putFloat(KEY_BG_IMAGE_SCALE, 1.0f);
        editor.putFloat(KEY_RENDER_SCALE, 1.0f);
        editor.putInt(KEY_TARGET_FPS, 30);
        editor.putBoolean(KEY_TOUCH_ENABLED, true);
        editor.putBoolean(KEY_PHYSBONE_ENABLED, true);
        editor.putInt(KEY_LOG_LEVEL, 0);
        editor.putBoolean(KEY_FPS_OVERLAY_ENABLED, true);
        editor.putFloat(KEY_MODEL_OFFSET_X, 0.0f);
        editor.putFloat(KEY_MODEL_OFFSET_Y, 0.0f);
        editor.putFloat(KEY_MODEL_OFFSET_Z, 0.0f);
        editor.putFloat(KEY_MODEL_SCALE, 1.0f);
        editor.commit();
    }

    private static String profileKey(int slot, String key) {
        return PROFILE_PREFIX + slot + "." + key;
    }

    private static void putSnapshotValue(SharedPreferences.Editor editor, String key, Object value) {
        if (value == null) {
            editor.remove(key);
            return;
        }

        if (value instanceof String) {
            editor.putString(key, (String) value);
            return;
        }

        if (value instanceof Integer) {
            editor.putInt(key, (Integer) value);
            return;
        }

        if (value instanceof Float) {
            editor.putFloat(key, (Float) value);
            return;
        }

        if (value instanceof Boolean) {
            editor.putBoolean(key, (Boolean) value);
            return;
        }

        if (value instanceof Long) {
            editor.putLong(key, (Long) value);
        }
    }
}
