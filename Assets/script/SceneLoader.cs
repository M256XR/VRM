using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class SceneLoader : MonoBehaviour
{
    private string logFilePath;
    
    void Start()
    {
        // ⭐ ログファイル初期化
        logFilePath = Path.Combine(Application.persistentDataPath, "scene_loader_log.txt");
        try
        {
            File.WriteAllText(logFilePath, "========== SceneLoader Log ==========\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize scene loader log: {e.Message}");
        }
        
        string currentScene = SceneManager.GetActiveScene().name;
        
        LogToFile($"SceneLoader Start - Current scene: {currentScene}");
        Debug.Log($"SceneLoader Start - Current scene: {currentScene}");
        
        // ⭐ 壁紙フラグをPlayerPrefsでチェック
        bool isWallpaperMode = PlayerPrefs.GetInt("IsWallpaperMode", 0) == 1;
        LogToFile($"IsWallpaperMode from PlayerPrefs: {isWallpaperMode}");
        
        if (isWallpaperMode)
        {
            LogToFile("Wallpaper mode detected - Loading MainScene");
            Debug.Log("Wallpaper mode detected - Loading MainScene");
            
            // フラグをリセット
            PlayerPrefs.SetInt("IsWallpaperMode", 0);
            PlayerPrefs.Save();
            
            SceneManager.LoadScene("MainScene");
        }
        else
        {
            LogToFile("App mode - Staying in SettingsScene");
            Debug.Log("App mode - Staying in SettingsScene");
        }
    }
    
    void LogToFile(string message)
    {
        try
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}\n";
            File.AppendAllText(logFilePath, logMessage);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write log: {e.Message}");
        }
    }
}
