using UnityEngine;

/// <summary>
/// SettingsScene用のタッチデバッグスクリプト
/// 空のGameObjectにアタッチして使用
/// </summary>
public class DebugSettingsTouchTest : MonoBehaviour
{
    public Camera targetCamera;
    
    void Start()
    {
        Debug.Log("===== DebugSettingsTouchTest START =====");
        
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("Camera is NULL!");
        }
        else
        {
            Debug.Log($"Camera found: {targetCamera.name}");
        }
        
        // VRMを探す
        var vrmProxy = GameObject.FindObjectOfType<VRM.VRMBlendShapeProxy>();
        if (vrmProxy != null)
        {
            Debug.Log($"VRM found: {vrmProxy.gameObject.name}");
            
            // Colliderを確認
            var colliders = vrmProxy.GetComponentsInChildren<Collider>();
            Debug.Log($"Colliders found: {colliders.Length}");
            foreach (var col in colliders)
            {
                Debug.Log($"  - {col.gameObject.name}: {col.GetType().Name}");
                
                var bodyPart = col.GetComponent<VRMBodyPart>();
                if (bodyPart != null)
                {
                    Debug.Log($"    -> VRMBodyPart: {bodyPart.bodyPartName}");
                }
            }
        }
        else
        {
            Debug.LogWarning("VRM not found!");
        }
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"===== MOUSE CLICKED at {Input.mousePosition} =====");
            
            if (targetCamera == null)
            {
                Debug.LogError("Camera is NULL!");
                return;
            }
            
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            Debug.Log($"Ray: Origin={ray.origin}, Direction={ray.direction}");
            
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                Debug.Log($"HIT: {hit.collider.gameObject.name}");
                Debug.Log($"  Position: {hit.point}");
                Debug.Log($"  Distance: {hit.distance}");
                
                var bodyPart = hit.collider.GetComponent<VRMBodyPart>();
                if (bodyPart != null)
                {
                    Debug.Log($"  VRMBodyPart: {bodyPart.bodyPartName}");
                }
                else
                {
                    Debug.Log("  No VRMBodyPart component");
                }
            }
            else
            {
                Debug.Log("MISS: No hit");
            }
        }
    }
}
