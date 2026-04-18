using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorPerfLogger
{
    private const float CaptureSeconds = 12.0f;
    private const float SampleIntervalSeconds = 0.25f;
    private const string AvatarPathPrefsKey = "vrmPath";
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";

    private static readonly List<Sample> samples = new List<Sample>(128);
    private static double startedAt;
    private static double nextSampleAt;
    private static bool isCapturing;
    private static ProfilerRecorder mainThreadRecorder;
    private static ProfilerRecorder renderThreadRecorder;
    private static string label;

    [MenuItem("Tools/VRM Wallpaper/Start 12s Editor Perf Log")]
    public static void StartCapture()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Editor perf log is intended for Play Mode. Enter Play Mode, load the avatar, then start capture.");
        }

        StopRecorders();

        samples.Clear();
        startedAt = EditorApplication.timeSinceStartup;
        nextSampleAt = startedAt;
        label = BuildLabel();
        isCapturing = true;

        mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread");
        renderThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Render Thread");

        EditorApplication.update -= Update;
        EditorApplication.update += Update;

        Debug.Log($"Editor perf capture started: duration={CaptureSeconds:F1}s interval={SampleIntervalSeconds:F2}s label={label}");
    }

    [MenuItem("Tools/VRM Wallpaper/Select Avatar Path For Play Mode...")]
    public static void SelectAvatarPathForPlayMode()
    {
        string currentPath = PlayerPrefs.GetString(AvatarPathPrefsKey, "");
        string initialDirectory = File.Exists(currentPath) ? Path.GetDirectoryName(currentPath) : Application.dataPath;
        string path = EditorUtility.OpenFilePanel("Select VRM or AssetBundle", initialDirectory, "");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        PlayerPrefs.SetString(AvatarPathPrefsKey, path);
        PlayerPrefs.SetInt(SceneController.EditorWallpaperPlayModePrefsKey, 1);
        PlayerPrefs.SetInt("IsWallpaperMode", 1);
        PlayerPrefs.Save();

        Debug.Log($"Avatar path for Play Mode set: {path}");
    }

    [MenuItem("Tools/VRM Wallpaper/Open MainScene For Avatar Perf Test")]
    public static void OpenMainSceneForAvatarPerfTest()
    {
        PlayerPrefs.SetInt(SceneController.EditorWallpaperPlayModePrefsKey, 1);
        PlayerPrefs.SetInt("IsWallpaperMode", 1);
        PlayerPrefs.Save();

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(MainScenePath);
            Debug.Log("MainScene opened for avatar perf test. Enter Play Mode after selecting an avatar path.");
        }
    }

    [MenuItem("Tools/VRM Wallpaper/Clear Avatar Path For Play Mode")]
    public static void ClearAvatarPathForPlayMode()
    {
        PlayerPrefs.DeleteKey(AvatarPathPrefsKey);
        PlayerPrefs.DeleteKey(SceneController.EditorWallpaperPlayModePrefsKey);
        PlayerPrefs.DeleteKey("IsWallpaperMode");
        PlayerPrefs.Save();

        Debug.Log("Avatar path for Play Mode cleared. VRMLoader will use its default editor path.");
    }

    [MenuItem("Tools/VRM Wallpaper/Stop Editor Perf Log")]
    public static void StopCaptureFromMenu()
    {
        if (!isCapturing)
        {
            Debug.Log("Editor perf capture is not running.");
            return;
        }

        FinishCapture("stopped");
    }

    private static void Update()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now >= nextSampleAt)
        {
            samples.Add(Sample.Capture((float)(now - startedAt), mainThreadRecorder, renderThreadRecorder));
            nextSampleAt = now + SampleIntervalSeconds;
        }

        if (now - startedAt >= CaptureSeconds)
        {
            FinishCapture("completed");
        }
    }

    private static void FinishCapture(string reason)
    {
        isCapturing = false;
        EditorApplication.update -= Update;
        StopRecorders();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "Logs", "EditorPerf");
        Directory.CreateDirectory(outputDirectory);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string safeLabel = SanitizeFileName(label);
        string csvPath = Path.Combine(outputDirectory, $"editor_perf_{safeLabel}_{timestamp}.csv");
        string summaryPath = Path.Combine(outputDirectory, $"editor_perf_{safeLabel}_{timestamp}.summary.txt");

        File.WriteAllText(csvPath, BuildCsv());
        File.WriteAllText(summaryPath, BuildSummary(reason, csvPath));

        Debug.Log($"Editor perf capture {reason}: samples={samples.Count}\nCSV: {csvPath}\nSummary: {summaryPath}");
    }

    private static ProfilerRecorder StartRecorder(ProfilerCategory category, string markerName)
    {
        try
        {
            return ProfilerRecorder.StartNew(category, markerName, 64);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"ProfilerRecorder unavailable: {markerName} ({exception.Message})");
            return default;
        }
    }

    private static void StopRecorders()
    {
        if (mainThreadRecorder.Valid)
        {
            mainThreadRecorder.Dispose();
        }
        if (renderThreadRecorder.Valid)
        {
            renderThreadRecorder.Dispose();
        }
    }

    private static string BuildCsv()
    {
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine("elapsedSec,fps,deltaMs,batches,setPassCalls,drawCalls,triangles,vertices,shadowCasters,renderTextureChanges,usedTextureMemoryMB,mainThreadMs,renderThreadMs");
        foreach (Sample sample in samples)
        {
            writer.WriteLine(string.Join(",",
                sample.elapsedSec.ToString("F3", CultureInfo.InvariantCulture),
                sample.fps.ToString("F1", CultureInfo.InvariantCulture),
                sample.deltaMs.ToString("F2", CultureInfo.InvariantCulture),
                sample.batches,
                sample.setPassCalls,
                sample.drawCalls,
                sample.triangles,
                sample.vertices,
                sample.shadowCasters,
                sample.renderTextureChanges,
                sample.usedTextureMemoryMb.ToString("F1", CultureInfo.InvariantCulture),
                sample.mainThreadMs.ToString("F2", CultureInfo.InvariantCulture),
                sample.renderThreadMs.ToString("F2", CultureInfo.InvariantCulture)));
        }

        return writer.ToString();
    }

    private static string BuildSummary(string reason, string csvPath)
    {
        return string.Join(Environment.NewLine,
            $"Editor perf capture {reason}",
            $"Label: {label}",
            $"Unity: {Application.unityVersion}",
            $"Samples: {samples.Count}",
            $"CSV: {csvPath}",
            "",
            BuildMetricSummary("FPS", sample => sample.fps),
            BuildMetricSummary("DeltaMs", sample => sample.deltaMs),
            BuildMetricSummary("Batches", sample => sample.batches),
            BuildMetricSummary("SetPassCalls", sample => sample.setPassCalls),
            BuildMetricSummary("DrawCalls", sample => sample.drawCalls),
            BuildMetricSummary("Triangles", sample => sample.triangles),
            BuildMetricSummary("Vertices", sample => sample.vertices),
            BuildMetricSummary("UsedTextureMemoryMB", sample => sample.usedTextureMemoryMb),
            BuildMetricSummary("MainThreadMs", sample => sample.mainThreadMs),
            BuildMetricSummary("RenderThreadMs", sample => sample.renderThreadMs));
    }

    private static string BuildMetricSummary(string name, Func<Sample, double> getValue)
    {
        double min = double.MaxValue;
        double max = double.MinValue;
        double total = 0.0;
        int count = 0;

        foreach (Sample sample in samples)
        {
            double value = getValue(sample);
            if (value < 0.0)
            {
                continue;
            }

            min = Math.Min(min, value);
            max = Math.Max(max, value);
            total += value;
            count++;
        }

        if (count == 0)
        {
            return $"{name}: n/a";
        }

        return $"{name}: avg={total / count:F2} min={min:F2} max={max:F2}";
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "capture";
        }

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value;
    }

    private static string BuildLabel()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string avatarPath = PlayerPrefs.GetString(AvatarPathPrefsKey, "");
        if (string.IsNullOrEmpty(avatarPath))
        {
            return sceneName;
        }

        return $"{sceneName}_{Path.GetFileNameWithoutExtension(avatarPath)}";
    }

    private struct Sample
    {
        public float elapsedSec;
        public float fps;
        public float deltaMs;
        public int batches;
        public int setPassCalls;
        public int drawCalls;
        public int triangles;
        public int vertices;
        public int shadowCasters;
        public int renderTextureChanges;
        public float usedTextureMemoryMb;
        public float mainThreadMs;
        public float renderThreadMs;

        public static Sample Capture(float elapsedSec, ProfilerRecorder mainThreadRecorder, ProfilerRecorder renderThreadRecorder)
        {
            return new Sample
            {
                elapsedSec = elapsedSec,
                fps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f,
                deltaMs = Time.unscaledDeltaTime * 1000f,
                batches = UnityStatsReader.GetInt("batches"),
                setPassCalls = UnityStatsReader.GetInt("setPassCalls"),
                drawCalls = UnityStatsReader.GetInt("drawCalls"),
                triangles = UnityStatsReader.GetInt("triangles", "tris"),
                vertices = UnityStatsReader.GetInt("vertices", "verts"),
                shadowCasters = UnityStatsReader.GetInt("shadowCasters"),
                renderTextureChanges = UnityStatsReader.GetInt("renderTextureChanges"),
                usedTextureMemoryMb = UnityStatsReader.GetInt("usedTextureMemorySize") / (1024f * 1024f),
                mainThreadMs = RecorderMs(mainThreadRecorder),
                renderThreadMs = RecorderMs(renderThreadRecorder)
            };
        }

        private static float RecorderMs(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue / 1000000f : -1f;
        }
    }

    private static class UnityStatsReader
    {
        private static readonly Type statsType = Type.GetType("UnityEditor.UnityStats,UnityEditor");

        public static int GetInt(params string[] names)
        {
            if (statsType == null)
            {
                return -1;
            }

            foreach (string name in names)
            {
                PropertyInfo property = statsType.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    return ConvertToInt(property.GetValue(null, null));
                }

                FieldInfo field = statsType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return ConvertToInt(field.GetValue(null));
                }
            }

            return -1;
        }

        private static int ConvertToInt(object value)
        {
            if (value == null)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return -1;
            }
        }
    }
}
