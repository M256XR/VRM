using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public const string EditorWallpaperPlayModePrefsKey = "EditorWallpaperPlayMode";

    private string logFilePath;

    void Awake()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, "scene_controller_log.txt");
        try
        {
            File.WriteAllText(logFilePath, "========== SceneController Log ==========\n");
        }
        catch
        {
        }

        LogToFile("SceneController Awake");
        StartCoroutine(CheckAndTransition());
    }

    IEnumerator CheckAndTransition()
    {
#if UNITY_EDITOR
        yield return null;
        if (PlayerPrefs.GetInt(EditorWallpaperPlayModePrefsKey, 0) == 1)
        {
            LogToFile("Editor wallpaper test mode detected. Staying in MainScene.");
            yield break;
        }

        LogToFile("Editor runtime detected. Loading SettingsScene.");
        SceneManager.LoadScene("SettingsScene");
#else
        LogToFile("Android runtime detected. Staying in MainScene.");
        yield break;
#endif
    }

    void LogToFile(string message)
    {
        try
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            File.AppendAllText(logFilePath, $"[{timestamp}] {message}\n");
        }
        catch
        {
        }
    }
}
