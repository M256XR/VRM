using UnityEngine;

public static class PrefsHelper
{
    private const string PREFS_NAME = "VRMWallpaperPrefs";
    private const string KEY_VRM_PATH = "vrmPath";
    private const string KEY_VRM_DISPLAY_NAME = "vrmDisplayName";
    private const string KEY_CAMERA_DISTANCE = "cameraDistance";
    private const string KEY_CAMERA_HEIGHT = "cameraHeight";
    private const string KEY_CAMERA_ANGLE = "cameraAngle";
    
    // VRMパスを保存(プロセス間共有)
    public static void SetVRMPath(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log($"===== PrefsHelper.SetVRMPath: {path} =====");
            
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putString", KEY_VRM_PATH, path);
                        bool committed = editor.Call<bool>("commit");
                        Debug.Log($"VRM path saved (multi-process): {path}, committed: {committed}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save VRM path: {e.Message}");
            Debug.LogError($"StackTrace: {e.StackTrace}");
        }
#else
        PlayerPrefs.SetString(KEY_VRM_PATH, path);
        PlayerPrefs.Save();
        Debug.Log($"Editor: VRM path saved: {path}");
#endif
    }
    
    // VRMパスを取得(プロセス間共有)
    public static string GetVRMPath(string defaultPath = "")
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("===== PrefsHelper.GetVRMPath START =====");
            
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        Debug.Log("SharedPreferences obtained");
                        
                        string path = prefs.Call<string>("getString", KEY_VRM_PATH, defaultPath);
                        Debug.Log($"VRM path loaded (multi-process): {path}");
                        
                        bool exists = System.IO.File.Exists(path);
                        Debug.Log($"File exists: {exists}");
                        
                        return path;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PrefsHelper.GetVRMPath EXCEPTION: {e.Message}");
            Debug.LogError($"StackTrace: {e.StackTrace}");
            return defaultPath;
        }
#endif
        string editorPath = PlayerPrefs.GetString(KEY_VRM_PATH, defaultPath);
        Debug.Log($"Editor mode - VRM path: {editorPath}");
        return editorPath;
    }

    public static void SetVRMDisplayName(string value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putString", KEY_VRM_DISPLAY_NAME, value ?? "");
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save VRM display name: {e.Message}");
        }
#else
        PlayerPrefs.SetString(KEY_VRM_DISPLAY_NAME, value ?? "");
        PlayerPrefs.Save();
#endif
    }

    public static string GetVRMDisplayName(string defaultValue = "")
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<string>("getString", KEY_VRM_DISPLAY_NAME, defaultValue);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load VRM display name: {e.Message}");
        }

        return defaultValue;
#else
        return PlayerPrefs.GetString(KEY_VRM_DISPLAY_NAME, defaultValue);
#endif
    }
    
    // ===== カメラ設定 =====
    
    // カメラ距離を保存
    public static void SetCameraDistance(float distance)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_CAMERA_DISTANCE, distance);
                        editor.Call<bool>("commit");
                        Debug.Log($"Camera distance saved: {distance}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save camera distance: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_CAMERA_DISTANCE, distance);
        PlayerPrefs.Save();
#endif
    }
    
    // カメラ距離を取得
    public static float GetCameraDistance(float defaultValue = 3.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        float distance = prefs.Call<float>("getFloat", KEY_CAMERA_DISTANCE, defaultValue);
                        Debug.Log($"Camera distance loaded: {distance}");
                        return distance;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load camera distance: {e.Message}");
            return defaultValue;
        }
#endif
        return PlayerPrefs.GetFloat(KEY_CAMERA_DISTANCE, defaultValue);
    }
    
    // カメラ高さを保存
    public static void SetCameraHeight(float height)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_CAMERA_HEIGHT, height);
                        editor.Call<bool>("commit");
                        Debug.Log($"Camera height saved: {height}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save camera height: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_CAMERA_HEIGHT, height);
        PlayerPrefs.Save();
#endif
    }
    
    // カメラ高さを取得
    public static float GetCameraHeight(float defaultValue = 0.5f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        float height = prefs.Call<float>("getFloat", KEY_CAMERA_HEIGHT, defaultValue);
                        Debug.Log($"Camera height loaded: {height}");
                        return height;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load camera height: {e.Message}");
            return defaultValue;
        }
#endif
        return PlayerPrefs.GetFloat(KEY_CAMERA_HEIGHT, defaultValue);
    }
    
    // カメラ角度を保存
    public static void SetCameraAngle(float angle)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_CAMERA_ANGLE, angle);
                        editor.Call<bool>("commit");
                        Debug.Log($"Camera angle saved: {angle}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save camera angle: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_CAMERA_ANGLE, angle);
        PlayerPrefs.Save();
#endif
    }
    
    // カメラ角度を取得
    public static float GetCameraAngle(float defaultValue = 0.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        float angle = prefs.Call<float>("getFloat", KEY_CAMERA_ANGLE, defaultValue);
                        Debug.Log($"Camera angle loaded: {angle}");
                        return angle;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load camera angle: {e.Message}");
            return defaultValue;
        }
#endif
        return PlayerPrefs.GetFloat(KEY_CAMERA_ANGLE, defaultValue);
    }
    
    // ===== 背景色設定 =====
    
    private const string KEY_BG_COLOR_R = "bgColorR";
    private const string KEY_BG_COLOR_G = "bgColorG";
    private const string KEY_BG_COLOR_B = "bgColorB";
    
    // 背景色を保存
    public static void SetBackgroundColor(Color color)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_BG_COLOR_R, color.r);
                        editor.Call<AndroidJavaObject>("putFloat", KEY_BG_COLOR_G, color.g);
                        editor.Call<AndroidJavaObject>("putFloat", KEY_BG_COLOR_B, color.b);
                        editor.Call<bool>("commit");
                        Debug.Log($"Background color saved: R={color.r}, G={color.g}, B={color.b}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save background color: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_BG_COLOR_R, color.r);
        PlayerPrefs.SetFloat(KEY_BG_COLOR_G, color.g);
        PlayerPrefs.SetFloat(KEY_BG_COLOR_B, color.b);
        PlayerPrefs.Save();
#endif
    }
    
    // 背景色を取得(デフォルト: 濃いグレー)
    public static Color GetBackgroundColor()
    {
        Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 濃いグレー
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        float r = prefs.Call<float>("getFloat", KEY_BG_COLOR_R, defaultColor.r);
                        float g = prefs.Call<float>("getFloat", KEY_BG_COLOR_G, defaultColor.g);
                        float b = prefs.Call<float>("getFloat", KEY_BG_COLOR_B, defaultColor.b);
                        
                        Color color = new Color(r, g, b, 1f);
                        Debug.Log($"Background color loaded: R={r}, G={g}, B={b}");
                        return color;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load background color: {e.Message}");
            return defaultColor;
        }
#endif
        float rEditor = PlayerPrefs.GetFloat(KEY_BG_COLOR_R, defaultColor.r);
        float gEditor = PlayerPrefs.GetFloat(KEY_BG_COLOR_G, defaultColor.g);
        float bEditor = PlayerPrefs.GetFloat(KEY_BG_COLOR_B, defaultColor.b);
        return new Color(rEditor, gEditor, bEditor, 1f);
    }
    
    // ===== 背景画像設定 =====
    
    private const string KEY_BG_MODE = "bgMode"; // 0=単色, 1=画像
    private const string KEY_BG_IMAGE_PATH = "bgImagePath";
    
    // 背景モードを保存(0=単色, 1=画像)
    public static void SetBackgroundMode(int mode)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putInt", KEY_BG_MODE, mode);
                        editor.Call<bool>("commit");
                        Debug.Log($"Background mode saved: {mode} ({(mode == 0 ? "Color" : "Image")})");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save background mode: {e.Message}");
        }
#else
        PlayerPrefs.SetInt(KEY_BG_MODE, mode);
        PlayerPrefs.Save();
#endif
    }
    
    // 背景モードを取得(デフォルト: 0=単色)
    public static int GetBackgroundMode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        int mode = prefs.Call<int>("getInt", KEY_BG_MODE, 0);
                        Debug.Log($"Background mode loaded: {mode} ({(mode == 0 ? "Color" : "Image")})");
                        return mode;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load background mode: {e.Message}");
            return 0;
        }
#endif
        return PlayerPrefs.GetInt(KEY_BG_MODE, 0);
    }
    
    // 背景画像パスを保存
    public static void SetBackgroundImagePath(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putString", KEY_BG_IMAGE_PATH, path);
                        editor.Call<bool>("commit");
                        Debug.Log($"Background image path saved: {path}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save background image path: {e.Message}");
        }
#else
        PlayerPrefs.SetString(KEY_BG_IMAGE_PATH, path);
        PlayerPrefs.Save();
#endif
    }
    
    // 背景画像パスを取得
    public static string GetBackgroundImagePath()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        string path = prefs.Call<string>("getString", KEY_BG_IMAGE_PATH, "");
                        Debug.Log($"Background image path loaded: {path}");
                        return path;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load background image path: {e.Message}");
            return "";
        }
#endif
        return PlayerPrefs.GetString(KEY_BG_IMAGE_PATH, "");
    }
    
    // ===== 背景画像調整設定 =====
    
    private const string KEY_BG_IMAGE_FIT_MODE = "bgImageFitMode"; // 0=Fit, 1=Fill, 2=Stretch, 3=Custom
    private const string KEY_BG_IMAGE_OFFSET_X = "bgImageOffsetX";
    private const string KEY_BG_IMAGE_OFFSET_Y = "bgImageOffsetY";
    private const string KEY_BG_IMAGE_SCALE = "bgImageScale";
    private const string KEY_RENDER_SCALE = "renderScale";
    private const string KEY_TARGET_FPS = "targetFps";
    private const string KEY_TOUCH_ENABLED = "touchEnabled";
    private const string KEY_PHYSBONE_ENABLED = "physBoneEnabled";
    private const string KEY_LOG_LEVEL = "logLevel";
    private const string KEY_FPS_OVERLAY_ENABLED = "fpsOverlayEnabled";
    private const string KEY_MODEL_OFFSET_X = "modelOffsetX";
    private const string KEY_MODEL_OFFSET_Y = "modelOffsetY";
    private const string KEY_MODEL_OFFSET_Z = "modelOffsetZ";
    private const string KEY_MODEL_SCALE = "modelScale";
    
    // フィットモードを保存 (0=Fit, 1=Fill, 2=Stretch, 3=Custom)
    public static void SetBackgroundImageFitMode(int mode)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putInt", KEY_BG_IMAGE_FIT_MODE, mode);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save fit mode: {e.Message}");
        }
#else
        PlayerPrefs.SetInt(KEY_BG_IMAGE_FIT_MODE, mode);
        PlayerPrefs.Save();
#endif
    }
    
    // フィットモードを取得 (デフォルト: 1=Fill)
    public static int GetBackgroundImageFitMode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<int>("getInt", KEY_BG_IMAGE_FIT_MODE, 1);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load fit mode: {e.Message}");
            return 1;
        }
#endif
        return PlayerPrefs.GetInt(KEY_BG_IMAGE_FIT_MODE, 1);
    }
    
    // 画像オフセットX を保存
    public static void SetBackgroundImageOffsetX(float offset)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_BG_IMAGE_OFFSET_X, offset);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save offset X: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_BG_IMAGE_OFFSET_X, offset);
        PlayerPrefs.Save();
#endif
    }
    
    // 画像オフセットX を取得 (デフォルト: 0)
    public static float GetBackgroundImageOffsetX()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<float>("getFloat", KEY_BG_IMAGE_OFFSET_X, 0.0f);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load offset X: {e.Message}");
            return 0.0f;
        }
#endif
        return PlayerPrefs.GetFloat(KEY_BG_IMAGE_OFFSET_X, 0.0f);
    }
    
    // 画像オフセットY を保存
    public static void SetBackgroundImageOffsetY(float offset)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_BG_IMAGE_OFFSET_Y, offset);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save offset Y: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_BG_IMAGE_OFFSET_Y, offset);
        PlayerPrefs.Save();
#endif
    }
    
    // 画像オフセットY を取得 (デフォルト: 0)
    public static float GetBackgroundImageOffsetY()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<float>("getFloat", KEY_BG_IMAGE_OFFSET_Y, 0.0f);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load offset Y: {e.Message}");
            return 0.0f;
        }
#endif
        return PlayerPrefs.GetFloat(KEY_BG_IMAGE_OFFSET_Y, 0.0f);
    }
    
    // 画像スケールを保存
    public static void SetBackgroundImageScale(float scale)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", KEY_BG_IMAGE_SCALE, scale);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save scale: {e.Message}");
        }
#else
        PlayerPrefs.SetFloat(KEY_BG_IMAGE_SCALE, scale);
        PlayerPrefs.Save();
#endif
    }
    
    // 画像スケールを取得 (デフォルト: 1.0)
    public static float GetBackgroundImageScale()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<float>("getFloat", KEY_BG_IMAGE_SCALE, 1.0f);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scale: {e.Message}");
            return 1.0f;
        }
#endif
        return PlayerPrefs.GetFloat(KEY_BG_IMAGE_SCALE, 1.0f);
    }

    public static void SetRenderScale(float scale)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveFloat(KEY_RENDER_SCALE, scale);
#else
        PlayerPrefs.SetFloat(KEY_RENDER_SCALE, scale);
        PlayerPrefs.Save();
#endif
    }

    public static float GetRenderScale(float defaultValue = 1.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadFloat(KEY_RENDER_SCALE, defaultValue);
#else
        return PlayerPrefs.GetFloat(KEY_RENDER_SCALE, defaultValue);
#endif
    }

    public static void SetTargetFps(int fps)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveInt(KEY_TARGET_FPS, fps);
#else
        PlayerPrefs.SetInt(KEY_TARGET_FPS, fps);
        PlayerPrefs.Save();
#endif
    }

    public static int GetTargetFps(int defaultValue = 30)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadInt(KEY_TARGET_FPS, defaultValue);
#else
        return PlayerPrefs.GetInt(KEY_TARGET_FPS, defaultValue);
#endif
    }

    public static void SetTouchEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveBool(KEY_TOUCH_ENABLED, enabled);
#else
        PlayerPrefs.SetInt(KEY_TOUCH_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
#endif
    }

    public static bool GetTouchEnabled(bool defaultValue = true)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadBool(KEY_TOUCH_ENABLED, defaultValue);
#else
        return PlayerPrefs.GetInt(KEY_TOUCH_ENABLED, defaultValue ? 1 : 0) == 1;
#endif
    }

    public static void SetPhysBoneEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveBool(KEY_PHYSBONE_ENABLED, enabled);
#else
        PlayerPrefs.SetInt(KEY_PHYSBONE_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
#endif
    }

    public static bool GetPhysBoneEnabled(bool defaultValue = true)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadBool(KEY_PHYSBONE_ENABLED, defaultValue);
#else
        return PlayerPrefs.GetInt(KEY_PHYSBONE_ENABLED, defaultValue ? 1 : 0) == 1;
#endif
    }

    public static void SetLogLevel(int level)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveInt(KEY_LOG_LEVEL, level);
#else
        PlayerPrefs.SetInt(KEY_LOG_LEVEL, level);
        PlayerPrefs.Save();
#endif
    }

    public static int GetLogLevel(int defaultValue = 0)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadInt(KEY_LOG_LEVEL, defaultValue);
#else
        return PlayerPrefs.GetInt(KEY_LOG_LEVEL, defaultValue);
#endif
    }

    public static void SetFpsOverlayEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveBool(KEY_FPS_OVERLAY_ENABLED, enabled);
#else
        PlayerPrefs.SetInt(KEY_FPS_OVERLAY_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
#endif
    }

    public static bool GetFpsOverlayEnabled(bool defaultValue = true)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadBool(KEY_FPS_OVERLAY_ENABLED, defaultValue);
#else
        return PlayerPrefs.GetInt(KEY_FPS_OVERLAY_ENABLED, defaultValue ? 1 : 0) == 1;
#endif
    }

    public static void SetModelOffsetX(float value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveFloat(KEY_MODEL_OFFSET_X, value);
#else
        PlayerPrefs.SetFloat(KEY_MODEL_OFFSET_X, value);
        PlayerPrefs.Save();
#endif
    }

    public static float GetModelOffsetX(float defaultValue = 0.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadFloat(KEY_MODEL_OFFSET_X, defaultValue);
#else
        return PlayerPrefs.GetFloat(KEY_MODEL_OFFSET_X, defaultValue);
#endif
    }

    public static void SetModelOffsetY(float value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveFloat(KEY_MODEL_OFFSET_Y, value);
#else
        PlayerPrefs.SetFloat(KEY_MODEL_OFFSET_Y, value);
        PlayerPrefs.Save();
#endif
    }

    public static float GetModelOffsetY(float defaultValue = 0.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadFloat(KEY_MODEL_OFFSET_Y, defaultValue);
#else
        return PlayerPrefs.GetFloat(KEY_MODEL_OFFSET_Y, defaultValue);
#endif
    }

    public static void SetModelOffsetZ(float value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveFloat(KEY_MODEL_OFFSET_Z, value);
#else
        PlayerPrefs.SetFloat(KEY_MODEL_OFFSET_Z, value);
        PlayerPrefs.Save();
#endif
    }

    public static float GetModelOffsetZ(float defaultValue = 0.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadFloat(KEY_MODEL_OFFSET_Z, defaultValue);
#else
        return PlayerPrefs.GetFloat(KEY_MODEL_OFFSET_Z, defaultValue);
#endif
    }

    public static void SetModelScale(float value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SaveFloat(KEY_MODEL_SCALE, value);
#else
        PlayerPrefs.SetFloat(KEY_MODEL_SCALE, value);
        PlayerPrefs.Save();
#endif
    }

    public static float GetModelScale(float defaultValue = 1.0f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return LoadFloat(KEY_MODEL_SCALE, defaultValue);
#else
        return PlayerPrefs.GetFloat(KEY_MODEL_SCALE, defaultValue);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void SaveFloat(string key, float value)
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putFloat", key, value);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save float {key}: {e.Message}");
        }
    }

    private static float LoadFloat(string key, float defaultValue)
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<float>("getFloat", key, defaultValue);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load float {key}: {e.Message}");
        }

        return defaultValue;
    }

    private static void SaveInt(string key, int value)
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putInt", key, value);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save int {key}: {e.Message}");
        }
    }

    private static int LoadInt(string key, int defaultValue)
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<int>("getInt", key, defaultValue);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load int {key}: {e.Message}");
        }

        return defaultValue;
    }

    private static void SaveBool(string key, bool value)
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    using (AndroidJavaObject editor = prefs.Call<AndroidJavaObject>("edit"))
                    {
                        editor.Call<AndroidJavaObject>("putBoolean", key, value);
                        editor.Call<bool>("commit");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save bool {key}: {e.Message}");
        }
    }

    private static bool LoadBool(string key, bool defaultValue)
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
            {
                if (context != null)
                {
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", PREFS_NAME, 0x0004))
                    {
                        return prefs.Call<bool>("getBoolean", key, defaultValue);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load bool {key}: {e.Message}");
        }

        return defaultValue;
    }
#endif
}
