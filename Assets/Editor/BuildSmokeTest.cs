using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildSmokeTest
{
    private const string OutputRoot = "BuildSmoke";
    private const string AndroidExportFolder = "AndroidExport";
    private const string ExportFolderEnv = "BUILD_SMOKE_EXPORT_FOLDER";
    private const string XrSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    private static readonly string[] RequiredUiShaderNames =
    {
        "UI/Default",
        "Hidden/Internal-GUITextureClipText",
        "Hidden/Internal-GUITextureClip",
        "Hidden/Internal-GUITexture"
    };

    public static void ExportAndroidProject()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string diagnosticsPath = Path.Combine(projectRoot, OutputRoot, "buildsmoke_diagnostics.log");
        string exportFolder = Environment.GetEnvironmentVariable(ExportFolderEnv);
        if (string.IsNullOrWhiteSpace(exportFolder))
        {
            exportFolder = AndroidExportFolder;
        }

        string exportPath = Path.Combine(projectRoot, OutputRoot, exportFolder);
        bool previousExportAsProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;

        if (Directory.Exists(exportPath))
        {
            Directory.Delete(exportPath, true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        AppendDiagnostics(diagnosticsPath, $"ExportAndroidProject start exportFolder={exportFolder}");
        List<UnityEngine.Object> savedAndroidLoaders = null;
        List<Shader> previousAlwaysIncludedShaders = null;

        try
        {
            savedAndroidLoaders = DisableAndroidOculusLoadersForBuild(diagnosticsPath);
            previousAlwaysIncludedShaders = PrepareAlwaysIncludedShadersForBuild(diagnosticsPath);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = exportPath,
                target = BuildTarget.Android,
                options = BuildOptions.DetailedBuildReport
            };

            AppendDiagnostics(diagnosticsPath, $"Calling BuildPlayer exportPath={exportPath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            AppendDiagnostics(
                diagnosticsPath,
                $"BuildPlayer result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} outputPath={summary.outputPath}");
            AppendBuildReportDiagnostics(diagnosticsPath, report);

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android export failed: " + summary.result + " at " + exportPath);
            }

            UnityEngine.Debug.Log("Android export succeeded: " + exportPath);
        }
        catch (Exception exception)
        {
            AppendDiagnostics(diagnosticsPath, "Exception: " + exception);
            throw;
        }
        finally
        {
            RestoreAlwaysIncludedShaders(previousAlwaysIncludedShaders, diagnosticsPath);
            RestoreAndroidLoaders(savedAndroidLoaders, diagnosticsPath);
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExportAsProject;
        }
    }

    private static List<Shader> PrepareAlwaysIncludedShadersForBuild(string diagnosticsPath)
    {
        UnityEngine.Object graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
        if (graphicsSettings == null)
        {
            UnityEngine.Debug.LogWarning("GraphicsSettings asset not found; skipping lilToon Always Included cleanup");
            return null;
        }

        SerializedObject serializedGraphicsSettings = new SerializedObject(graphicsSettings);
        SerializedProperty alwaysIncludedShaders = serializedGraphicsSettings.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncludedShaders == null || !alwaysIncludedShaders.isArray)
        {
            UnityEngine.Debug.LogWarning("m_AlwaysIncludedShaders not found; skipping lilToon Always Included cleanup");
            return null;
        }

        List<Shader> previousShaders = new List<Shader>();
        List<Shader> shaders = new List<Shader>();
        int removed = 0;
        for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
        {
            Shader shader = alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            previousShaders.Add(shader);
            if (shader != null && !IsLilToonPackageShader(shader))
            {
                shaders.Add(shader);
            }
            else if (shader != null)
            {
                removed++;
            }
        }

        int added = 0;
        foreach (string shaderName in RequiredUiShaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                AppendDiagnostics(diagnosticsPath, $"Required UI shader not found: {shaderName}");
                continue;
            }

            if (shaders.Contains(shader))
            {
                continue;
            }

            shaders.Add(shader);
            added++;
        }

        alwaysIncludedShaders.arraySize = shaders.Count;
        for (int i = 0; i < shaders.Count; i++)
        {
            alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue = shaders[i];
        }

        serializedGraphicsSettings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        AppendDiagnostics(diagnosticsPath, $"Prepared Always Included shaders: removedLilToon={removed} addedUi={added} total={shaders.Count}");
        UnityEngine.Debug.Log($"Always Included shaders prepared: removedLilToon={removed} addedUi={added} total={shaders.Count}");
        return previousShaders;
    }

    private static void RestoreAlwaysIncludedShaders(List<Shader> previousShaders, string diagnosticsPath)
    {
        if (previousShaders == null)
        {
            return;
        }

        UnityEngine.Object graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
        if (graphicsSettings == null)
        {
            AppendDiagnostics(diagnosticsPath, "GraphicsSettings asset not found during Always Included restore");
            return;
        }

        SerializedObject serializedGraphicsSettings = new SerializedObject(graphicsSettings);
        SerializedProperty alwaysIncludedShaders = serializedGraphicsSettings.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncludedShaders == null || !alwaysIncludedShaders.isArray)
        {
            AppendDiagnostics(diagnosticsPath, "m_AlwaysIncludedShaders missing during restore");
            return;
        }

        alwaysIncludedShaders.arraySize = previousShaders.Count;
        for (int i = 0; i < previousShaders.Count; i++)
        {
            alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue = previousShaders[i];
        }

        serializedGraphicsSettings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AppendDiagnostics(diagnosticsPath, $"Restored Always Included shaders after build; restored={previousShaders.Count}");
    }

    private static bool IsLilToonPackageShader(Shader shader)
    {
        string path = AssetDatabase.GetAssetPath(shader);
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return path.StartsWith("Packages/jp.lilxyzw.liltoon/Shader/", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendDiagnostics(string diagnosticsPath, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(diagnosticsPath));
        File.AppendAllText(diagnosticsPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
    }

    private static void AppendBuildReportDiagnostics(string diagnosticsPath, BuildReport report)
    {
        if (report == null)
        {
            AppendDiagnostics(diagnosticsPath, "BuildReport is null");
            return;
        }

        foreach (BuildStep step in report.steps)
        {
            AppendDiagnostics(
                diagnosticsPath,
                $"STEP name={step.name} duration={step.duration.TotalSeconds:F3}s depth={step.depth} messages={step.messages.Length}");

            foreach (BuildStepMessage message in step.messages)
            {
                AppendDiagnostics(diagnosticsPath, $"STEPMSG type={message.type} content={message.content}");
            }
        }
    }

    private static List<UnityEngine.Object> DisableAndroidOculusLoadersForBuild(string diagnosticsPath)
    {
        if (!File.Exists(XrSettingsPath))
        {
            AppendDiagnostics(diagnosticsPath, "XR settings asset not found; skipping Android loader cleanup");
            return null;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(XrSettingsPath);
        UnityEngine.Object androidProviders = assets.FirstOrDefault(asset => asset != null && asset.name == "Android Providers");
        if (androidProviders == null)
        {
            AppendDiagnostics(diagnosticsPath, "Android Providers asset not found; skipping Android loader cleanup");
            return null;
        }

        SerializedObject serializedProviders = new SerializedObject(androidProviders);
        SerializedProperty loadersProperty = serializedProviders.FindProperty("m_Loaders");
        if (loadersProperty == null || !loadersProperty.isArray)
        {
            AppendDiagnostics(diagnosticsPath, "Android Providers m_Loaders missing; skipping Android loader cleanup");
            return null;
        }

        List<UnityEngine.Object> previousLoaders = new List<UnityEngine.Object>();
        for (int i = 0; i < loadersProperty.arraySize; i++)
        {
            UnityEngine.Object loader = loadersProperty.GetArrayElementAtIndex(i).objectReferenceValue;
            previousLoaders.Add(loader);
        }

        bool removedAny = previousLoaders.Any(loader => loader != null && loader.name.IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0);
        if (!removedAny)
        {
            AppendDiagnostics(diagnosticsPath, "No Android Oculus loaders were active");
            return previousLoaders;
        }

        loadersProperty.arraySize = 0;
        serializedProviders.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(androidProviders);
        AssetDatabase.SaveAssets();
        AppendDiagnostics(diagnosticsPath, $"Disabled Android XR loaders for build; removed={previousLoaders.Count}");
        return previousLoaders;
    }

    private static void RestoreAndroidLoaders(List<UnityEngine.Object> previousLoaders, string diagnosticsPath)
    {
        if (previousLoaders == null)
        {
            return;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(XrSettingsPath);
        UnityEngine.Object androidProviders = assets.FirstOrDefault(asset => asset != null && asset.name == "Android Providers");
        if (androidProviders == null)
        {
            AppendDiagnostics(diagnosticsPath, "Android Providers asset missing during restore");
            return;
        }

        SerializedObject serializedProviders = new SerializedObject(androidProviders);
        SerializedProperty loadersProperty = serializedProviders.FindProperty("m_Loaders");
        if (loadersProperty == null || !loadersProperty.isArray)
        {
            AppendDiagnostics(diagnosticsPath, "Android Providers m_Loaders missing during restore");
            return;
        }

        loadersProperty.arraySize = previousLoaders.Count;
        for (int i = 0; i < previousLoaders.Count; i++)
        {
            loadersProperty.GetArrayElementAtIndex(i).objectReferenceValue = previousLoaders[i];
        }

        serializedProviders.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(androidProviders);
        AssetDatabase.SaveAssets();
        AppendDiagnostics(diagnosticsPath, $"Restored Android XR loaders after build; restored={previousLoaders.Count}");
    }
}
