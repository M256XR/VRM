using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public sealed class FpsMonitor : MonoBehaviour
{
    private const string LOG_FILE = "fps_monitor.log";
    private const float WarmupSeconds = 3f;
    private const float MeasurementSeconds = 180f;
    private const float WindowSeconds = 1f;
    private const float RollingStatsSeconds = 10f;
    private const float LogIntervalSeconds = 1f;

    private struct WindowSample
    {
        public float ElapsedTime;
        public float Fps;
    }

    private static FpsMonitor instance;

    private string label = "Unknown";
    private float warmupRemaining;
    private float measuredTime;
    private float windowTime;
    private float nextLogTime;
    private int measuredFrames;
    private int windowFrames;
    private int framesOver33Ms;
    private int framesOver50Ms;
    private int framesOver100Ms;
    private int skippedResumeFrames;
    private float minWindowFps;
    private float maxWindowFps;
    private float lastWindowFps;
    private float maxFrameMs;
    private bool skipNextFrameAfterResume;
    private readonly List<float> frameTimesMs = new List<float>(4096);
    private readonly Queue<WindowSample> recentWindowSamples = new Queue<WindowSample>(16);
    private bool measuring;
    private bool finalLogged;
    private GUIStyle overlayStyle;

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
        framesOver33Ms = 0;
        framesOver50Ms = 0;
        framesOver100Ms = 0;
        skippedResumeFrames = 0;
        minWindowFps = float.MaxValue;
        maxWindowFps = 0f;
        lastWindowFps = 0f;
        maxFrameMs = 0f;
        skipNextFrameAfterResume = false;
        frameTimesMs.Clear();
        recentWindowSamples.Clear();
        measuring = true;
        finalLogged = false;

        DebugLogger.LogSeparator(LOG_FILE, "FPS Measurement START");
        DebugLogger.Log(LOG_FILE, $"Label: {label}");
        DebugLogger.Log(LOG_FILE, $"WarmupSeconds={WarmupSeconds:F1} MeasurementSeconds={MeasurementSeconds:F1} WindowSeconds={WindowSeconds:F1} RollingStatsSeconds={RollingStatsSeconds:F1}");
        DebugLogger.Log(LOG_FILE, $"GraphicsDevice={SystemInfo.graphicsDeviceType} GraphicsDeviceName={SystemInfo.graphicsDeviceName}");
        DebugLogger.Log(LOG_FILE, $"Resolution={Screen.width}x{Screen.height} refreshRate={Screen.currentResolution.refreshRate}");
        DebugLogger.Log(LOG_FILE, $"QualityLevel={QualitySettings.GetQualityLevel()} vSyncCount={QualitySettings.vSyncCount} targetFrameRate={Application.targetFrameRate}");
        DebugLogger.Log(LOG_FILE, $"Memory managed={FormatBytes(GC.GetTotalMemory(false))} allocated={FormatBytes(Profiler.GetTotalAllocatedMemoryLong())} reserved={FormatBytes(Profiler.GetTotalReservedMemoryLong())}");
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

        if (skipNextFrameAfterResume)
        {
            skipNextFrameAfterResume = false;
            skippedResumeFrames++;
            DebugLogger.Log(LOG_FILE, $"Skipped resume frame: deltaMs={deltaTime * 1000f:F1}");
            return;
        }

        if (warmupRemaining > 0f)
        {
            warmupRemaining -= deltaTime;
            return;
        }

        measuredTime += deltaTime;
        measuredFrames++;
        float frameMs = deltaTime * 1000f;
        frameTimesMs.Add(frameMs);
        maxFrameMs = Mathf.Max(maxFrameMs, frameMs);
        if (frameMs > 33.333f)
        {
            framesOver33Ms++;
        }
        if (frameMs > 50f)
        {
            framesOver50Ms++;
        }
        if (frameMs > 100f)
        {
            framesOver100Ms++;
        }

        windowTime += deltaTime;
        windowFrames++;

        if (windowTime >= WindowSeconds)
        {
            lastWindowFps = windowFrames / windowTime;
            EnqueueRecentWindowSample(lastWindowFps);
            CalculateRecentStats(out _, out minWindowFps, out maxWindowFps);
            windowTime = 0f;
            windowFrames = 0;
            PushStatsToAndroid();
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

        if (!PrefsHelper.GetFpsOverlayEnabled(true))
        {
            return;
        }

        if (overlayStyle == null)
        {
            overlayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24
            };
            overlayStyle.normal.textColor = Color.white;
        }

        CalculateRecentStats(out float avgFps, out float minFps, out float maxFps);
        string currentText = lastWindowFps > 0f ? lastWindowFps.ToString("F1") : "--";
        string avgText = avgFps > 0f ? avgFps.ToString("F1") : "--";
        string minText = minFps < float.MaxValue ? minFps.ToString("F1") : "--";
        string maxText = maxFps > 0f ? maxFps.ToString("F1") : "--";
        string text = warmupRemaining > 0f
            ? $"FPS warmup {Mathf.CeilToInt(warmupRemaining)}s\n{label}"
            : $"FPS cur {currentText} avg {avgText} min {minText} max {maxText}\n{label}";

        GUI.Label(new Rect(16, 48, Screen.width - 32, 120), text, overlayStyle);
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
        CalculateRecentStats(out float avgFps, out float minFps, out float maxFps);
        float p95FrameMs = GetPercentileFrameMs(0.95f);
        float p99FrameMs = GetPercentileFrameMs(0.99f);
        return $"Label={label} elapsed={measuredTime:F1}s rollingWindow={RollingStatsSeconds:F1}s currentFps={lastWindowFps:F1} avgFps={avgFps:F1} minFps={minFps:F1} maxFps={maxFps:F1} frames={measuredFrames} p95FrameMs={p95FrameMs:F1} p99FrameMs={p99FrameMs:F1} maxFrameMs={maxFrameMs:F1} framesOver33ms={framesOver33Ms} framesOver50ms={framesOver50Ms} framesOver100ms={framesOver100Ms} skippedResumeFrames={skippedResumeFrames} managedMem={FormatBytes(GC.GetTotalMemory(false))} allocMem={FormatBytes(Profiler.GetTotalAllocatedMemoryLong())} reservedMem={FormatBytes(Profiler.GetTotalReservedMemoryLong())}";
    }

    private void PushStatsToAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CalculateRecentStats(out float avgFps, out float minFps, out float maxFps);
        string currentText = lastWindowFps > 0f ? $"{lastWindowFps:F1}" : "--";
        string avgText = avgFps > 0f ? $"{avgFps:F1}" : "--";
        string minText = minFps < float.MaxValue ? $"{minFps:F1}" : "--";
        string maxText = maxFps > 0f ? $"{maxFps:F1}" : "--";
        string info = warmupRemaining > 0f
            ? $"Warmup: {Mathf.CeilToInt(warmupRemaining)}s\nLabel: {label}"
            : $"Current: {currentText} fps\nAverage: {avgText} fps\nMin: {minText} fps\nMax: {maxText} fps\nMax ms: {maxFrameMs:F1}";

        try
        {
            using (AndroidJavaClass mainActivity = new AndroidJavaClass("com.oreoreooooooo.VRM.MainActivity"))
            {
                mainActivity.CallStatic("updateFpsInfoFromUnity", info);
            }
        }
        catch
        {
        }
#endif
    }

    private void EnqueueRecentWindowSample(float fps)
    {
        recentWindowSamples.Enqueue(new WindowSample
        {
            ElapsedTime = measuredTime,
            Fps = fps
        });

        while (recentWindowSamples.Count > 0 &&
               measuredTime - recentWindowSamples.Peek().ElapsedTime > RollingStatsSeconds)
        {
            recentWindowSamples.Dequeue();
        }
    }

    private void CalculateRecentStats(out float avgFps, out float minFps, out float maxFps)
    {
        avgFps = 0f;
        minFps = float.MaxValue;
        maxFps = 0f;

        if (recentWindowSamples.Count == 0)
        {
            return;
        }

        float sum = 0f;
        int count = 0;
        foreach (WindowSample sample in recentWindowSamples)
        {
            sum += sample.Fps;
            minFps = Mathf.Min(minFps, sample.Fps);
            maxFps = Mathf.Max(maxFps, sample.Fps);
            count++;
        }

        if (count > 0)
        {
            avgFps = sum / count;
        }
    }

    private float GetPercentileFrameMs(float percentile)
    {
        if (frameTimesMs.Count == 0)
        {
            return 0f;
        }

        List<float> sorted = new List<float>(frameTimesMs);
        sorted.Sort();
        int index = Mathf.Clamp(Mathf.CeilToInt(percentile * sorted.Count) - 1, 0, sorted.Count - 1);
        return sorted[index];
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        DebugLogger.Log(LOG_FILE, $"ApplicationFocus changed: {hasFocus}");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        DebugLogger.Log(LOG_FILE, $"ApplicationPause changed: {pauseStatus}");
        if (!pauseStatus)
        {
            skipNextFrameAfterResume = true;
            windowTime = 0f;
            windowFrames = 0;
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double mb = 1024.0 * 1024.0;
        return $"{bytes / mb:F1}MB";
    }
}
