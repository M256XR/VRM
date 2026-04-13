using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using VRM;

public static class VRMLoaderHelper
{
    private const string LOG_FILE = "vrm_loader_helper.log";

    static VRMLoaderHelper()
    {
        DebugLogger.InitLog(LOG_FILE);
    }

    public static async Task<GameObject> LoadVRMAsync(string vrmPath)
    {
        DebugLogger.LogSeparator(LOG_FILE, "LoadVRMAsync START");
        DebugLogger.Log(LOG_FILE, $"VRM path: {vrmPath}");

        if (!File.Exists(vrmPath))
        {
            DebugLogger.LogError(LOG_FILE, $"File not found: {vrmPath}");
            return null;
        }

        try
        {
            var instance = await VrmUtility.LoadAsync(vrmPath);
            if (instance == null || instance.Root == null)
            {
                DebugLogger.LogError(LOG_FILE, "VRM instance is null");
                return null;
            }

            GameObject vrmRoot = instance.Root;
            DebugLogger.Log(LOG_FILE, $"VRM loaded: {vrmRoot.name}");
            PrepareLoadedAvatar(vrmRoot);

            DebugLogger.LogSeparator(LOG_FILE, "LoadVRMAsync END");
            return vrmRoot;
        }
        catch (System.Exception e)
        {
            DebugLogger.LogException(LOG_FILE, e);
            return null;
        }
    }

    public static void SetupVRMTransform(GameObject vrmRoot, Transform spawnPoint)
    {
        if (vrmRoot == null)
        {
            DebugLogger.LogError(LOG_FILE, "vrmRoot is null");
            return;
        }

        if (spawnPoint != null)
        {
            vrmRoot.transform.position = spawnPoint.position;
            vrmRoot.transform.rotation = spawnPoint.rotation;
            vrmRoot.transform.localScale = spawnPoint.localScale;
            DebugLogger.Log(LOG_FILE, $"VRM positioned at: {vrmRoot.transform.position}");
        }
        else
        {
            vrmRoot.transform.position = Vector3.zero;
            vrmRoot.transform.eulerAngles = new Vector3(0, 180, 0);
            vrmRoot.transform.localScale = Vector3.one;
            DebugLogger.Log(LOG_FILE, "VRM positioned at default (0,0,0)");
        }
    }

    public static void PrepareLoadedAvatar(GameObject avatarRoot)
    {
        if (avatarRoot == null)
        {
            DebugLogger.LogError(LOG_FILE, "avatarRoot is null");
            return;
        }

        SetLayerRecursively(avatarRoot, 0);
        DebugLogger.Log(LOG_FILE, "Avatar layer set to 0 (Default)");

        if (avatarRoot.GetComponent<LoadedAvatarMarker>() == null)
        {
            avatarRoot.AddComponent<LoadedAvatarMarker>();
        }

        LogRendererInfo(avatarRoot);
    }

    public static void SetupAnimator(GameObject vrmRoot)
    {
        if (vrmRoot == null)
        {
            DebugLogger.LogError(LOG_FILE, "vrmRoot is null");
            return;
        }

        VRMBlendShapeProxy vrmProxy = vrmRoot.GetComponent<VRMBlendShapeProxy>();
        if (vrmProxy == null)
        {
            vrmProxy = vrmRoot.GetComponentInChildren<VRMBlendShapeProxy>(true);
        }

        bool isVrmAvatar = vrmProxy != null;

        Animator animator = vrmRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = vrmRoot.GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "Animator not found on avatar");
            return;
        }

        if (!isVrmAvatar && animator.runtimeAnimatorController != null)
        {
            DebugLogger.Log(LOG_FILE, $"Keeping existing animator controller for native avatar: {animator.runtimeAnimatorController.name}");
            return;
        }

        RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("VRMAnimator");
        if (animatorController == null)
        {
            DebugLogger.LogError(LOG_FILE, "VRMAnimator not found in Resources folder");
            return;
        }

        animator.runtimeAnimatorController = animatorController;
        DebugLogger.Log(LOG_FILE, isVrmAvatar
            ? "Animator Controller applied to VRM avatar"
            : "Animator Controller applied to native avatar");
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void LogRendererInfo(GameObject avatarRoot)
    {
        Renderer[] renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
        DebugLogger.Log(LOG_FILE, $"Avatar has {renderers.Length} renderers");

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.enabled = true;
            DebugLogger.Log(LOG_FILE, $"  Renderer[{i}]: {renderer.name}, layer: {renderer.gameObject.layer}");
        }
    }
}
