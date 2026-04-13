using UnityEngine;
using System.IO;

/// <summary>
/// 背景画像表示のデバッグ用スクリプト
/// SettingsSceneの空のGameObjectにアタッチして使用
/// </summary>
public class BackgroundImageDebugger : MonoBehaviour
{
    public Camera targetCamera;
    public string testImagePath = ""; // Inspectorで設定
    
    private GameObject testQuad;
    
    void Start()
    {
        Debug.Log("===== BackgroundImageDebugger START =====");
        
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (string.IsNullOrEmpty(testImagePath))
        {
            Debug.LogError("testImagePath is not set in Inspector!");
            return;
        }
        
        Debug.Log($"Test image path: {testImagePath}");
        Debug.Log($"File exists: {File.Exists(testImagePath)}");
        
        if (!File.Exists(testImagePath))
        {
            Debug.LogError($"File not found: {testImagePath}");
            return;
        }
        
        CreateTestQuad();
    }
    
    void CreateTestQuad()
    {
        Debug.Log("===== CreateTestQuad START =====");
        
        try
        {
            // 画像読み込み
            byte[] imageBytes = File.ReadAllBytes(testImagePath);
            Debug.Log($"Image bytes loaded: {imageBytes.Length} bytes");
            
            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(imageBytes);
            
            Debug.Log($"Texture.LoadImage result: {loaded}");
            
            if (!loaded)
            {
                Debug.LogError("Failed to load texture from bytes");
                return;
            }
            
            Debug.Log($"Texture loaded: {texture.width}x{texture.height}");
            
            // Quad作成
            testQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            testQuad.name = "TestBackgroundQuad";
            
            Debug.Log("Quad created");
            
            // Collider削除
            Collider collider = testQuad.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
                Debug.Log("Collider removed");
            }
            
            // Material設定
            Material material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = texture;
            testQuad.GetComponent<Renderer>().material = material;
            
            Debug.Log("Material applied");
            
            // カメラの前方10mに配置
            testQuad.transform.position = new Vector3(0, 0, 10);
            testQuad.transform.rotation = Quaternion.identity;
            
            // スケール設定
            float aspect = (float)texture.width / texture.height;
            float distance = 10f;
            float fov = targetCamera.fieldOfView;
            float height = 2f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float width = height * aspect;
            
            testQuad.transform.localScale = new Vector3(width * 1.2f, height * 1.2f, 1f);
            
            Debug.Log($"Quad position: {testQuad.transform.position}");
            Debug.Log($"Quad rotation: {testQuad.transform.rotation.eulerAngles}");
            Debug.Log($"Quad scale: {testQuad.transform.localScale}");
            
            // カメラ設定
            targetCamera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = null;
            
            Debug.Log($"Camera clear flags: {targetCamera.clearFlags}");
            Debug.Log($"Camera position: {targetCamera.transform.position}");
            Debug.Log($"Camera rotation: {targetCamera.transform.rotation.eulerAngles}");
            
            // Renderer情報
            Renderer renderer = testQuad.GetComponent<Renderer>();
            Debug.Log($"Renderer enabled: {renderer.enabled}");
            Debug.Log($"Renderer layer: {testQuad.layer}");
            Debug.Log($"Material: {renderer.material.name}");
            Debug.Log($"MainTexture: {renderer.material.mainTexture != null}");
            
            Debug.Log("===== CreateTestQuad SUCCESS =====");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EXCEPTION: {e.Message}");
            Debug.LogError($"StackTrace: {e.StackTrace}");
        }
    }
    
    void Update()
    {
        // スペースキーで情報を再表示
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (testQuad != null)
            {
                Debug.Log("===== Current Quad Info =====");
                Debug.Log($"Quad active: {testQuad.activeSelf}");
                Debug.Log($"Quad position: {testQuad.transform.position}");
                Debug.Log($"Quad rotation: {testQuad.transform.rotation.eulerAngles}");
                Debug.Log($"Quad scale: {testQuad.transform.localScale}");
                
                Renderer renderer = testQuad.GetComponent<Renderer>();
                Debug.Log($"Renderer enabled: {renderer.enabled}");
                
                Debug.Log($"Camera position: {targetCamera.transform.position}");
                Debug.Log($"Camera clear flags: {targetCamera.clearFlags}");
            }
            else
            {
                Debug.LogWarning("testQuad is null");
            }
        }
    }
}
