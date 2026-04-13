using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using System.IO;

public class SettingsManagerV2 : MonoBehaviour
{
    [Header("UI References")]
    public Button selectVRMButton;
    public Button setWallpaperButton;
    public Button reloadWallpaperButton;
    public Text currentVRMText;
    
    [Header("Preview")]
    public Camera previewCamera;
    public RuntimeAnimatorController animatorController;
    
    [Header("Panels")]
    public GameObject settingsPanel;
    public Button previewButton;
    public Button backButton;
    
    [Header("Camera Settings")]
    public Slider distanceSlider;
    public Text distanceValueText;
    public Slider heightSlider;
    public Text heightValueText;
    public Slider angleSlider;
    public Text angleValueText;
    
    [Header("Background Color Settings")]
    public ColorPicker colorPicker;
    
    [Header("Background Image Settings")]
    public Button selectImageButton;
    public Button resetToColorButton;
    public Text backgroundModeText;
    
    [Header("Editor Test Settings")]
    
    [Header("Background Image Adjustment")]
    public GameObject imageAdjustmentPanel;  // 画像調整パネル(画像選択時のみ表示)
    public Dropdown fitModeDropdown;
    public Slider offsetXSlider;
    public Text offsetXValueText;
    public Slider offsetYSlider;
    public Text offsetYValueText;
    public Slider scaleSlider;
    public Text scaleValueText;
    public Button resetAdjustmentButton;
    public string testImagePath = "";
    
    private const string LOG_FILE = "settings_manager_v2.log";
    
    private GameObject currentVRM;
    private BackgroundManagerV2 backgroundManager;
    private VRMPositionController vrmPositionController;
    private ExpressionManager expressionManager;
    private ColliderVisualizer colliderVisualizer;
    private bool isFullPreview = false;
    private string selectedVrmName = "None";
    private string lastTouchDebugText = "Tap preview to inspect hit areas";
    
    // ボタン連打防止
    private float lastReloadTime = 0f;
    private const float RELOAD_COOLDOWN = 3.0f; // 3秒間のクールダウン
    
    void Start()
    {
        // ログ初期化
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.LogSeparator(LOG_FILE, "SettingsManagerV2 Start");
        
        // カメラを原点に固定
        if (previewCamera != null)
        {
            previewCamera.transform.position = Vector3.zero;
            previewCamera.transform.rotation = Quaternion.identity;
            DebugLogger.Log(LOG_FILE, "Preview camera fixed at origin (0,0,0)");
        }
        
        // 共通クラスの初期化
        backgroundManager = new BackgroundManagerV2(previewCamera);
        
        // ボタンイベント設定
        selectVRMButton.onClick.AddListener(OnSelectVRMClicked);
        setWallpaperButton.onClick.AddListener(OnSetWallpaperClicked);
        reloadWallpaperButton.onClick.AddListener(OnReloadWallpaperClicked);
        previewButton.onClick.AddListener(OnPreviewTapped);
        backButton.onClick.AddListener(OnBackButtonClicked);
        
        selectImageButton.onClick.AddListener(OnSelectImageClicked);
        resetToColorButton.onClick.AddListener(OnResetToColorClicked);
        
        // 画像調整スライダーのイベント設定
        if (fitModeDropdown != null) fitModeDropdown.onValueChanged.AddListener(OnFitModeChanged);
        if (offsetXSlider != null) offsetXSlider.onValueChanged.AddListener(OnOffsetXChanged);
        if (offsetYSlider != null) offsetYSlider.onValueChanged.AddListener(OnOffsetYChanged);
        if (scaleSlider != null) scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        if (resetAdjustmentButton != null) resetAdjustmentButton.onClick.AddListener(OnResetAdjustmentClicked);
        
        // カメラスライダーのイベント設定
        distanceSlider.onValueChanged.AddListener(OnDistanceChanged);
        heightSlider.onValueChanged.AddListener(OnHeightChanged);
        angleSlider.onValueChanged.AddListener(OnAngleChanged);
        
        // カラーピッカーの設定
        if (colorPicker != null)
        {
            colorPicker.onColorChanged += OnBackgroundColorChanged;
            DebugLogger.Log(LOG_FILE, "ColorPicker connected");
        }
        else
        {
            DebugLogger.LogError(LOG_FILE, "ColorPicker is NULL");
        }
        
        // 初期状態
        backButton.gameObject.SetActive(false);
        
        // 遅延読み込み
        StartCoroutine(LoadSettingsDelayed());
    }
    
    void Update()
    {
        // タッチ判定（プレビューVRMがあり、フルプレビューモードの時のみ）
        if (Input.GetMouseButtonDown(0))
        {
            DebugLogger.Log(LOG_FILE, $"Mouse clicked - currentVRM: {currentVRM != null}, isFullPreview: {isFullPreview}");
            
            if (currentVRM != null && isFullPreview)
            {
                CheckVRMTouch(Input.mousePosition);
            }
            else
            {
                if (currentVRM == null)
                {
                    DebugLogger.LogWarning(LOG_FILE, "VRM not loaded yet");
                }
                if (!isFullPreview)
                {
                    DebugLogger.LogWarning(LOG_FILE, "Not in full preview mode");
                }
            }
        }
    }
    
    void CheckVRMTouch(Vector2 touchPos)
    {
        if (previewCamera == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "Preview camera is null");
            return;
        }
        
        if (expressionManager == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "ExpressionManager is null, VRM may not be loaded yet");
            return;
        }
        
        Physics.SyncTransforms();

        // タッチ位置からRayを飛ばす
        Ray ray = previewCamera.ScreenPointToRay(touchPos);
        RaycastHit hit;
        
        // Raycastで判定
        if (Physics.Raycast(ray, out hit, 100f))
        {
            DebugLogger.Log(LOG_FILE, $"Hit: {hit.collider.gameObject.name}");

            colliderVisualizer?.Highlight(hit.collider);

            if (TryResolveBodyPart(hit.collider, out string bodyPart))
            {
                lastTouchDebugText = $"Hit {bodyPart}: {hit.collider.gameObject.name}";
                DebugLogger.Log(LOG_FILE, $"Body part touched: {bodyPart}");
                ChangeExpressionByBodyPart(bodyPart);
            }
            else
            {
                lastTouchDebugText = $"Hit collider: {hit.collider.gameObject.name}";
                DebugLogger.Log(LOG_FILE, "Hit VRM but no VRMBodyPart component");
            }

            UpdatePreviewStatusText();
        }
        else
        {
            lastTouchDebugText = "Miss: tap a collider";
            UpdatePreviewStatusText();
        }
    }
    
    void ChangeExpressionByBodyPart(string bodyPart)
    {
        if (expressionManager == null) return;
        
        DebugLogger.Log(LOG_FILE, $"Changing expression for body part: {bodyPart}");
        
        switch (bodyPart)
        {
            case "Head":
                expressionManager.ChangeExpression("Joy", 5f);
                break;
                
            case "Body":
                expressionManager.ChangeExpression("Fun", 5f);
                break;

            case "Hand":
                expressionManager.ChangeExpression("Blink", 2.5f);
                break;
                
            default:
                DebugLogger.LogWarning(LOG_FILE, $"Unknown body part: {bodyPart}");
                break;
        }
    }
    
    IEnumerator LoadSettingsDelayed()
    {
        DebugLogger.LogSeparator(LOG_FILE, "LoadSettingsDelayed START");
        
        yield return new WaitForSeconds(0.5f);
        
        // カメラ設定を読み込み
        LoadCameraSettings();
        
        // カラーピッカーに保存済み色を設定
        if (colorPicker != null)
        {
            Color savedColor = PrefsHelper.GetBackgroundColor();
            colorPicker.SetColor(savedColor);
            DebugLogger.Log(LOG_FILE, $"ColorPicker initialized with saved color: R={savedColor.r:F2}, G={savedColor.g:F2}, B={savedColor.b:F2}");
        }
        
        // 背景を適用
        backgroundManager.ApplyBackground();
        
        // 背景モード表示を更新
        UpdateBackgroundModeText();
        
        // 画像調整設定を読み込み
        LoadImageAdjustmentSettings();
        
        // VRMパスを表示
        string savedPath = PrefsHelper.GetVRMPath();
        SetSelectedVrmName(savedPath);
        UpdatePreviewStatusText();
        DebugLogger.Log(LOG_FILE, $"Saved VRM path: {savedPath}");
        
        // VRMプレビュー読み込み
        LoadVRMPreviewAsync(savedPath);
        
        DebugLogger.LogSeparator(LOG_FILE, "LoadSettingsDelayed END");
    }
    
    // ===== 背景画像選択 =====
    void OnSelectImageClicked()
    {
        DebugLogger.Log(LOG_FILE, "Select image button clicked");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("OpenImagePicker");
#else
        if (!string.IsNullOrEmpty(testImagePath) && System.IO.File.Exists(testImagePath))
        {
            OnBackgroundImageSelected(testImagePath);
        }
        else
        {
            DebugLogger.LogError(LOG_FILE, "testImagePath not set or file not found");
        }
#endif
    }
    
    public void OnBackgroundImageSelected(string path)
    {
        DebugLogger.Log(LOG_FILE, $"Image selected: {path}");
        
        PrefsHelper.SetBackgroundImagePath(path);
        PrefsHelper.SetBackgroundMode(1);
        
        backgroundManager.ApplyBackground();
        UpdateBackgroundModeText();
        UpdateImageAdjustmentUI();
    }
    
    void OnResetToColorClicked()
    {
        DebugLogger.Log(LOG_FILE, "Reset to color button clicked");
        
        PrefsHelper.SetBackgroundMode(0);
        backgroundManager.ApplyBackground();
        UpdateBackgroundModeText();
        UpdateImageAdjustmentUI();
    }
    
    void UpdateBackgroundModeText()
    {
        int bgMode = PrefsHelper.GetBackgroundMode();
        
        if (bgMode == 0)
        {
            backgroundModeText.text = "背景: 単色";
        }
        else
        {
            string imagePath = PrefsHelper.GetBackgroundImagePath();
            backgroundModeText.text = $"背景: 画像 ({System.IO.Path.GetFileName(imagePath)})";
        }
        
        UpdateImageAdjustmentUI();
    }
    
    // ===== カメラ設定読み込み =====
    void LoadCameraSettings()
    {
        float distance = PrefsHelper.GetCameraDistance(3.0f);
        float height = PrefsHelper.GetCameraHeight(0.5f);
        float angle = PrefsHelper.GetCameraAngle(0.0f);
        
        DebugLogger.Log(LOG_FILE, $"Camera settings loaded - Distance: {distance}, Height: {height}, Angle: {angle}");
        
        distanceSlider.value = distance;
        heightSlider.value = height;
        angleSlider.value = angle;
        
        distanceValueText.text = distance.ToString("F1");
        heightValueText.text = height.ToString("F1");
        angleValueText.text = angle.ToString("F0") + "°";
    }
    
    // ===== カメラスライダー変更イベント =====
    void OnDistanceChanged(float value)
    {
        distanceValueText.text = value.ToString("F1");
        PrefsHelper.SetCameraDistance(value);
        UpdateVRMPosition();
    }
    
    void OnHeightChanged(float value)
    {
        heightValueText.text = value.ToString("F1");
        PrefsHelper.SetCameraHeight(value);
        UpdateVRMPosition();
    }
    
    void OnAngleChanged(float value)
    {
        angleValueText.text = value.ToString("F0") + "°";
        PrefsHelper.SetCameraAngle(value);
        UpdateVRMPosition();
    }
    
    void UpdateVRMPosition()
    {
        if (vrmPositionController != null)
        {
            float distance = distanceSlider.value;
            float height = heightSlider.value;
            float angle = angleSlider.value;
            
            vrmPositionController.UpdateVRMPosition(distance, height, angle);
        }
    }
    
    // ===== カラーピッカー変更イベント =====
    void OnBackgroundColorChanged(Color color)
    {
        DebugLogger.Log(LOG_FILE, $"Background color changed: R={color.r:F2}, G={color.g:F2}, B={color.b:F2}");
        
        PrefsHelper.SetBackgroundColor(color);
        
        // 常にカメラに適用（単色モード・画像モード共通で余白色として機能）
        if (previewCamera != null)
        {
            previewCamera.backgroundColor = color;
        }
    }
    
    // ===== プレビュー切り替え =====
    public void OnPreviewTapped()
    {
        if (isFullPreview) return;
        
        isFullPreview = true;
        settingsPanel.SetActive(false);
        previewButton.interactable = false;
        backButton.gameObject.SetActive(true);
        SetColliderDebugVisible(true);
        
        DebugLogger.Log(LOG_FILE, "Full preview mode activated");
    }
    
    public void OnBackButtonClicked()
    {
        isFullPreview = false;
        settingsPanel.SetActive(true);
        previewButton.interactable = true;
        backButton.gameObject.SetActive(false);
        SetColliderDebugVisible(false);
        UpdatePreviewStatusText();
        
        DebugLogger.Log(LOG_FILE, "Settings mode activated");
    }
    
    // ===== VRM選択 =====
    void OnSelectVRMClicked()
    {
        DebugLogger.Log(LOG_FILE, "Select VRM button clicked");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("OpenFilePicker");
#else
        DebugLogger.Log(LOG_FILE, "File picker not available in editor");
        currentVRMText.text = "エディタではファイル選択不可";
#endif
    }
    
    public void OnVRMSelected(string path)
    {
        DebugLogger.Log(LOG_FILE, $"Avatar selected: {path}");
        
        PrefsHelper.SetVRMPath(path);
        SetSelectedVrmName(path);
        lastTouchDebugText = "Tap preview to inspect hit areas";
        UpdatePreviewStatusText();
        
        LoadVRMPreviewAsync(path);
    }
    
    // ===== VRMプレビュー読み込み =====
    async void LoadVRMPreviewAsync(string path)
    {
        DebugLogger.LogSeparator(LOG_FILE, "LoadVRMPreviewAsync START");
        
        // 既存のVRMを削除
        if (currentVRM != null)
        {
            Destroy(currentVRM);
            currentVRM = null;
            expressionManager = null;
            colliderVisualizer = null;
            DebugLogger.Log(LOG_FILE, "Previous VRM and ExpressionManager cleared");
        }
        
        await Task.Delay(100);
        
        if (!System.IO.File.Exists(path))
        {
            DebugLogger.LogError(LOG_FILE, $"VRM file not found: {path}");
            return;
        }
        
        // VRM読み込み
        string sourceType = AvatarLoaderHelper.IsVrmPath(path) ? "VRM" : "AssetBundle";
        currentVRM = await AvatarLoaderHelper.LoadAvatarAsync(path);
        
        if (currentVRM == null)
        {
            DebugLogger.LogError(LOG_FILE, $"Failed to load {sourceType}");
            return;
        }
        
        // VRM位置コントローラーを初期化
        vrmPositionController = new VRMPositionController(currentVRM, Vector3.zero);

        VRMLoaderHelper.SetupAnimator(currentVRM);

        if (animatorController != null)
        {
            Animator animator = currentVRM.GetComponent<Animator>();
            if (animator == null)
            {
                animator = currentVRM.GetComponentInChildren<Animator>(true);
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
                DebugLogger.Log(LOG_FILE, "Preview animator controller applied");
            }
        }
        
        // 表情管理を初期化
        expressionManager = new ExpressionManager(currentVRM, this);
        DebugLogger.Log(LOG_FILE, "ExpressionManager initialized");
        
        // Colliderをセットアップ（タッチ判定用）
        VRMColliderSetup.SetupColliders(currentVRM);
        DebugLogger.Log(LOG_FILE, "VRM Colliders setup completed");

        colliderVisualizer = currentVRM.GetComponent<ColliderVisualizer>();
        if (colliderVisualizer == null)
        {
            colliderVisualizer = currentVRM.AddComponent<ColliderVisualizer>();
        }

        colliderVisualizer.Rebuild();
        colliderVisualizer.SetVisible(isFullPreview);
        lastTouchDebugText = "Tap preview to inspect hit areas";
        UpdatePreviewStatusText();
        
        // VRM位置を設定
        UpdateVRMPosition();
        
        DebugLogger.LogSeparator(LOG_FILE, $"{sourceType} preview loaded successfully");
    }
    
    // ===== 壁紙に設定 =====
    void OnSetWallpaperClicked()
    {
        DebugLogger.Log(LOG_FILE, "Set wallpaper button clicked");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            currentActivity.Call("OpenWallpaperPicker");
            DebugLogger.Log(LOG_FILE, "OpenWallpaperPicker called");
        }
        catch (System.Exception e)
        {
            DebugLogger.LogException(LOG_FILE, e);
        }
#else
        DebugLogger.Log(LOG_FILE, "Wallpaper picker not available in editor");
#endif
    }
    
    // ===== 壁紙を更新 =====
    void OnReloadWallpaperClicked()
    {
        // クールダウンチェック
        if (Time.time - lastReloadTime < RELOAD_COOLDOWN)
        {
            float remaining = RELOAD_COOLDOWN - (Time.time - lastReloadTime);
            currentVRMText.text = $"待機中... ({remaining:F0}秒)";
            DebugLogger.Log(LOG_FILE, $"Reload button clicked too soon. Cooldown remaining: {remaining:F1}s");
            return;
        }
        
        lastReloadTime = Time.time;
        
        DebugLogger.Log(LOG_FILE, "Reload wallpaper button clicked");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            currentActivity.Call("NotifyWallpaperReload");
            
            currentVRMText.text = "壁紙が更新されました！";
            StartCoroutine(ResetStatusText());
            
            DebugLogger.Log(LOG_FILE, "Wallpaper reload notification sent");
        }
        catch (System.Exception e)
        {
            DebugLogger.LogException(LOG_FILE, e);
            currentVRMText.text = "壁紙更新エラー";
        }
#else
        DebugLogger.Log(LOG_FILE, "Wallpaper reload not available in editor");
        currentVRMText.text = "エディタでは動作しません";
#endif
    }
    
    IEnumerator ResetStatusText()
    {
        yield return new WaitForSeconds(2.0f);
        UpdatePreviewStatusText();
    }

    // ===== 画像調整 =====
    
    void LoadImageAdjustmentSettings()
    {
        int fitMode = PrefsHelper.GetBackgroundImageFitMode();
        float offsetX = PrefsHelper.GetBackgroundImageOffsetX();
        float offsetY = PrefsHelper.GetBackgroundImageOffsetY();
        float scale = PrefsHelper.GetBackgroundImageScale();
        
        if (fitModeDropdown != null) fitModeDropdown.value = fitMode;
        if (offsetXSlider != null) offsetXSlider.value = offsetX;
        if (offsetYSlider != null) offsetYSlider.value = offsetY;
        if (scaleSlider != null) scaleSlider.value = scale;
        
        UpdateImageAdjustmentUI();
        
        DebugLogger.Log(LOG_FILE, $"Image adjustment loaded - FitMode: {fitMode}, OffsetX: {offsetX}, OffsetY: {offsetY}, Scale: {scale}");
    }
    
    void UpdateImageAdjustmentUI()
    {
        if (offsetXValueText != null) offsetXValueText.text = offsetXSlider.value.ToString("F2");
        if (offsetYValueText != null) offsetYValueText.text = offsetYSlider.value.ToString("F2");
        if (scaleValueText != null) scaleValueText.text = scaleSlider.value.ToString("F2");
        
        // 画像調整パネルの表示/非表示
        int bgMode = PrefsHelper.GetBackgroundMode();
        if (imageAdjustmentPanel != null)
        {
            imageAdjustmentPanel.SetActive(bgMode == 1);
        }
    }
    
    void OnFitModeChanged(int value)
    {
        PrefsHelper.SetBackgroundImageFitMode(value);
        backgroundManager.UpdateImageAdjustment();
        DebugLogger.Log(LOG_FILE, $"Fit mode changed: {value}");
    }
    
    void OnOffsetXChanged(float value)
    {
        if (offsetXValueText != null) offsetXValueText.text = value.ToString("F2");
        PrefsHelper.SetBackgroundImageOffsetX(value);
        backgroundManager.UpdateImageAdjustment();
    }
    
    void OnOffsetYChanged(float value)
    {
        if (offsetYValueText != null) offsetYValueText.text = value.ToString("F2");
        PrefsHelper.SetBackgroundImageOffsetY(value);
        backgroundManager.UpdateImageAdjustment();
    }
    
    void OnScaleChanged(float value)
    {
        if (scaleValueText != null) scaleValueText.text = value.ToString("F2");
        PrefsHelper.SetBackgroundImageScale(value);
        backgroundManager.UpdateImageAdjustment();
    }
    
    void OnResetAdjustmentClicked()
    {
        PrefsHelper.SetBackgroundImageFitMode(1);  // Fill
        PrefsHelper.SetBackgroundImageOffsetX(0.0f);
        PrefsHelper.SetBackgroundImageOffsetY(0.0f);
        PrefsHelper.SetBackgroundImageScale(1.0f);
        
        LoadImageAdjustmentSettings();
        backgroundManager.UpdateImageAdjustment();
        
        DebugLogger.Log(LOG_FILE, "Image adjustment reset to default");
    }

    bool TryResolveBodyPart(Collider collider, out string bodyPart)
    {
        bodyPart = null;

        if (collider == null)
        {
            return false;
        }

        VRMBodyPart taggedBodyPart = collider.GetComponent<VRMBodyPart>();
        if (taggedBodyPart == null)
        {
            taggedBodyPart = collider.GetComponentInParent<VRMBodyPart>();
        }

        if (taggedBodyPart != null && !string.IsNullOrEmpty(taggedBodyPart.bodyPartName))
        {
            bodyPart = taggedBodyPart.bodyPartName;
            return true;
        }

        if (currentVRM == null || !collider.transform.IsChildOf(currentVRM.transform))
        {
            return false;
        }

        Transform current = collider.transform;
        while (current != null && current != currentVRM.transform.parent)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("hand") || name.Contains("wrist") || name.Contains("thumb") ||
                name.Contains("index") || name.Contains("middle") || name.Contains("ring") ||
                name.Contains("little"))
            {
                bodyPart = "Hand";
                return true;
            }

            if (name.Contains("head") || name.Contains("face") || name.Contains("neck"))
            {
                bodyPart = "Head";
                return true;
            }

            if (name.Contains("chest") || name.Contains("spine") || name.Contains("hips") || name.Contains("body"))
            {
                bodyPart = "Body";
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void SetSelectedVrmName(string path)
    {
        selectedVrmName = string.IsNullOrWhiteSpace(path) ? "None" : Path.GetFileName(path);
    }

    void SetColliderDebugVisible(bool visible)
    {
        if (colliderVisualizer == null)
        {
            return;
        }

        colliderVisualizer.SetVisible(visible);
    }

    void UpdatePreviewStatusText()
    {
        if (currentVRMText == null)
        {
            return;
        }

        currentVRMText.text = $"VRM: {selectedVrmName}\nTouch: {lastTouchDebugText}";
    }

}
