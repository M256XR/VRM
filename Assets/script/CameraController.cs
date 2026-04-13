using UnityEngine;

/// <summary>
/// カメラ制御の共通処理を提供するクラス
/// </summary>
public class CameraController
{
    private const string LOG_FILE = "camera_controller.log";
    
    private Camera targetCamera;
    private Transform vrmSpawnPoint;
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public CameraController(Camera camera, Transform spawnPoint)
    {
        targetCamera = camera;
        vrmSpawnPoint = spawnPoint;
        
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.Log(LOG_FILE, "CameraController initialized");
    }
    
    /// <summary>
    /// カメラ設定を適用
    /// </summary>
    public void ApplyCameraSettings()
    {
        if (targetCamera == null || vrmSpawnPoint == null)
        {
            DebugLogger.LogError(LOG_FILE, "Camera or VRMSpawnPoint is null");
            return;
        }
        
        float distance = PrefsHelper.GetCameraDistance(3.0f);
        float height = PrefsHelper.GetCameraHeight(0.5f);
        float angle = PrefsHelper.GetCameraAngle(0.0f);
        
        DebugLogger.Log(LOG_FILE, $"Applying camera settings - Distance: {distance}, Height: {height}, Angle: {angle}°");
        
        UpdateCameraPosition(distance, height, angle);
    }
    
    /// <summary>
    /// カメラ位置を更新
    /// </summary>
    public void UpdateCameraPosition(float distance, float height, float angle)
    {
        if (targetCamera == null || vrmSpawnPoint == null)
        {
            return;
        }
        
        // 角度をラジアンに変換
        float angleRad = angle * Mathf.Deg2Rad;
        
        // VRMを中心に円周上に配置
        Vector3 offset = new Vector3(
            Mathf.Sin(angleRad) * distance,
            height,
            Mathf.Cos(angleRad) * distance
        );
        
        targetCamera.transform.position = vrmSpawnPoint.position + offset;
        targetCamera.transform.LookAt(vrmSpawnPoint.position + Vector3.up * height);
        
        DebugLogger.Log(LOG_FILE, $"Camera position: {targetCamera.transform.position}");
        DebugLogger.Log(LOG_FILE, $"Camera looking at: {vrmSpawnPoint.position + Vector3.up * height}");
    }
    
    /// <summary>
    /// カメラ情報をログ出力
    /// </summary>
    public void LogCameraInfo()
    {
        if (targetCamera == null)
        {
            DebugLogger.LogError(LOG_FILE, "Camera is null");
            return;
        }
        
        DebugLogger.LogSeparator(LOG_FILE, "Camera Info");
        DebugLogger.Log(LOG_FILE, $"Position: {targetCamera.transform.position}");
        DebugLogger.Log(LOG_FILE, $"Rotation: {targetCamera.transform.rotation.eulerAngles}");
        DebugLogger.Log(LOG_FILE, $"Clear Flags: {targetCamera.clearFlags}");
        DebugLogger.Log(LOG_FILE, $"Background Color: {targetCamera.backgroundColor}");
        DebugLogger.Log(LOG_FILE, $"Culling Mask: {targetCamera.cullingMask}");
        DebugLogger.Log(LOG_FILE, $"Depth: {targetCamera.depth}");
    }
}
