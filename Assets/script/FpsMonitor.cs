using System;
using UnityEngine;

public sealed class FpsMonitor : MonoBehaviour
{
    private const string LOG_FILE = "fps_monitor.log";
    private const float WarmupSeconds = 3f;
    private const float MeasurementSeconds = 30f;
    private const float WindowSeconds = 1f;
    private const float LogIntervalSeconds = 5f;

    private static FpsMonitor instance;

    private string label = "Unknown";
    private float warmupRemaining;
    private float measuredTime;
    private float windowTime;
    private float nextLogTime;
    private int measuredFrames;
    private int windowFrames;
    private float minWindowFps;
    private float maxWindowFps;
    private float lastWindowFps;
    private bool measuring;
    private bool finalLogged;

    public static void Begin(string measurementLabel)
    {
        if (instance == null)
        {
            GameObject monitorObject = new GameObject("FpsMonitor");
            DontDestroyOnLoad(monitorObject);
            instance = monitorObject.AddComponent<FpsMonitor>();
        }

        instance.ResetMeasurement(measurementLabel);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void ResetMeasurement(string measurementLabel)
    {
        DebugLogger.InitLog(LOG_FILE);
        label = string.IsNullOrWhiteSpace(measurementLabel) ? "Unknown" : measurementLabel;
        warmupRemaining = WarmupSeconds;
        measuredTime = 0f;
        windowTime = 0f;
        nextLogTime = LogIntervalSeconds;
        measuredFrames = 0;
        windowFrames = 0;
        minWindowFps = float.MaxValue;
        maxWindowFps = 0f;
        lastWindowFps = 0f;
        measuring = true;
        finalLogged = false;

        DebugLogger.LogSeparator(LOG_FILE, "FPS Measurement START");
        DebugLogger.Log(LOG_FILE, $"Label: {label}");
        DebugLogger.Log(LOG_FILE, $"WarmupSeconds={WarmupSeconds:F1} MeasurementSeconds={MeasurementSeconds:F1} WindowSeconds={WindowSeconds:F1}");
        DebugLogger.Log(LOG_FILE, $"GraphicsDevice={SystemInfo.graphicsDeviceType} GraphicsDeviceName={SystemInfo.graphicsDeviceName}");
        DebugLogger.Log(LOG_FILE, $"Resolution={Screen.width}x{Screen.height} refreshRate={Screen.currentResolution.refreshRate}");
        DebugLogger.Log(LOG_FILE, $"QualityLevel={QualitySettings.GetQualityLevel()} vSyncCount={QualitySettings.vSyncCount} targetFrameRate={Application.targetFrameRate}");
    }

    private void Update()
    {
        if (!measuring)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        if (warmupRemaining > 0f)
        {
            warmupRemaining -= deltaTime;
            return;
        }

        measuredTime += deltaTime;
        measuredFrames++;
        windowTime += deltaTime;
        windowFrames++;

        if (windowTime >= WindowSeconds)
        {
            lastWindowFps = windowFrames / windowTime;
            minWindowFps = Mathf.Min(minWindowFps, lastWindowFps);
            maxWindowFps = Mathf.Max(maxWindowFps, lastWindowFps);
            windowTime = 0f;
            windowFrames = 0;
        }

        if (measuredTime >= nextLogTime)
        {
            LogSnapshot("FPS Measurement SAMPLE");
            nextLogTime += LogIntervalSeconds;
        }

        if (measuredTime >= MeasurementSeconds)
        {
            LogFinal();
        }
    }

    private void OnGUI()
    {
        if (!measuring)
        {
            return;
        }

        float avgFps = measuredTime > 0f ? measuredFrames / measuredTime : 0f;
        string minText = minWindowFps < float.MaxValue ? minWindowFps.ToString("F1") : "--";
        string text = warmupRemaining > 0f
            ? $"FPS warmup {Mathf.CeilToInt(warmupRemaining)}s\n{label}"
            : $"FPS avg {avgFps:F1} min {minText} max {maxWindowFps:F1}\n{label}";

        GUI.Label(new Rect(16, 16, Screen.width - 32, 80), text);
    }

    private void LogSnapshot(string title)
    {
        DebugLogger.LogSeparator(LOG_FILE, title);
        DebugLogger.Log(LOG_FILE, BuildSummaryLine());
    }

    private void LogFinal()
    {
        if (finalLogged)
        {
            return;
        }

        finalLogged = true;
        measuring = false;
        LogSnapshot("FPS Measurement END");
    }

    private string BuildSummaryLine()
    {
        float avgFps = measuredTime > 0f ? measuredFrames / measuredTime : 0f;
        float minFps = minWindowFps < float.MaxValue ? minWindowFps : 0f;
        return $"Label={label} elapsed={measuredTime:F1}s avgFps={avgFps:F1} minFps={minFps:F1} maxFps={maxWindowFps:F1} lastWindowFps={lastWindowFps:F1} frames={measuredFrames}";
    }
}
