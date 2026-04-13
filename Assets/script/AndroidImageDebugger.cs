using UnityEngine;
using System.IO;

/// <summary>
/// Android実機での画像読み込みデバッグ用
/// </summary>
public class AndroidImageDebugger : MonoBehaviour
{
    private string logPath;
    
    void Start()
    {
        logPath = Path.Combine(Application.persistentDataPath, "image_debug.log");
        File.WriteAllText(logPath, "===== Image Debug Log =====\n");
        
        Log("AndroidImageDebugger Start");
        
        // SharedPreferencesから画像パスを読み込み
        string imagePath = GetImagePathFromPrefs();
        Log($"Image path from prefs: {imagePath}");
        
        // ファイル存在確認
        bool exists = File.Exists(imagePath);
        Log($"File exists: {exists}");
        
        if (!exists)
        {
            Log("ERROR: File does not exist");
            
            // ディレクトリ確認
            string dir = Path.GetDirectoryName(imagePath);
            Log($"Directory: {dir}");
            
            if (Directory.Exists(dir))
            {
                Log("Directory exists, listing files:");
                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    Log($"  - {file}");
                }
            }
            else
            {
                Log("ERROR: Directory does not exist");
            }
            
            return;
        }
        
        // ファイルサイズ確認
        FileInfo fileInfo = new FileInfo(imagePath);
        Log($"File size: {fileInfo.Length} bytes");
        
        // 読み込みテスト
        try
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            Log($"Read {imageBytes.Length} bytes successfully");
            
            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(imageBytes);
            
            Log($"Texture.LoadImage result: {loaded}");
            
            if (loaded)
            {
                Log($"Texture size: {texture.width}x{texture.height}");
                Log($"Texture format: {texture.format}");
            }
            else
            {
                Log("ERROR: Texture.LoadImage failed");
            }
        }
        catch (System.Exception e)
        {
            Log($"EXCEPTION: {e.Message}");
            Log($"StackTrace: {e.StackTrace}");
        }
        
        Log("===== Debug Complete =====");
    }
    
    string GetImagePathFromPrefs()
    {
        try
        {
            using (AndroidJavaClass contextHolderClass = new AndroidJavaClass("com.oreoreooooooo.VRM.ContextHolder"))
            {
                using (AndroidJavaObject context = contextHolderClass.CallStatic<AndroidJavaObject>("getContext"))
                {
                    if (context == null)
                    {
                        Log("ERROR: Context is null");
                        return "";
                    }
                    
                    using (AndroidJavaObject prefs = context.Call<AndroidJavaObject>("getSharedPreferences", "VRMWallpaperPrefs", 0x0004))
                    {
                        string path = prefs.Call<string>("getString", "bgImagePath", "");
                        return path;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Log($"EXCEPTION in GetImagePathFromPrefs: {e.Message}");
            return "";
        }
    }
    
    void Log(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        string logMessage = $"[{timestamp}] {message}\n";
        
        try
        {
            File.AppendAllText(logPath, logMessage);
        }
        catch { }
        
        Debug.Log(message);
    }
}
