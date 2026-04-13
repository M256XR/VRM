using UnityEngine;
using System.IO;

/// <summary>
/// 背景管理クラス V2(カメラ固定方式)
/// 背景Quadはカメラの後ろに固定配置
/// </summary>
public class BackgroundManagerV2
{
    private const string LOG_FILE = "background_manager_v2.log";
    
    private GameObject backgroundQuad;
    private Camera targetCamera;
    
    // フィットモード定義
    public enum FitMode
    {
        Fit = 0,      // 画面に収める(余白が出る可能性あり)
        Fill = 1,     // 画面を覆う(はみ出す可能性あり)
        Stretch = 2,  // 引き伸ばし(アスペクト比無視)
        Custom = 3    // 手動調整
    }
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="camera">対象カメラ</param>
    public BackgroundManagerV2(Camera camera)
    {
        targetCamera = camera;
        
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.Log(LOG_FILE, "BackgroundManagerV2 initialized");
    }
    
    /// <summary>
    /// 背景を適用(単色または画像)
    /// </summary>
    public void ApplyBackground()
    {
        if (targetCamera == null)
        {
            DebugLogger.LogError(LOG_FILE, "Target camera is null");
            return;
        }
        
        int bgMode = PrefsHelper.GetBackgroundMode();
        DebugLogger.Log(LOG_FILE, $"Background mode: {bgMode} ({(bgMode == 0 ? "Color" : "Image")})");
        
        if (bgMode == 0)
        {
            // 単色モード
            ApplyColorBackground();
        }
        else
        {
            // 画像モード
            string imagePath = PrefsHelper.GetBackgroundImagePath();
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                ApplyImageBackground(imagePath);
            }
            else
            {
                DebugLogger.LogWarning(LOG_FILE, $"Image not found: {imagePath}, fallback to color mode");
                ApplyColorBackground();
            }
        }
    }
    
    /// <summary>
    /// 単色背景を適用
    /// </summary>
    private void ApplyColorBackground()
    {
        Color bgColor = PrefsHelper.GetBackgroundColor();
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = bgColor;
        
        DebugLogger.Log(LOG_FILE, $"Color background applied: R={bgColor.r:F2}, G={bgColor.g:F2}, B={bgColor.b:F2}");
        
        // 既存のQuadを削除
        DestroyBackgroundQuad();
    }
    
    /// <summary>
    /// 画像背景を適用
    /// </summary>
    private void ApplyImageBackground(string imagePath)
    {
        DebugLogger.Log(LOG_FILE, $"Loading image: {imagePath}");
        
        try
        {
            // 画像読み込み
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(imageBytes);
            
            if (!loaded)
            {
                DebugLogger.LogError(LOG_FILE, "Failed to load texture from image bytes");
                ApplyColorBackground();
                return;
            }
            
            DebugLogger.Log(LOG_FILE, $"Image loaded: {texture.width}x{texture.height}");
            
            // 既存のQuadを削除
            DestroyBackgroundQuad();
            
            // 新しいQuadを作成
            CreateBackgroundQuad(texture);
            
            // カメラ設定
            targetCamera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = null;
            targetCamera.backgroundColor = PrefsHelper.GetBackgroundColor(); // 余白色として適用
            
            DebugLogger.Log(LOG_FILE, "Image background applied successfully");
        }
        catch (System.Exception e)
        {
            DebugLogger.LogException(LOG_FILE, e);
            ApplyColorBackground();
        }
    }
    
    /// <summary>
    /// 背景Quadを作成(カメラの後ろに固定配置)
    /// </summary>
    private void CreateBackgroundQuad(Texture2D texture)
    {
        backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backgroundQuad.name = "BackgroundQuad";
        
        // Collider削除
        Collider collider = backgroundQuad.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
        
        // Material設定
        Material material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = texture;
        backgroundQuad.GetComponent<Renderer>().material = material;
        
        // カメラの後ろに固定配置
        SetupBackgroundQuad(texture);
    }
    
    /// <summary>
    /// 背景Quadの位置・回転・スケールを設定
    /// カメラの後ろの壁として固定
    /// </summary>
    private void SetupBackgroundQuad(Texture2D texture)
    {
        if (backgroundQuad == null || targetCamera == null)
        {
            return;
        }
        
        // 設定を読み込み
        int fitMode = PrefsHelper.GetBackgroundImageFitMode();
        float offsetX = PrefsHelper.GetBackgroundImageOffsetX();
        float offsetY = PrefsHelper.GetBackgroundImageOffsetY();
        float scaleMultiplier = PrefsHelper.GetBackgroundImageScale();
        
        DebugLogger.Log(LOG_FILE, $"Image adjustment - FitMode: {fitMode}, OffsetX: {offsetX}, OffsetY: {offsetY}, Scale: {scaleMultiplier}");
        
        // カメラの前方10mに配置(固定)
        float distance = 10f;
        Vector3 basePosition = new Vector3(0, 0, distance);
        
        // オフセット適用
        Vector3 position = basePosition + new Vector3(offsetX, offsetY, 0);
        backgroundQuad.transform.position = position;
        
        // 回転なし(そのまま)
        backgroundQuad.transform.rotation = Quaternion.identity;
        
        // スケール設定
        Vector3 scale = CalculateQuadScale(texture, (FitMode)fitMode, distance);
        backgroundQuad.transform.localScale = scale * scaleMultiplier;
        
        DebugLogger.Log(LOG_FILE, $"Background quad setup - Pos: {backgroundQuad.transform.position}, Rot: {backgroundQuad.transform.rotation.eulerAngles}, Scale: {backgroundQuad.transform.localScale}");
    }
    
    /// <summary>
    /// フィットモードに応じたQuadのスケールを計算
    /// </summary>
    private Vector3 CalculateQuadScale(Texture2D texture, FitMode mode, float distance)
    {
        float imageAspect = (float)texture.width / texture.height;
        
        // カメラのFOVを考慮した画面サイズ計算
        float fov = targetCamera.fieldOfView;
        float screenHeight = 2f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        float screenWidth = screenHeight * targetCamera.aspect;
        float screenAspect = targetCamera.aspect;
        
        float width, height;
        
        switch (mode)
        {
            case FitMode.Fit:
                // 画面に収める(余白が出る可能性あり)
                if (imageAspect > screenAspect)
                {
                    // 画像の方が横長 → 幅を基準
                    width = screenWidth;
                    height = screenWidth / imageAspect;
                }
                else
                {
                    // 画像の方が縦長 → 高さを基準
                    height = screenHeight;
                    width = screenHeight * imageAspect;
                }
                break;
                
            case FitMode.Fill:
                // 画面を覆う(はみ出す可能性あり)
                if (imageAspect > screenAspect)
                {
                    // 画像の方が横長 → 高さを基準
                    height = screenHeight;
                    width = screenHeight * imageAspect;
                }
                else
                {
                    // 画像の方が縦長 → 幅を基準
                    width = screenWidth;
                    height = screenWidth / imageAspect;
                }
                break;
                
            case FitMode.Stretch:
                // 引き伸ばし(アスペクト比無視)
                width = screenWidth;
                height = screenHeight;
                break;
                
            case FitMode.Custom:
                // Customモードでは基本的にFillと同じだが、手動調整を前提
                if (imageAspect > screenAspect)
                {
                    height = screenHeight;
                    width = screenHeight * imageAspect;
                }
                else
                {
                    width = screenWidth;
                    height = screenWidth / imageAspect;
                }
                break;
                
            default:
                // デフォルトはFill
                if (imageAspect > screenAspect)
                {
                    height = screenHeight;
                    width = screenHeight * imageAspect;
                }
                else
                {
                    width = screenWidth;
                    height = screenWidth / imageAspect;
                }
                break;
        }
        
        DebugLogger.Log(LOG_FILE, $"Calculated scale - Mode: {mode}, Width: {width:F2}, Height: {height:F2}");
        
        return new Vector3(width, height, 1f);
    }
    
    /// <summary>
    /// 背景画像の調整を更新(既存のQuadに対して適用)
    /// </summary>
    public void UpdateImageAdjustment()
    {
        if (backgroundQuad == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "No background quad to update");
            return;
        }
        
        Texture2D texture = backgroundQuad.GetComponent<Renderer>().material.mainTexture as Texture2D;
        if (texture == null)
        {
            DebugLogger.LogError(LOG_FILE, "Background quad has no texture");
            return;
        }
        
        SetupBackgroundQuad(texture);
        DebugLogger.Log(LOG_FILE, "Image adjustment updated");
    }
    
    /// <summary>
    /// 背景Quadを削除
    /// </summary>
    public void DestroyBackgroundQuad()
    {
        if (backgroundQuad != null)
        {
            Object.Destroy(backgroundQuad);
            backgroundQuad = null;
            DebugLogger.Log(LOG_FILE, "Background quad destroyed");
        }
    }
    
    /// <summary>
    /// 背景Quadが存在するか
    /// </summary>
    public bool HasBackgroundQuad()
    {
        return backgroundQuad != null;
    }
}