using UnityEngine;

public class VRMPositionController
{
    private const string LOG_FILE = "vrm_position_controller.log";

    private GameObject vrmRoot;
    private Vector3 centerPosition;

    public VRMPositionController(GameObject vrm, Vector3 center = default)
    {
        vrmRoot = vrm;
        centerPosition = center;

        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.Log(LOG_FILE, "VRMPositionController initialized");
    }

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

    public void UpdateVRMPosition(float distance, float height, float angle)
    {
        if (vrmRoot == null)
        {
            return;
        }

        float offsetX = PrefsHelper.GetModelOffsetX();
        float offsetY = PrefsHelper.GetModelOffsetY();
        float offsetZ = PrefsHelper.GetModelOffsetZ();
        float modelScale = PrefsHelper.GetModelScale(1.0f);

        Vector3 position = centerPosition + new Vector3(offsetX, height + offsetY, distance + offsetZ);
        vrmRoot.transform.position = position;
        vrmRoot.transform.rotation = Quaternion.Euler(0, 180 + angle, 0);
        vrmRoot.transform.localScale = Vector3.one * modelScale;

        DebugLogger.Log(
            LOG_FILE,
            $"VRM position updated - Pos: {position}, Rotation: Y={180 + angle}deg, Distance: {distance}, Height: {height}, Offset=({offsetX:F2},{offsetY:F2},{offsetZ:F2}), Scale={modelScale:F2}");
    }

    public void SetVRM(GameObject vrm)
    {
        vrmRoot = vrm;
        DebugLogger.Log(LOG_FILE, $"VRM set: {(vrm != null ? vrm.name : "null")}");
    }

    public void SetCenterPosition(Vector3 center)
    {
        centerPosition = center;
        DebugLogger.Log(LOG_FILE, $"Center position set: {center}");
    }
}
