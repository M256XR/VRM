using System.Collections;
using UnityEngine;
using VRM;

public class ExpressionManager
{
    private const string LOG_FILE = "expression_manager.log";

    private static readonly BlendShapePreset[] ResettablePresets =
    {
        BlendShapePreset.Joy,
        BlendShapePreset.Angry,
        BlendShapePreset.Sorrow,
        BlendShapePreset.Fun,
        BlendShapePreset.Neutral,
        BlendShapePreset.Blink,
        BlendShapePreset.A,
        BlendShapePreset.I,
        BlendShapePreset.U,
        BlendShapePreset.E,
        BlendShapePreset.O
    };

    private enum BackendType
    {
        None,
        VrmBlendShape,
        BundleClips,
        BundleExpressions
    }

    private VRMBlendShapeProxy blendShapeProxy;
    private BundleExpressionLibrary bundleExpressionLibrary;
    private GameObject bundleAvatarRoot;
    private MonoBehaviour coroutineRunner;
    private Coroutine resetCoroutine;
    private Coroutine blendCoroutine;
    private string currentExpression = "Neutral";
    private BackendType backendType;
    private const float BLEND_IN_DURATION = 0.12f;
    private const float BLEND_OUT_DURATION = 0.3f;

    public enum ExpressionCategory
    {
        Happy,
        Sad,
        Angry,
        Neutral,
        All
    }

    public bool IsReady => backendType != BackendType.None;

    public ExpressionManager(GameObject vrmRoot, MonoBehaviour runner)
    {
        DebugLogger.InitLog(LOG_FILE);
        Rebind(vrmRoot, runner);
    }

    public bool Rebind(GameObject vrmRoot, MonoBehaviour runner = null)
    {
        if (runner != null)
        {
            coroutineRunner = runner;
        }

        CancelResetCoroutine();
        CancelBlendCoroutine();
        blendShapeProxy = null;
        bundleExpressionLibrary = null;
        bundleAvatarRoot = null;
        backendType = BackendType.None;
        currentExpression = "Neutral";

        if (vrmRoot == null)
        {
            DebugLogger.LogError(LOG_FILE, "Cannot bind expressions because avatar root is null");
            return false;
        }

        blendShapeProxy = vrmRoot.GetComponent<VRMBlendShapeProxy>();
        if (blendShapeProxy == null)
        {
            blendShapeProxy = vrmRoot.GetComponentInChildren<VRMBlendShapeProxy>(true);
        }

        if (blendShapeProxy != null)
        {
            backendType = BackendType.VrmBlendShape;
            DebugLogger.Log(LOG_FILE, $"ExpressionManager bound to VRM proxy on {blendShapeProxy.gameObject.name}");
            return true;
        }

        DebugLogger.Log(LOG_FILE, "No VRMBlendShapeProxy found, looking for BundleExpressionLibrary");

        bundleExpressionLibrary = vrmRoot.GetComponent<BundleExpressionLibrary>();
        if (bundleExpressionLibrary == null)
        {
            bundleExpressionLibrary = vrmRoot.GetComponentInChildren<BundleExpressionLibrary>(true);
        }

        if (bundleExpressionLibrary == null)
        {
            DebugLogger.LogError(LOG_FILE, $"No expression backend found under {vrmRoot.name}");
            return false;
        }

        DebugLogger.Log(LOG_FILE, $"BundleExpressionLibrary found - HasRuntime={bundleExpressionLibrary.HasRuntimeExpressions} ({bundleExpressionLibrary.ExpressionCount}), HasClips={bundleExpressionLibrary.HasClipExpressions}");

        if (bundleExpressionLibrary.HasRuntimeExpressions)
        {
            bundleAvatarRoot = vrmRoot;
            backendType = BackendType.BundleExpressions;
            DebugLogger.Log(LOG_FILE, $"ExpressionManager bound to bundle expressions on {bundleAvatarRoot.name} ({bundleExpressionLibrary.ExpressionCount} expressions)");
            ApplyBundleNeutralIfAvailable();
            return true;
        }

        if (bundleExpressionLibrary.HasClipExpressions)
        {
            bundleAvatarRoot = vrmRoot;
            backendType = BackendType.BundleClips;
            DebugLogger.Log(LOG_FILE, $"ExpressionManager bound to legacy bundle clips on {bundleAvatarRoot.name}");
            PlayNeutralClipIfAvailable();
            return true;
        }

        DebugLogger.LogWarning(LOG_FILE, $"BundleExpressionLibrary exists but has no expressions under {vrmRoot.name}");
        return false;
    }

    public void ChangeExpression(string expressionName, float resetTime = 5f)
    {
        DebugLogger.Log(LOG_FILE, $"ChangeExpression: {expressionName}, reset={resetTime}");

        if (!IsReady)
        {
            DebugLogger.LogError(LOG_FILE, "ChangeExpression aborted because no expression backend is available");
            return;
        }

        CancelResetCoroutine();
        CancelBlendCoroutine();

        if (backendType == BackendType.BundleExpressions && bundleExpressionLibrary != null && coroutineRunner != null)
        {
            if (bundleExpressionLibrary.TryGetExpressionTargets(expressionName, out var targets))
            {
                currentExpression = expressionName;
                blendCoroutine = coroutineRunner.StartCoroutine(BlendToTargets(targets, BLEND_IN_DURATION));
            }
            else
            {
                DebugLogger.LogWarning(LOG_FILE, $"Bundle expression targets not found: {expressionName}");
                return;
            }
        }
        else
        {
            if (!TryApplyExpression(expressionName))
            {
                return;
            }
            currentExpression = expressionName;
        }

        if (resetTime > 0f && coroutineRunner != null)
        {
            resetCoroutine = coroutineRunner.StartCoroutine(ResetAfterDelay(resetTime));
        }
    }

    public void RandomExpression(ExpressionCategory category, float resetTime = 5f)
    {
        string[] expressions = GetExpressionsFromCategory(category);
        if (expressions.Length == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, $"No expressions defined for category {category}");
            return;
        }

        string randomExpression = expressions[Random.Range(0, expressions.Length)];
        ChangeExpression(randomExpression, resetTime);
    }

    public void ResetToNeutral()
    {
        CancelResetCoroutine();
        CancelBlendCoroutine();

        if (backendType == BackendType.BundleClips)
        {
            PlayNeutralClipIfAvailable();
            currentExpression = "Neutral";
            return;
        }

        if (backendType == BackendType.BundleExpressions && bundleExpressionLibrary != null && coroutineRunner != null)
        {
            BundleExpressionLibrary.BlendShapeTarget[] targets;
            if (!bundleExpressionLibrary.TryGetExpressionTargets("Neutral", out targets))
            {
                targets = bundleExpressionLibrary.GetResetTargets();
            }
            blendCoroutine = coroutineRunner.StartCoroutine(BlendToTargets(targets, BLEND_OUT_DURATION));
            currentExpression = "Neutral";
            return;
        }

        ChangeExpression("Neutral", 0f);
    }

    public string GetCurrentExpression()
    {
        return currentExpression;
    }

    private bool TryApplyExpression(string expressionName)
    {
        if (backendType == BackendType.BundleClips)
        {
            return TryApplyBundleClip(expressionName);
        }

        if (backendType == BackendType.BundleExpressions)
        {
            return TryApplyBundleExpression(expressionName);
        }

        BlendShapePreset? preset = GetBlendShapePreset(expressionName);
        if (preset == null)
        {
            DebugLogger.LogError(LOG_FILE, $"Invalid expression name: {expressionName}");
            return false;
        }

        ResetAllExpressions(applyAfterReset: false);
        SetPresetWeight(preset.Value, 1.0f);
        blendShapeProxy.Apply();

        DebugLogger.Log(LOG_FILE, $"Applied expression: {expressionName}");
        return true;
    }

    private bool TryApplyBundleExpression(string expressionName)
    {
        if (bundleExpressionLibrary == null)
        {
            DebugLogger.LogError(LOG_FILE, "Bundle expression library is null");
            return false;
        }

        DebugLogger.Log(LOG_FILE, $"TryApplyBundleExpression: '{expressionName}' (library has {bundleExpressionLibrary.ExpressionCount} runtime expressions, hasRuntime={bundleExpressionLibrary.HasRuntimeExpressions})");

        if (!bundleExpressionLibrary.TryApplyExpression(expressionName))
        {
            DebugLogger.LogWarning(LOG_FILE, $"Bundle expression not found or empty: {expressionName}");
            return false;
        }

        DebugLogger.Log(LOG_FILE, $"Applied bundle expression: {expressionName}");
        return true;
    }

    private bool TryApplyBundleClip(string expressionName)
    {
        if (bundleExpressionLibrary == null)
        {
            DebugLogger.LogError(LOG_FILE, "Bundle expression library is null");
            return false;
        }

        if (!bundleExpressionLibrary.TryGetClip(expressionName, out AnimationClip clip) || clip == null)
        {
            DebugLogger.LogWarning(LOG_FILE, $"Expression clip not found: {expressionName}");
            return false;
        }

        PlayBundleClip(clip);
        DebugLogger.Log(LOG_FILE, $"Applied bundle clip: {expressionName} ({clip.name})");
        return true;
    }

    private void ResetAllExpressions(bool applyAfterReset)
    {
        if (backendType != BackendType.VrmBlendShape || blendShapeProxy == null)
        {
            return;
        }

        foreach (BlendShapePreset preset in ResettablePresets)
        {
            SetPresetWeight(preset, 0.0f);
        }

        if (applyAfterReset)
        {
            blendShapeProxy.Apply();
        }
    }

    private void SetPresetWeight(BlendShapePreset preset, float value)
    {
        BlendShapeKey key = BlendShapeKey.CreateFromPreset(preset);
        blendShapeProxy.ImmediatelySetValue(key, value);
    }

    private BlendShapePreset? GetBlendShapePreset(string expressionName)
    {
        switch (expressionName)
        {
            case "Joy":
                return BlendShapePreset.Joy;
            case "Angry":
                return BlendShapePreset.Angry;
            case "Sorrow":
                return BlendShapePreset.Sorrow;
            case "Fun":
                return BlendShapePreset.Fun;
            case "Neutral":
                return BlendShapePreset.Neutral;
            case "Blink":
                return BlendShapePreset.Blink;
            default:
                return null;
        }
    }

    private string[] GetExpressionsFromCategory(ExpressionCategory category)
    {
        switch (category)
        {
            case ExpressionCategory.Happy:
                return new[] { "Joy", "Fun" };
            case ExpressionCategory.Sad:
                return new[] { "Sorrow" };
            case ExpressionCategory.Angry:
                return new[] { "Angry" };
            case ExpressionCategory.Neutral:
                return new[] { "Neutral", "Blink" };
            case ExpressionCategory.All:
                return new[] { "Joy", "Angry", "Sorrow", "Fun", "Blink" };
            default:
                return new string[0];
        }
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsReady)
        {
            resetCoroutine = null;
            yield break;
        }

        CancelBlendCoroutine();

        if (backendType == BackendType.BundleClips)
        {
            PlayNeutralClipIfAvailable();
            currentExpression = "Neutral";
            DebugLogger.Log(LOG_FILE, "Bundle clip reset to Neutral");
        }
        else if (backendType == BackendType.BundleExpressions && bundleExpressionLibrary != null && coroutineRunner != null)
        {
            BundleExpressionLibrary.BlendShapeTarget[] targets;
            if (!bundleExpressionLibrary.TryGetExpressionTargets("Neutral", out targets))
            {
                targets = bundleExpressionLibrary.GetResetTargets();
            }
            blendCoroutine = coroutineRunner.StartCoroutine(BlendToTargets(targets, BLEND_OUT_DURATION));
            currentExpression = "Neutral";
            DebugLogger.Log(LOG_FILE, "Bundle expression blending to Neutral");
        }
        else
        {
            ResetAllExpressions(applyAfterReset: false);
            SetPresetWeight(BlendShapePreset.Neutral, 1.0f);
            blendShapeProxy.Apply();
            currentExpression = "Neutral";
            DebugLogger.Log(LOG_FILE, "Expression reset to Neutral");
        }

        resetCoroutine = null;
    }

    private void CancelResetCoroutine()
    {
        if (resetCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(resetCoroutine);
        }

        resetCoroutine = null;
    }

    private void CancelBlendCoroutine()
    {
        if (blendCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(blendCoroutine);
        }

        blendCoroutine = null;
    }

    private IEnumerator BlendToTargets(BundleExpressionLibrary.BlendShapeTarget[] targets, float duration)
    {
        if (targets == null || targets.Length == 0)
        {
            blendCoroutine = null;
            yield break;
        }

        // 現在の BlendShape ウェイトをキャプチャ
        float[] fromValues = new float[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].Renderer != null)
            {
                fromValues[i] = targets[i].Renderer.GetBlendShapeWeight(targets[i].Index);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].Renderer == null) continue;
                float value = Mathf.Lerp(fromValues[i], targets[i].Value, t);
                targets[i].Renderer.SetBlendShapeWeight(targets[i].Index, value);
            }

            yield return null;
        }

        // 最終値にスナップ
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].Renderer == null) continue;
            targets[i].Renderer.SetBlendShapeWeight(targets[i].Index, targets[i].Value);
        }

        blendCoroutine = null;
    }

    private void ApplyBundleNeutralIfAvailable()
    {
        if (bundleExpressionLibrary == null || !bundleExpressionLibrary.TryApplyExpression("Neutral"))
        {
            DebugLogger.Log(LOG_FILE, "Neutral bundle expression not available");
        }
    }

    private void PlayNeutralClipIfAvailable()
    {
        if (bundleExpressionLibrary == null)
        {
            return;
        }

        if (bundleExpressionLibrary.TryGetClip("Neutral", out AnimationClip neutralClip) && neutralClip != null)
        {
            PlayBundleClip(neutralClip);
        }
        else
        {
            DebugLogger.Log(LOG_FILE, "Neutral bundle clip not available");
        }
    }

    private void PlayBundleClip(AnimationClip clip)
    {
        if (clip == null || bundleAvatarRoot == null)
        {
            return;
        }

        clip.SampleAnimation(bundleAvatarRoot, 0f);
    }
}
