using UnityEngine;

/// <summary>
/// VRMの位置制御クラス（カメラ固定、VRMを動かす方式）
/// </summary>
public class VRMPositionController
{
    private const string LOG_FILE = "vrm_position_controller.log";
    
    private GameObject vrmRoot;
    private Vector3 centerPosition; // VRMの回転中心点
    
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="vrm">制御対象のVRM</param>
    /// <param name="center">回転の中心位置（通常はVector3.zero）</param>
    public VRMPositionController(GameObject vrm, Vector3 center = default)
    {
        vrmRoot = vrm;
        centerPosition = center;
        
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.Log(LOG_FILE, "VRMPositionController initialized");
    }
    
    /// <summary>
    /// VRMの位置を設定から更新
    /// </summary>
    public void UpdateVRMPosition()
    {
        if (vrmRoot == null)
        {
            DebugLogger.LogError(LOG_FILE, "VRM root is null");
            return;
        }
        
        float distance = PrefsHelper.GetCameraDistance(3.0f);
        float height = PrefsHelper.GetCameraHeight(0.5f);
        float angle = PrefsHelper.GetCameraAngle(0.0f);
        
        UpdateVRMPosition(distance, height, angle);
    }
    
    /// <summary>
    /// VRMの位置を更新
    /// </summary>
    /// <param name="distance">カメラからの距離</param>
    /// <param name="height">高さ</param>
    /// <param name="angle">水平角度（度）</param>
    public void UpdateVRMPosition(float distance, float height, float angle)
    {
        if (vrmRoot == null)
        {
            return;
        }
        
        // VRMの位置（カメラ原点から距離distance、高さheight）
        Vector3 position = centerPosition + new Vector3(0, height, distance);
        vrmRoot.transform.position = position;
        
        // VRMをその場でY軸回転（初期値180度 + angle）
        // 180度でカメラ側を向く
        vrmRoot.transform.rotation = Quaternion.Euler(0, 180 + angle, 0);
        
        DebugLogger.Log(LOG_FILE, $"VRM position updated - Pos: {position}, Rotation: Y={180 + angle}°, Distance: {distance}, Height: {height}");
    }
    
    /// <summary>
    /// VRMを設定
    /// </summary>
    public void SetVRM(GameObject vrm)
    {
        vrmRoot = vrm;
        DebugLogger.Log(LOG_FILE, $"VRM set: {(vrm != null ? vrm.name : "null")}");
    }
    
    /// <summary>
    /// 回転中心を設定
    /// </summary>
    public void SetCenterPosition(Vector3 center)
    {
        centerPosition = center;
        DebugLogger.Log(LOG_FILE, $"Center position set: {center}");
    }
}