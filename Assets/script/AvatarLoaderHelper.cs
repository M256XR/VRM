using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class AvatarLoaderHelper
{
    private const string LOG_FILE = "avatar_loader_helper.log";

    static AvatarLoaderHelper()
    {
        DebugLogger.InitLog(LOG_FILE);
    }

    public static bool IsVrmPath(string path)
    {
        return string.Equals(Path.GetExtension(path), ".vrm", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<GameObject> LoadAvatarAsync(string path)
    {
        if (IsVrmPath(path))
        {
            return await VRMLoaderHelper.LoadVRMAsync(path);
        }

        return await LoadAssetBundleAvatarAsync(path);
    }

    private static async Task<GameObject> LoadAssetBundleAvatarAsync(string bundlePath)
    {
        DebugLogger.LogSeparator(LOG_FILE, "LoadAssetBundleAvatarAsync START");
        DebugLogger.Log(LOG_FILE, $"Bundle path: {bundlePath}");

        if (!File.Exists(bundlePath))
        {
            DebugLogger.LogError(LOG_FILE, $"Bundle not found: {bundlePath}");
            return null;
        }

        AssetBundleCreateRequest createRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        while (!createRequest.isDone)
        {
            await Task.Yield();
        }

        AssetBundle bundle = createRequest.assetBundle;
        if (bundle == null)
        {
            DebugLogger.LogError(LOG_FILE, $"Failed to load AssetBundle: {bundlePath}");
            return null;
        }

        try
        {
            string prefabAssetName = FindPrefabAssetName(bundle);
            if (string.IsNullOrWhiteSpace(prefabAssetName))
            {
                DebugLogger.LogError(LOG_FILE, "No prefab asset found in AssetBundle");
                return null;
            }

            AssetBundleRequest loadRequest = bundle.LoadAssetAsync<GameObject>(prefabAssetName);
            while (!loadRequest.isDone)
            {
                await Task.Yield();
            }

            GameObject prefab = loadRequest.asset as GameObject;
            if (prefab == null)
            {
                DebugLogger.LogError(LOG_FILE, $"Prefab asset could not be loaded: {prefabAssetName}");
                return null;
            }

            // UsePass 参照先シェーダーを先にロードして Shader.Find で解決可能にする
            Shader[] allShaders = bundle.LoadAllAssets<Shader>();
            DebugLogger.Log(LOG_FILE, $"Shaders loaded from bundle: {(allShaders != null ? allShaders.Length : 0)}");
            if (allShaders != null)
            {
                foreach (Shader s in allShaders)
                {
                    Shader found = Shader.Find(s.name);
                    DebugLogger.Log(LOG_FILE,
                        $"  Shader asset: {s.name} supported={s.isSupported} Shader.Find={(found != null ? "found" : "NOT found")}" +
                        (found != null ? $" foundSupported={found.isSupported}" : ""));
                }
            }

            AnimationClip[] animationClips = bundle.LoadAllAssets<AnimationClip>();
            DebugLogger.Log(LOG_FILE, $"AnimationClips loaded from bundle: {(animationClips != null ? animationClips.Length : 0)}");
            if (animationClips != null)
            {
                foreach (AnimationClip clip in animationClips)
                {
                    DebugLogger.Log(LOG_FILE, $"  Clip: {clip.name} ({clip.length:F2}s)");
                }
            }

            TextAsset[] allTextAssets = bundle.LoadAllAssets<TextAsset>();
            DebugLogger.Log(LOG_FILE, $"TextAssets loaded from bundle: {(allTextAssets != null ? allTextAssets.Length : 0)}");
            if (allTextAssets != null)
            {
                foreach (TextAsset ta in allTextAssets)
                {
                    DebugLogger.Log(LOG_FILE, $"  TextAsset: '{ta.name}' ({ta.text.Length} chars)");
                }
            }

            TextAsset expressionManifest = FindExpressionManifest(allTextAssets);
            TextAsset springBoneManifest = FindSpringBoneManifest(allTextAssets);

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = prefab.name;
            AttachExpressionLibrary(instance, expressionManifest, animationClips);
            ApplyBundleShaders(instance, allShaders);

            // ネイティブ VRCPhysBone が存在する場合はそちらを優先し、
            // VRMSpringBone のフォールバックはスキップする
            int nativePhysBoneCount = DiagnosePhysBone(instance);
            if (nativePhysBoneCount > 0)
            {
                DebugLogger.Log(LOG_FILE, $"Native VRCPhysBone detected ({nativePhysBoneCount}), skipping SpringBoneSetup fallback");
            }
            else
            {
                ApplySpringBones(instance, springBoneManifest);
            }

            DiagnoseShaders(instance);
            VRMLoaderHelper.PrepareLoadedAvatar(instance);

            DebugLogger.Log(LOG_FILE, $"AssetBundle avatar loaded: {prefabAssetName}");
            DebugLogger.LogSeparator(LOG_FILE, "LoadAssetBundleAvatarAsync END");
            return instance;
        }
        finally
        {
            bundle.Unload(false);
        }
    }

    private static string FindPrefabAssetName(AssetBundle bundle)
    {
        string[] allNames = bundle.GetAllAssetNames();
        DebugLogger.Log(LOG_FILE, $"Bundle contains {allNames.Length} assets:");
        foreach (string name in allNames)
        {
            DebugLogger.Log(LOG_FILE, $"  {name}");
        }

        foreach (string assetName in allNames)
        {
            if (assetName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                DebugLogger.Log(LOG_FILE, $"Prefab selected from bundle: {assetName}");
                return assetName;
            }
        }

        return null;
    }

    private static TextAsset FindExpressionManifest(TextAsset[] textAssets)
    {
        if (textAssets == null)
        {
            return null;
        }

        foreach (TextAsset textAsset in textAssets)
        {
            if (textAsset != null &&
                string.Equals(textAsset.name, BundleExpressionLibrary.ManifestAssetName, StringComparison.OrdinalIgnoreCase))
            {
                DebugLogger.Log(LOG_FILE, $"Expression manifest found: {textAsset.name}");
                return textAsset;
            }
        }

        DebugLogger.Log(LOG_FILE, "Expression manifest not found in bundle");
        return null;
    }

    private static TextAsset FindSpringBoneManifest(TextAsset[] textAssets)
    {
        if (textAssets == null) return null;
        foreach (TextAsset ta in textAssets)
        {
            if (ta != null && string.Equals(ta.name, SpringBoneSetup.ManifestAssetName, StringComparison.OrdinalIgnoreCase))
            {
                DebugLogger.Log(LOG_FILE, $"Spring bone manifest found: {ta.name}");
                return ta;
            }
        }
        DebugLogger.Log(LOG_FILE, "Spring bone manifest not found in bundle");
        return null;
    }

    private static void ApplySpringBones(GameObject avatarRoot, TextAsset manifestAsset)
    {
        if (manifestAsset == null)
        {
            DebugLogger.Log(LOG_FILE, "No spring bone manifest, skipping SpringBoneSetup");
            return;
        }
        SpringBoneSetup.Apply(avatarRoot, manifestAsset);
    }

    /// <summary>
    /// VRCPhysBone コンポーネントの存在を確認し、検出数を返す。
    /// </summary>
    private static int DiagnosePhysBone(GameObject avatarRoot)
    {
        MonoBehaviour[] allBehaviours = avatarRoot.GetComponentsInChildren<MonoBehaviour>(true);
        int physBoneCount = 0;
        int physBoneColliderCount = 0;
        int missingScriptCount = 0;

        foreach (MonoBehaviour mb in allBehaviours)
        {
            if (mb == null)
            {
                missingScriptCount++;
                continue;
            }

            string typeName = mb.GetType().Name;
            if (typeName.Contains("PhysBone") && !typeName.Contains("Collider"))
            {
                physBoneCount++;
                DebugLogger.Log(LOG_FILE, $"  PhysBone: {mb.gameObject.name} enabled={mb.enabled} type={mb.GetType().FullName}");
            }
            else if (typeName.Contains("PhysBoneCollider"))
            {
                physBoneColliderCount++;
            }
        }

        DebugLogger.Log(LOG_FILE, $"PhysBone diagnostic: {physBoneCount} PhysBones, {physBoneColliderCount} Colliders, {missingScriptCount} MissingScripts");

        if (missingScriptCount > 0)
        {
            DebugLogger.LogWarning(LOG_FILE, $"{missingScriptCount} components have missing scripts (DLL not loaded?)");
        }

        return physBoneCount;
    }

    private static void ApplyBundleShaders(GameObject avatarRoot, Shader[] bundleShaders)
    {
        if (avatarRoot == null || bundleShaders == null || bundleShaders.Length == 0)
        {
            DebugLogger.Log(LOG_FILE, "Bundle shader rebinding skipped");
            return;
        }

        var shadersByName = new System.Collections.Generic.Dictionary<string, Shader>(StringComparer.Ordinal);
        foreach (Shader shader in bundleShaders)
        {
            if (shader == null || string.IsNullOrWhiteSpace(shader.name))
            {
                continue;
            }

            if (!shadersByName.ContainsKey(shader.name))
            {
                shadersByName.Add(shader.name, shader);
            }
        }

        int reboundCount = 0;
        int missingCount = 0;
        var loggedMissing = new System.Collections.Generic.HashSet<string>();

        foreach (Renderer renderer in avatarRoot.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null)
                {
                    continue;
                }

                string shaderName = material.shader.name;
                if (shadersByName.TryGetValue(shaderName, out Shader bundleShader))
                {
                    Shader preferredShader = ResolvePreferredShader(shaderName, bundleShader);
                    if (preferredShader != null && material.shader != preferredShader)
                    {
                        DebugLogger.Log(LOG_FILE, $"Rebinding material shader: {material.name} {shaderName} -> {preferredShader.name} supported={preferredShader.isSupported}");
                        AssignShaderPreservingMaterialState(material, preferredShader);
                        reboundCount++;
                    }
                }
                else if (loggedMissing.Add(shaderName))
                {
                    DebugLogger.Log(LOG_FILE, $"No bundle shader match for material shader: {shaderName}");
                    missingCount++;
                }
            }
        }

        DebugLogger.Log(LOG_FILE, $"Bundle shader rebinding complete: rebound={reboundCount} missingShaderNames={missingCount}");
    }

    private static Shader ResolvePreferredShader(string shaderName, Shader bundleShader)
    {
#if UNITY_EDITOR
        Shader editorShader = Shader.Find(shaderName);
        if (editorShader != null && editorShader.isSupported)
        {
            DebugLogger.Log(LOG_FILE, $"Editor shader preferred: {shaderName} bundleSupported={(bundleShader != null && bundleShader.isSupported)} editorSupported={editorShader.isSupported}");
            return editorShader;
        }
#endif
        return bundleShader;
    }

    private static void AssignShaderPreservingMaterialState(Material material, Shader shader)
    {
        int renderQueue = material.renderQueue;
        string[] keywords = material.shaderKeywords;

        material.shader = shader;
        material.shaderKeywords = keywords;
        material.renderQueue = renderQueue;
    }

    /// <summary>
    /// Logs shader/material state without changing the loaded avatar.
    /// Keep this diagnostic-only so lilToon features from the Android AssetBundle remain intact.
    /// </summary>
    private static void DiagnoseShaders(GameObject avatarRoot)
    {
        DebugLogger.LogSeparator(LOG_FILE, "Shader Diagnostic (no swap)");
        Renderer[] renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
        DebugLogger.Log(LOG_FILE, $"Renderer count: {renderers.Length}");

        var loggedMaterials = new System.Collections.Generic.HashSet<int>();

        foreach (Renderer r in renderers)
        {
            Material[] mats = r.sharedMaterials;
            DebugLogger.Log(LOG_FILE,
                $"Renderer: {GetTransformPath(r.transform, avatarRoot.transform)} type={r.GetType().Name} enabled={r.enabled} materials={mats.Length}");

            foreach (Material mat in mats)
            {
                if (mat == null || mat.shader == null) continue;

                int matId = mat.GetInstanceID();
                string shaderName = mat.shader.name;
                bool firstLogForMaterial = loggedMaterials.Add(matId);

                DebugLogger.Log(LOG_FILE,
                    $"  mat={mat.name} shader={shaderName} renderQueue={mat.renderQueue} keywords=[{string.Join(",", mat.shaderKeywords)}] " +
                    $"lilToon={IsLilToonShader(shaderName)} outlineShader={IsLilToonOutlineShader(shaderName)} passes={mat.passCount}{BuildMaterialFeatureInfo(mat)}");

                if (firstLogForMaterial)
                {
                    for (int i = 0; i < mat.passCount; i++)
                    {
                        DebugLogger.Log(LOG_FILE, $"    pass[{i}] name={mat.GetPassName(i)}");
                    }

                    foreach (string passName in new[] { "FORWARD", "FORWARD_OUTLINE", "FORWARD_ADD", "FORWARD_ADD_OUTLINE", "SHADOW_CASTER", "SHADOW_CASTER_OUTLINE" })
                    {
                        DebugLogger.Log(LOG_FILE, $"    passCheck {passName} enabled={mat.GetShaderPassEnabled(passName)}");
                    }
                }
            }
        }
    }

    private static string BuildMaterialFeatureInfo(Material mat)
    {
        var parts = new System.Collections.Generic.List<string>();
        AddFloat(parts, mat, "_OutlineWidth", "outline");
        AddColor(parts, mat, "_OutlineColor", "outlineColor");
        AddFloat(parts, mat, "_TransparentMode", "transparentMode");
        AddFloat(parts, mat, "_Cutoff", "cutoff");
        AddFloat(parts, mat, "_UseFur", "fur");
        AddFloat(parts, mat, "_FurLayerNum", "furLayers");
        AddFloat(parts, mat, "_FurVectorScale", "furVectorScale");
        AddVector(parts, mat, "_FurVector", "furVector");
        AddFloat(parts, mat, "_UseGem", "gem");
        AddFloat(parts, mat, "_GemChromaticAberration", "gemChromatic");
        AddFloat(parts, mat, "_GemEnvContrast", "gemContrast");
        AddFloat(parts, mat, "_UseEmission", "emission");
        AddFloat(parts, mat, "_UseEmission2nd", "emission2");
        AddFloat(parts, mat, "_Cull", "cull");
        AddFloat(parts, mat, "_SrcBlend", "srcBlend");
        AddFloat(parts, mat, "_DstBlend", "dstBlend");
        AddTexture(parts, mat, "_MainTex", "mainTex");
        AddTexture(parts, mat, "_FurNoiseMask", "furNoise");
        AddTexture(parts, mat, "_FurMask", "furMask");
        AddTexture(parts, mat, "_FurLengthMask", "furLengthMask");
        AddTexture(parts, mat, "_FurVectorTex", "furVectorTex");
        AddTexture(parts, mat, "_EmissionMap", "emissionMap");
        AddTexture(parts, mat, "_MatCapTex", "matcapTex");

        return parts.Count == 0 ? string.Empty : " " + string.Join(" ", parts);
    }

    private static void AddFloat(System.Collections.Generic.List<string> parts, Material mat, string propertyName, string label)
    {
        if (mat.HasProperty(propertyName))
        {
            parts.Add($"{label}={mat.GetFloat(propertyName)}");
        }
    }

    private static void AddColor(System.Collections.Generic.List<string> parts, Material mat, string propertyName, string label)
    {
        if (mat.HasProperty(propertyName))
        {
            Color color = mat.GetColor(propertyName);
            parts.Add($"{label}=({color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3})");
        }
    }

    private static void AddVector(System.Collections.Generic.List<string> parts, Material mat, string propertyName, string label)
    {
        if (mat.HasProperty(propertyName))
        {
            Vector4 vector = mat.GetVector(propertyName);
            parts.Add($"{label}=({vector.x:F3},{vector.y:F3},{vector.z:F3},{vector.w:F3})");
        }
    }

    private static void AddTexture(System.Collections.Generic.List<string> parts, Material mat, string propertyName, string label)
    {
        if (mat.HasProperty(propertyName))
        {
            Texture texture = mat.GetTexture(propertyName);
            parts.Add($"{label}={(texture != null ? texture.name : "null")}");
        }
    }

    private static bool IsLilToonShader(string shaderName)
    {
        return shaderName != null &&
            (shaderName == "lilToon" || shaderName.Contains("lilToon"));
    }

    private static bool IsLilToonOutlineShader(string shaderName)
    {
        return IsLilToonShader(shaderName) && shaderName.Contains("Outline");
    }

    private static string GetTransformPath(Transform target, Transform root)
    {
        if (target == null) return "<null>";
        if (target == root) return target.name;

        var parts = new System.Collections.Generic.List<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            parts.Insert(0, current.name);
            current = current.parent;
        }

        if (root != null)
        {
            parts.Insert(0, root.name);
        }

        return string.Join("/", parts);
    }

    private static void AttachExpressionLibrary(GameObject avatarRoot, TextAsset manifestAsset, AnimationClip[] animationClips)
    {
        if (avatarRoot == null || ((animationClips == null || animationClips.Length == 0) && manifestAsset == null))
        {
            return;
        }

        BundleExpressionLibrary expressionLibrary = avatarRoot.GetComponent<BundleExpressionLibrary>();
        if (expressionLibrary == null)
        {
            expressionLibrary = avatarRoot.AddComponent<BundleExpressionLibrary>();
        }

        expressionLibrary.Initialize(manifestAsset, animationClips);

        if (expressionLibrary.HasRuntimeExpressions)
        {
            DebugLogger.Log(LOG_FILE, $"Runtime expressions registered: {expressionLibrary.ExpressionCount}");
        }
        else if (expressionLibrary.HasExpressions)
        {
            DebugLogger.LogWarning(LOG_FILE, "Bundle provided legacy clip-only expressions; runtime blendshape data is missing");
        }
        else
        {
            DebugLogger.LogWarning(LOG_FILE, "Bundle did not provide usable runtime expressions");
        }
    }
}
