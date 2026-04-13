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

    public static void ExportAndroidProject()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
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
        RemoveLilToonShadersFromAlwaysIncluded();

        try
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = exportPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android export failed: " + summary.result + " at " + exportPath);
            }

            UnityEngine.Debug.Log("Android export succeeded: " + exportPath);
        }
        finally
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExportAsProject;
        }
    }

    private static void RemoveLilToonShadersFromAlwaysIncluded()
    {
        UnityEngine.Object graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
        if (graphicsSettings == null)
        {
            UnityEngine.Debug.LogWarning("GraphicsSettings asset not found; skipping lilToon Always Included cleanup");
            return;
        }

        SerializedObject serializedGraphicsSettings = new SerializedObject(graphicsSettings);
        SerializedProperty alwaysIncludedShaders = serializedGraphicsSettings.FindProperty("m_AlwaysIncludedShaders");
        if (alwaysIncludedShaders == null || !alwaysIncludedShaders.isArray)
        {
            UnityEngine.Debug.LogWarning("m_AlwaysIncludedShaders not found; skipping lilToon Always Included cleanup");
            return;
        }

        List<Shader> shaders = new List<Shader>();
        int removed = 0;
        for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
        {
            Shader shader = alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (shader != null && !IsLilToonPackageShader(shader))
            {
                shaders.Add(shader);
            }
            else if (shader != null)
            {
                removed++;
            }
        }

        alwaysIncludedShaders.arraySize = shaders.Count;
        for (int i = 0; i < shaders.Count; i++)
        {
            alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue = shaders[i];
        }

        serializedGraphicsSettings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log($"Always Included lilToon shaders cleaned: removed={removed} total={shaders.Count}");
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
}
