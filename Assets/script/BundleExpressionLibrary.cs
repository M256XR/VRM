using System;
using System.Collections.Generic;
using UnityEngine;

public class BundleExpressionLibrary : MonoBehaviour
{
    public const string ManifestAssetName = "wallpaper_expression_manifest";

    [Serializable]
    private class ManifestData
    {
        public ManifestEntry[] expressions;
    }

    [Serializable]
    private class ManifestEntry
    {
        public string key;
        public string clipName;
        public RendererEntry[] renderers;
    }

    [Serializable]
    private class RendererEntry
    {
        public string path;
        public BlendShapeEntry[] blendShapes;
    }

    [Serializable]
    private class BlendShapeEntry
    {
        public string name;
        public float value;
    }

    private sealed class RuntimeExpression
    {
        public string ClipName;
        public RuntimeRendererBinding[] Renderers;
    }

    private sealed class RuntimeRendererBinding
    {
        public SkinnedMeshRenderer Renderer;
        public RuntimeBlendShapeBinding[] BlendShapes;
    }

    private readonly struct RuntimeBlendShapeBinding
    {
        public RuntimeBlendShapeBinding(int index, float value)
        {
            Index = index;
            Value = value;
        }

        public int Index { get; }
        public float Value { get; }
    }

    public struct BlendShapeTarget
    {
        public SkinnedMeshRenderer Renderer;
        public int Index;
        public float Value;
    }

    private readonly Dictionary<string, RuntimeExpression> expressionsByName =
        new Dictionary<string, RuntimeExpression>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, AnimationClip> clipsByExpression =
        new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<SkinnedMeshRenderer, HashSet<int>> trackedBlendShapeIndices =
        new Dictionary<SkinnedMeshRenderer, HashSet<int>>();

    private const string LOG_FILE = "bundle_expression_library.log";

    public bool HasExpressions => expressionsByName.Count > 0 || clipsByExpression.Count > 0;
    public bool HasClipExpressions => clipsByExpression.Count > 0;
    public bool HasRuntimeExpressions => expressionsByName.Count > 0;
    public int ExpressionCount => expressionsByName.Count;

    public void Initialize(TextAsset manifestAsset, AnimationClip[] animationClips)
    {
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.LogSeparator(LOG_FILE, "BundleExpressionLibrary Initialize");

        expressionsByName.Clear();
        clipsByExpression.Clear();
        trackedBlendShapeIndices.Clear();

        DebugLogger.Log(LOG_FILE, $"Manifest asset: {(manifestAsset != null ? manifestAsset.name : "NULL")}");
        DebugLogger.Log(LOG_FILE, $"Animation clips: {(animationClips != null ? animationClips.Length.ToString() : "NULL")}");

        LogAvailableRenderers();

        Dictionary<string, AnimationClip> clipsByName =
            new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);

        if (animationClips != null)
        {
            foreach (AnimationClip clip in animationClips)
            {
                if (clip == null || clipsByName.ContainsKey(clip.name))
                {
                    continue;
                }

                clipsByName[clip.name] = clip;
                DebugLogger.Log(LOG_FILE, $"  Clip registered: {clip.name}");
            }
        }

        RegisterManifestMappings(manifestAsset, clipsByName);

        if (expressionsByName.Count == 0 && clipsByExpression.Count == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, "No manifest expressions found, falling back to heuristic mappings");
            RegisterHeuristicMappings(clipsByName);
        }

        DebugLogger.Log(LOG_FILE, $"Initialize complete - RuntimeExpressions: {expressionsByName.Count}, ClipExpressions: {clipsByExpression.Count}");
    }

    public bool TryApplyExpression(string expressionName)
    {
        if (!expressionsByName.TryGetValue(expressionName, out RuntimeExpression expression) || expression == null)
        {
            DebugLogger.LogWarning(LOG_FILE, $"TryApplyExpression: '{expressionName}' not found in expressionsByName (count={expressionsByName.Count})");
            return false;
        }

        ResetTrackedBlendShapes();

        if (expression.Renderers == null || expression.Renderers.Length == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, $"TryApplyExpression: '{expressionName}' has no renderer bindings - expression will have no visual effect");
            return true;
        }

        int appliedCount = 0;
        foreach (RuntimeRendererBinding rendererBinding in expression.Renderers)
        {
            if (rendererBinding?.Renderer == null || rendererBinding.BlendShapes == null)
            {
                continue;
            }

            foreach (RuntimeBlendShapeBinding blendShape in rendererBinding.BlendShapes)
            {
                rendererBinding.Renderer.SetBlendShapeWeight(blendShape.Index, blendShape.Value);
                appliedCount++;
            }
        }

        DebugLogger.Log(LOG_FILE, $"TryApplyExpression: '{expressionName}' applied {appliedCount} blendshape(s) across {expression.Renderers.Length} renderer(s)");
        return true;
    }

    public bool TryGetClip(string expressionName, out AnimationClip clip)
    {
        return clipsByExpression.TryGetValue(expressionName, out clip);
    }

    /// <summary>
    /// 表情のターゲット BlendShape 値を取得（適用はしない）。
    /// トラッキング中の全 BlendShape を 0 にリセットした上で、指定表情の値で上書きした結果を返す。
    /// </summary>
    public bool TryGetExpressionTargets(string expressionName, out BlendShapeTarget[] targets)
    {
        targets = null;
        if (!expressionsByName.TryGetValue(expressionName, out RuntimeExpression expression) || expression == null)
        {
            return false;
        }

        var result = new Dictionary<(int rendererID, int shapeIndex), BlendShapeTarget>();

        // トラッキング中の全 BlendShape を 0 にリセット
        foreach (KeyValuePair<SkinnedMeshRenderer, HashSet<int>> pair in trackedBlendShapeIndices)
        {
            if (pair.Key == null) continue;
            int rid = pair.Key.GetInstanceID();
            foreach (int idx in pair.Value)
            {
                result[(rid, idx)] = new BlendShapeTarget { Renderer = pair.Key, Index = idx, Value = 0f };
            }
        }

        // 表情のターゲット値で上書き
        if (expression.Renderers != null)
        {
            foreach (RuntimeRendererBinding rb in expression.Renderers)
            {
                if (rb?.Renderer == null || rb.BlendShapes == null) continue;
                int rid = rb.Renderer.GetInstanceID();
                foreach (RuntimeBlendShapeBinding bs in rb.BlendShapes)
                {
                    result[(rid, bs.Index)] = new BlendShapeTarget { Renderer = rb.Renderer, Index = bs.Index, Value = bs.Value };
                    TrackBlendShape(rb.Renderer, bs.Index);
                }
            }
        }

        targets = new BlendShapeTarget[result.Count];
        int i = 0;
        foreach (var kvp in result)
        {
            targets[i++] = kvp.Value;
        }

        return true;
    }

    /// <summary>
    /// トラッキング中の全 BlendShape を 0 にするターゲット配列を返す。
    /// </summary>
    public BlendShapeTarget[] GetResetTargets()
    {
        List<BlendShapeTarget> list = new List<BlendShapeTarget>();
        foreach (KeyValuePair<SkinnedMeshRenderer, HashSet<int>> pair in trackedBlendShapeIndices)
        {
            if (pair.Key == null) continue;
            foreach (int idx in pair.Value)
            {
                list.Add(new BlendShapeTarget { Renderer = pair.Key, Index = idx, Value = 0f });
            }
        }
        return list.ToArray();
    }

    private void RegisterManifestMappings(TextAsset manifestAsset, Dictionary<string, AnimationClip> clipsByName)
    {
        if (manifestAsset == null || string.IsNullOrWhiteSpace(manifestAsset.text))
        {
            return;
        }

        ManifestData manifest = JsonUtility.FromJson<ManifestData>(manifestAsset.text);
        if (manifest == null || manifest.expressions == null)
        {
            return;
        }

        foreach (ManifestEntry entry in manifest.expressions)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(entry.clipName) &&
                clipsByName.TryGetValue(entry.clipName, out AnimationClip clip))
            {
                clipsByExpression[entry.key] = clip;
            }

            RuntimeExpression runtimeExpression = CreateRuntimeExpression(entry);
            if (runtimeExpression != null)
            {
                expressionsByName[entry.key] = runtimeExpression;
            }
        }
    }

    private RuntimeExpression CreateRuntimeExpression(ManifestEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        DebugLogger.Log(LOG_FILE, $"Creating runtime expression: '{entry.key}' (clip: {entry.clipName}, renderers: {entry.renderers?.Length ?? 0})");

        List<RuntimeRendererBinding> rendererBindings = new List<RuntimeRendererBinding>();

        if (entry.renderers != null)
        {
            foreach (RendererEntry rendererEntry in entry.renderers)
            {
                if (!TryResolveRenderer(rendererEntry, out SkinnedMeshRenderer renderer))
                {
                    DebugLogger.LogWarning(LOG_FILE, $"  Renderer resolve failed for path '{rendererEntry?.path}' in expression '{entry.key}'");
                    continue;
                }

                RuntimeBlendShapeBinding[] blendShapeBindings = CreateBlendShapeBindings(renderer, rendererEntry);
                if (blendShapeBindings.Length == 0)
                {
                    DebugLogger.LogWarning(LOG_FILE, $"  No blendshape bindings matched for '{entry.key}' on {renderer.gameObject.name}");
                    continue;
                }

                rendererBindings.Add(new RuntimeRendererBinding
                {
                    Renderer = renderer,
                    BlendShapes = blendShapeBindings
                });
            }
        }

        if (rendererBindings.Count == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, $"  Expression '{entry.key}' has ZERO effective bindings - will not produce visible change");
        }

        return new RuntimeExpression
        {
            ClipName = entry.clipName,
            Renderers = rendererBindings.ToArray()
        };
    }

    private bool TryResolveRenderer(RendererEntry rendererEntry, out SkinnedMeshRenderer renderer)
    {
        renderer = null;
        if (rendererEntry == null)
        {
            return false;
        }

        string path = rendererEntry.path;

        // 1. パスが空なら自身から取得
        if (string.IsNullOrWhiteSpace(path))
        {
            renderer = GetComponent<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                renderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            }

            bool found = renderer != null && renderer.sharedMesh != null;
            DebugLogger.Log(LOG_FILE, $"ResolveRenderer (empty path): {(found ? renderer.gameObject.name : "NOT FOUND")}");
            return found;
        }

        // 2. Transform.Find で正確なパス解決
        Transform target = transform.Find(path);
        if (target != null)
        {
            renderer = target.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null && renderer.sharedMesh != null)
            {
                DebugLogger.Log(LOG_FILE, $"ResolveRenderer: path '{path}' -> {renderer.gameObject.name} ({renderer.sharedMesh.blendShapeCount} shapes)");
                return true;
            }
        }

        // 3. パス解決失敗 — 名前で全 SkinnedMeshRenderer を検索
        DebugLogger.LogWarning(LOG_FILE, $"ResolveRenderer: transform.Find('{path}') failed, trying name-based fallback");

        SkinnedMeshRenderer[] allRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        SkinnedMeshRenderer bestByName = null;
        SkinnedMeshRenderer bestByShapeCount = null;
        int maxShapeCount = 0;

        foreach (SkinnedMeshRenderer smr in allRenderers)
        {
            if (smr.sharedMesh == null)
            {
                continue;
            }

            // 名前が一致（大小無視）
            if (string.Equals(smr.gameObject.name, path, StringComparison.OrdinalIgnoreCase))
            {
                bestByName = smr;
            }

            // BlendShape 数が最多のものを記録（最終フォールバック用）
            if (smr.sharedMesh.blendShapeCount > maxShapeCount)
            {
                maxShapeCount = smr.sharedMesh.blendShapeCount;
                bestByShapeCount = smr;
            }
        }

        if (bestByName != null)
        {
            renderer = bestByName;
            DebugLogger.Log(LOG_FILE, $"ResolveRenderer: name fallback matched '{renderer.gameObject.name}' ({renderer.sharedMesh.blendShapeCount} shapes)");
            return true;
        }

        if (bestByShapeCount != null)
        {
            renderer = bestByShapeCount;
            DebugLogger.LogWarning(LOG_FILE, $"ResolveRenderer: using renderer with most blendshapes as last resort: '{renderer.gameObject.name}' ({maxShapeCount} shapes)");
            return true;
        }

        DebugLogger.LogError(LOG_FILE, $"ResolveRenderer: no SkinnedMeshRenderer found for path '{path}'");
        return false;
    }

    private RuntimeBlendShapeBinding[] CreateBlendShapeBindings(SkinnedMeshRenderer renderer, RendererEntry rendererEntry)
    {
        if (rendererEntry.blendShapes == null || rendererEntry.blendShapes.Length == 0)
        {
            return Array.Empty<RuntimeBlendShapeBinding>();
        }

        // メッシュ上の全 BlendShape 名を取得してルックアップ用辞書を作成
        Mesh mesh = renderer.sharedMesh;
        Dictionary<string, int> nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i);
            if (!nameToIndex.ContainsKey(shapeName))
            {
                nameToIndex[shapeName] = i;
            }
        }

        List<RuntimeBlendShapeBinding> bindings = new List<RuntimeBlendShapeBinding>();
        int matchCount = 0;
        int missCount = 0;

        foreach (BlendShapeEntry blendShapeEntry in rendererEntry.blendShapes)
        {
            if (blendShapeEntry == null || string.IsNullOrWhiteSpace(blendShapeEntry.name))
            {
                continue;
            }

            // 1. 完全一致
            int blendShapeIndex = mesh.GetBlendShapeIndex(blendShapeEntry.name);

            // 2. 大小無視で一致
            if (blendShapeIndex < 0 && nameToIndex.TryGetValue(blendShapeEntry.name, out int fallbackIndex))
            {
                blendShapeIndex = fallbackIndex;
            }

            // 3. 部分一致（末尾一致 or 含む）— MA/NDMF ベイク後のプレフィックス付き名前に対応
            if (blendShapeIndex < 0)
            {
                string searchName = blendShapeEntry.name;
                foreach (KeyValuePair<string, int> kvp in nameToIndex)
                {
                    if (kvp.Key.EndsWith(searchName, StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        blendShapeIndex = kvp.Value;
                        DebugLogger.Log(LOG_FILE, $"  BlendShape partial match: '{blendShapeEntry.name}' -> '{kvp.Key}' (index {kvp.Value})");
                        break;
                    }
                }
            }

            if (blendShapeIndex < 0)
            {
                missCount++;
                DebugLogger.LogWarning(LOG_FILE, $"  BlendShape NOT FOUND: '{blendShapeEntry.name}' on {renderer.gameObject.name}");
                continue;
            }

            matchCount++;
            bindings.Add(new RuntimeBlendShapeBinding(blendShapeIndex, blendShapeEntry.value));
            TrackBlendShape(renderer, blendShapeIndex);
        }

        DebugLogger.Log(LOG_FILE, $"  BlendShape bindings on {renderer.gameObject.name}: {matchCount} matched, {missCount} missed (out of {rendererEntry.blendShapes.Length})");
        return bindings.ToArray();
    }

    private void TrackBlendShape(SkinnedMeshRenderer renderer, int blendShapeIndex)
    {
        if (!trackedBlendShapeIndices.TryGetValue(renderer, out HashSet<int> indices))
        {
            indices = new HashSet<int>();
            trackedBlendShapeIndices[renderer] = indices;
        }

        indices.Add(blendShapeIndex);
    }

    private void ResetTrackedBlendShapes()
    {
        foreach (KeyValuePair<SkinnedMeshRenderer, HashSet<int>> pair in trackedBlendShapeIndices)
        {
            SkinnedMeshRenderer renderer = pair.Key;
            if (renderer == null)
            {
                continue;
            }

            foreach (int blendShapeIndex in pair.Value)
            {
                renderer.SetBlendShapeWeight(blendShapeIndex, 0f);
            }
        }
    }

    private void LogAvailableRenderers()
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        DebugLogger.Log(LOG_FILE, $"Available SkinnedMeshRenderers under '{gameObject.name}': {renderers.Length}");

        foreach (SkinnedMeshRenderer smr in renderers)
        {
            if (smr.sharedMesh == null)
            {
                DebugLogger.Log(LOG_FILE, $"  [{GetRelativePath(smr.transform)}] {smr.gameObject.name} - NO MESH");
                continue;
            }

            int shapeCount = smr.sharedMesh.blendShapeCount;
            DebugLogger.Log(LOG_FILE, $"  [{GetRelativePath(smr.transform)}] {smr.gameObject.name} - {shapeCount} blendshapes");

            for (int i = 0; i < shapeCount; i++)
            {
                DebugLogger.Log(LOG_FILE, $"    [{i}] {smr.sharedMesh.GetBlendShapeName(i)}");
            }
        }
    }

    private string GetRelativePath(Transform target)
    {
        if (target == transform)
        {
            return "";
        }

        List<string> parts = new List<string>();
        Transform current = target;
        while (current != null && current != transform)
        {
            parts.Insert(0, current.name);
            current = current.parent;
        }

        return string.Join("/", parts);
    }

    private void RegisterHeuristicMappings(Dictionary<string, AnimationClip> clipsByName)
    {
        foreach (KeyValuePair<string, AnimationClip> pair in clipsByName)
        {
            string expressionKey = InferExpressionKey(pair.Key);
            if (string.IsNullOrWhiteSpace(expressionKey) || clipsByExpression.ContainsKey(expressionKey))
            {
                continue;
            }

            clipsByExpression[expressionKey] = pair.Value;
        }
    }

    private static string InferExpressionKey(string clipName)
    {
        string lower = clipName.ToLowerInvariant();

        if (lower.Contains("default_face") || lower == "neutral" || lower.Contains("idle"))
        {
            return "Neutral";
        }

        if (lower.Contains("blink") || lower.Contains("wink"))
        {
            return "Blink";
        }

        if (lower.Contains("smile"))
        {
            return "Fun";
        }

        if (lower.Contains("joy") || lower.Contains("happy"))
        {
            return "Joy";
        }

        if (lower.Contains("angry") || lower.Contains("jito"))
        {
            return "Angry";
        }

        if (lower.Contains("sad") || lower.Contains("sorrow"))
        {
            return "Sorrow";
        }

        return null;
    }
}
