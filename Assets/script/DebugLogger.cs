using System;
using System.IO;
using UnityEngine;

/// <summary>
/// ログをファイルとConsoleの両方に出力する統一ロガー
/// </summary>
public static class DebugLogger
{
    private static string logDirectory = "";
    
    /// <summary>
    /// ログディレクトリを初期化
    /// </summary>
    static DebugLogger()
    {
#if UNITY_EDITOR
        logDirectory = Application.dataPath + "/../Logs";
#else
        logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
#endif
        
        // ディレクトリが存在しなければ作成
        try
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create log directory: {e.Message}");
        }
    }
    
    /// <summary>
    /// 新しいログファイルを初期化
    /// </summary>
    /// <param name="filename">ファイル名（例: "vrm_loader.log"）</param>
    public static void InitLog(string filename)
    {
        string filePath = GetLogPath(filename);
        
        try
        {
            string header = $"========== {filename} ==========\n";
            header += $"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            header += $"Unity Version: {Application.unityVersion}\n";
            header += $"Platform: {Application.platform}\n";
            header += "========================================\n\n";
            
            File.WriteAllText(filePath, header);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize log file: {e.Message}");
        }
    }
    
    /// <summary>
    /// ログを出力（INFO レベル）
    /// </summary>
    public static void Log(string filename, string message)
    {
        WriteLog(filename, "INFO", message);
        Debug.Log(message);
    }
    
    /// <summary>
    /// 警告ログを出力（WARNING レベル）
    /// </summary>
    public static void LogWarning(string filename, string message)
    {
        WriteLog(filename, "WARN", message);
        Debug.LogWarning(message);
    }
    
    /// <summary>
    /// エラーログを出力（ERROR レベル）
    /// </summary>
    public static void LogError(string filename, string message)
    {
        WriteLog(filename, "ERROR", message);
        Debug.LogError(message);
    }
    
    /// <summary>
    /// 例外ログを出力
    /// </summary>
    public static void LogException(string filename, Exception exception)
    {
        string message = $"EXCEPTION: {exception.Message}\nStackTrace:\n{exception.StackTrace}";
        WriteLog(filename, "ERROR", message);
        Debug.LogException(exception);
    }
    
    /// <summary>
    /// ログファイルにメッセージを書き込む
    /// </summary>
    private static void WriteLog(string filename, string level, string message)
    {
        string filePath = GetLogPath(filename);
        
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"[{timestamp}] [{level}] {message}\n";
            File.AppendAllText(filePath, logLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write log: {e.Message}");
        }
    }
    
    /// <summary>
    /// ログファイルのフルパスを取得
    /// </summary>
    private static string GetLogPath(string filename)
    {
        return Path.Combine(logDirectory, filename);
    }
    
    /// <summary>
    /// 区切り線をログに出力
    /// </summary>
    public static void LogSeparator(string filename, string title = "")
    {
        string separator = "========================================";
        if (!string.IsNullOrEmpty(title))
        {
            separator = $"===== {title} =====";
        }
        WriteLog(filename, "INFO", separator);
    }
}
