using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRM;
using VRC.SDK3.Dynamics.PhysBone.Components;

public class VRMLoaderV2 : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string vrmFileName = "avatar.vrm";
    [SerializeField] private Text statusText;

    private const string LOG_FILE = "vrm_loader_v2.log";
    private GameObject currentVRM;
    private VRMBlendShapeProxy blendShapeProxy;
    private BackgroundManagerV2 backgroundManager;
    private VRMPositionController vrmPositionController;
    private bool isLoading = false;
    private PhysBoneTouchHandler physBoneTouchHandler;
    private int baseScreenWidth;
    private int baseScreenHeight;
    private bool hasBaseScreenResolution;

    async void Start()
    {
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.LogSeparator(LOG_FILE, "VRMLoaderV2 Start");
        VrcSdkRuntimeDynamicsBootstrap.EnsureInitialized();

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != "MainScene")
        {
            DebugLogger.Log(LOG_FILE, $"Wrong scene detected: {currentScene}. Loading MainScene...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
            return;
        }

        DebugLogger.Log(LOG_FILE, $"Scene: {currentScene}");

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = Vector3.zero;
            mainCamera.transform.rotation = Quaternion.identity;
            DebugLogger.Log(LOG_FILE, "Camera fixed at origin (0,0,0)");
        }
        ApplyWallpaperRenderScale();

        backgroundManager = new BackgroundManagerV2(mainCamera);

        // PhysBone タッチハンドラーを自動追加
        if (GetComponent<PhysBoneTouchHandler>() == null)
        {
            physBoneTouchHandler = gameObject.AddComponent<PhysBoneTouchHandler>();
            DebugLogger.Log(LOG_FILE, "PhysBoneTouchHandler added");
        }
        else
        {
            physBoneTouchHandler = GetComponent<PhysBoneTouchHandler>();
        }

        ApplyRuntimeSettings();

        await Task.Delay(1000);
        await LoadVRMAsync();
    }

    async Task LoadVRMAsync()
    {
        if (isLoading)
        {
            DebugLogger.LogWarning(LOG_FILE, "LoadVRMAsync already in progress, skipping");
            return;
        }

        isLoading = true;
        try
        {
            DebugLogger.LogSeparator(LOG_FILE, "LoadVRMAsync START");

            string avatarPath = GetVRMPath();
            if (string.IsNullOrWhiteSpace(avatarPath))
            {
                UpdateStatus("No avatar selected");
                DebugLogger.LogWarning(LOG_FILE, "Avatar path is empty");
                return;
            }

            string sourceType = AvatarLoaderHelper.IsVrmPath(avatarPath) ? "VRM" : "AssetBundle";
            UpdateStatus($"Loading {sourceType}...\n{avatarPath}");

            if (!System.IO.File.Exists(avatarPath))
            {
                UpdateStatus($"File not found:\n{avatarPath}");
                DebugLogger.LogError(LOG_FILE, $"File not found: {avatarPath}");
                return;
            }

            // 新しいアバターをロードする前に既存アバターを破棄
            // （LoadAvatarAsync 内で LoadedAvatarMarker が付与されるため、
            //   ロード後に DestroyAllAvatars すると新アバターも消えてしまう）
            DestroyAllAvatars();

            GameObject loaded = await AvatarLoaderHelper.LoadAvatarAsync(avatarPath);

            if (loaded == null)
            {
                UpdateStatus($"Failed to load {sourceType}");
                return;
            }

            currentVRM = loaded;

            vrmPositionController = new VRMPositionController(currentVRM, Vector3.zero);
            vrmPositionController.UpdateVRMPosition();
            LogAvatarVisibility(currentVRM);

            blendShapeProxy = currentVRM.GetComponent<VRMBlendShapeProxy>();
            DebugLogger.Log(LOG_FILE, $"BlendShapeProxy: {(blendShapeProxy != null ? "OK" : "NULL")}");

            VRMColliderSetup.SetupColliders(currentVRM);
            VRMLoaderHelper.SetupAnimator(currentVRM);
            backgroundManager.ApplyBackground();
            ApplyRuntimeSettings();
            SendModelInfoToAndroid(currentVRM, sourceType, avatarPath);

            UpdateStatus($"{sourceType} loaded");
            FpsMonitor.Begin($"{sourceType}: {System.IO.Path.GetFileName(avatarPath)}");
            DebugLogger.LogSeparator(LOG_FILE, $"{sourceType} loaded successfully");
        }
        catch (Exception exception)
        {
            UpdateStatus($"Avatar load error:\n{exception.Message}");
            DebugLogger.LogException(LOG_FILE, exception);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void LogAvatarVisibility(GameObject avatarRoot)
    {
        if (avatarRoot == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "Visibility diagnostic skipped: Camera.main is null");
            return;
        }

        Renderer[] renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, "Visibility diagnostic: avatar has no renderers");
            return;
        }

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;
        int visibleByFrustum = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            if (renderer.enabled && renderer.gameObject.activeInHierarchy &&
                GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
            {
                visibleByFrustum++;
            }
        }

        DebugLogger.Log(LOG_FILE,
            $"Visibility diagnostic: cameraPos={camera.transform.position} cameraRot={camera.transform.rotation.eulerAngles} " +
            $"near={camera.nearClipPlane:F2} far={camera.farClipPlane:F2} fov={camera.fieldOfView:F1} cullingMask={camera.cullingMask} " +
            $"avatarPos={avatarRoot.transform.position} avatarRot={avatarRoot.transform.rotation.eulerAngles} avatarScale={avatarRoot.transform.lossyScale} " +
            $"renderers={renderers.Length} frustumVisibleRenderers={visibleByFrustum} boundsCenter={(hasBounds ? combinedBounds.center.ToString() : "none")} " +
            $"boundsSize={(hasBounds ? combinedBounds.size.ToString() : "none")}");
    }

    string GetVRMPath()
    {
#if UNITY_EDITOR
        string defaultPath = Application.dataPath + "/StreamingAssets/" + vrmFileName;
#else
        string defaultPath = "";
#endif
        string path = PrefsHelper.GetVRMPath(defaultPath);
        DebugLogger.Log(LOG_FILE, $"Resolved VRM path: {path}");
        return path;
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        DebugLogger.Log(LOG_FILE, $"Status: {message}");
    }

    private void ApplyWallpaperRenderScale()
    {
        float renderScale = Mathf.Clamp(PrefsHelper.GetRenderScale(1.0f), 0.5f, 1.5f);
        int targetFps = Mathf.Clamp(PrefsHelper.GetTargetFps(30), 15, 60);
        CaptureBaseScreenResolution();

        Application.targetFrameRate = targetFps;
        QualitySettings.resolutionScalingFixedDPIFactor = renderScale;
        ScalableBufferManager.ResizeBuffers(renderScale, renderScale);

        DebugLogger.Log(LOG_FILE,
            $"Wallpaper render scale applied: targetFrameRate={Application.targetFrameRate} " +
            $"vSyncCount={QualitySettings.vSyncCount} " +
            $"renderScale={renderScale:F2} " +
            $"bufferScale={ScalableBufferManager.widthScaleFactor:F2}x{ScalableBufferManager.heightScaleFactor:F2} " +
            $"screen={Screen.width}x{Screen.height}");
    }

    private void CaptureBaseScreenResolution()
    {
        if (hasBaseScreenResolution)
        {
            return;
        }

        int width = Mathf.Max(Screen.width, Screen.currentResolution.width);
        int height = Mathf.Max(Screen.height, Screen.currentResolution.height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        baseScreenWidth = width;
        baseScreenHeight = height;
        hasBaseScreenResolution = true;
        DebugLogger.Log(LOG_FILE, $"Captured base screen resolution: {baseScreenWidth}x{baseScreenHeight}");
    }

    private void ApplyRuntimeSettings()
    {
        ApplyWallpaperRenderScale();
        ApplyInteractionSettings();

        if (vrmPositionController != null)
        {
            vrmPositionController.UpdateVRMPosition();
        }
    }

    private void ApplyInteractionSettings()
    {
        bool touchEnabled = PrefsHelper.GetTouchEnabled(true);
        bool physBoneEnabled = PrefsHelper.GetPhysBoneEnabled(true);
        bool verboseLogging = PrefsHelper.GetLogLevel(0) > 0;

        if (physBoneTouchHandler == null)
        {
            physBoneTouchHandler = GetComponent<PhysBoneTouchHandler>();
        }

        if (physBoneTouchHandler != null)
        {
            physBoneTouchHandler.enabled = touchEnabled;
            physBoneTouchHandler.SetVerboseLogging(verboseLogging);
        }

        if (currentVRM != null)
        {
            VRCPhysBone[] physBones = currentVRM.GetComponentsInChildren<VRCPhysBone>(true);
            foreach (VRCPhysBone physBone in physBones)
            {
                if (physBone != null)
                {
                    physBone.enabled = physBoneEnabled;
                }
            }
        }

        DebugLogger.Log(LOG_FILE, $"Runtime settings applied: touch={touchEnabled} physBone={physBoneEnabled} verbose={verboseLogging}");
    }

    private void SendModelInfoToAndroid(GameObject avatarRoot, string sourceType, string avatarPath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (avatarRoot == null)
        {
            return;
        }

        int rendererCount = 0;
        int materialSlots = 0;
        int triangleCount = 0;
        int physBoneCount = avatarRoot.GetComponentsInChildren<VRCPhysBone>(true).Length;

        Renderer[] renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
        rendererCount = renderers.Length;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (renderer.sharedMaterials != null)
            {
                materialSlots += renderer.sharedMaterials.Length;
            }

            if (renderer is SkinnedMeshRenderer skinnedRenderer)
            {
                Mesh mesh = skinnedRenderer.sharedMesh;
                if (mesh != null)
                {
                    triangleCount += mesh.triangles.Length / 3;
                }
            }
            else
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    triangleCount += meshFilter.sharedMesh.triangles.Length / 3;
                }
            }
        }

        string info = string.Join("\n", new[]
        {
            $"モデル: {ResolveModelDisplayName(avatarPath)}",
            $"形式: {sourceType}",
            $"Renderer: {rendererCount}",
            $"Material: {materialSlots}",
            $"Triangle: {triangleCount:N0}",
            $"PhysBone: {physBoneCount}"
        });

        try
        {
            using (AndroidJavaClass mainActivity = new AndroidJavaClass("com.oreoreooooooo.VRM.MainActivity"))
            {
                mainActivity.CallStatic("updateModelInfoFromUnity", info);
            }
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LOG_FILE, $"Failed to send model info to Android: {exception.Message}");
        }
#endif
    }

    private string ResolveModelDisplayName(string avatarPath)
    {
        string displayName = PrefsHelper.GetVRMDisplayName(string.Empty);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return System.IO.Path.GetFileName(avatarPath);
    }

    public void SetBlendShape(BlendShapePreset preset, float value)
    {
        if (blendShapeProxy != null)
        {
            var key = BlendShapeKey.CreateFromPreset(preset);
            blendShapeProxy.ImmediatelySetValue(key, value);
            blendShapeProxy.Apply();
        }
    }

    // ===== Real-time settings callbacks (called from Java via UnitySendMessage) =====

    public void OnCameraChanged(string csv)
    {
        if (vrmPositionController == null) return;

        string[] parts = csv.Split(',');
        if (parts.Length == 3 &&
            float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float distance) &&
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float height) &&
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float angle))
        {
            vrmPositionController.UpdateVRMPosition(distance, height, angle);
        }
    }

    public void OnBackgroundColorChanged(string csv)
    {
        string[] parts = csv.Split(',');
        if (parts.Length == 3 &&
            float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r) &&
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g) &&
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = new Color(r, g, b, 1f);
            }
        }
    }

    public void OnBackgroundChanged(string message)
    {
        if (backgroundManager != null)
        {
            backgroundManager.ApplyBackground();
        }
    }

    public void OnImageAdjustmentChanged(string message)
    {
        if (backgroundManager != null && backgroundManager.HasBackgroundQuad())
        {
            backgroundManager.UpdateImageAdjustment();
        }
    }

    public void OnRuntimeSettingsChanged(string message)
    {
        ApplyRuntimeSettings();
    }

    public void ReloadVRM(string message)
    {
        DebugLogger.LogSeparator(LOG_FILE, "ReloadVRM called by BroadcastReceiver");
        DebugLogger.Log(LOG_FILE, $"Message: '{message}'");

        if (isLoading)
        {
            DebugLogger.LogWarning(LOG_FILE, "Load already in progress, ignoring reload request");
            return;
        }

        StartCoroutine(ReloadVRMCoroutine());
    }

    IEnumerator ReloadVRMCoroutine()
    {
        DebugLogger.Log(LOG_FILE, "ReloadVRMCoroutine started");

        DestroyAllAvatars();
        backgroundManager.DestroyBackgroundQuad();

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        DebugLogger.Log(LOG_FILE, "Starting LoadVRMAsync...");
        var task = LoadVRMAsync();

        int waitCount = 0;
        while (!task.IsCompleted)
        {
            waitCount++;
            if (waitCount % 60 == 0)
            {
                DebugLogger.Log(LOG_FILE, $"Waiting for LoadVRMAsync... ({waitCount / 60}s)");
            }
            yield return null;
        }

        DebugLogger.Log(LOG_FILE, $"LoadVRMAsync completed after {waitCount} frames");

        if (task.Exception != null)
        {
            DebugLogger.LogException(LOG_FILE, task.Exception);
        }
        else
        {
            DebugLogger.LogSeparator(LOG_FILE, "ReloadVRM completed successfully");
        }
    }

    private void DestroyAllAvatars()
    {
        // currentVRM だけでなく、シーン内の全アバターを確実に破棄
        LoadedAvatarMarker[] markers = FindObjectsOfType<LoadedAvatarMarker>();
        foreach (LoadedAvatarMarker marker in markers)
        {
            if (marker != null && marker.gameObject != null)
            {
                DebugLogger.Log(LOG_FILE, $"Destroying avatar: {marker.gameObject.name}");
                Destroy(marker.gameObject);
            }
        }

        if (currentVRM != null)
        {
            Destroy(currentVRM);
        }

        currentVRM = null;
        blendShapeProxy = null;
    }
}
