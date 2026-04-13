using UnityEngine;
using UnityEngine.UI;
using VRM;

public class WallpaperTouchTest : MonoBehaviour
{
    [Header("References")]
    public Text statusText;
    public Camera mainCamera;

    [Header("Tap Reactions")]
    [SerializeField] private string[] headExpressions;
    [SerializeField] private string[] bodyExpressions;
    [SerializeField] private string[] handExpressions;
    [SerializeField] private float expressionResetSeconds = 4f;
    [SerializeField] private float tapCooldownSeconds = 0.2f;

    private const string LOG_FILE = "wallpaper_touch.log";

    private int tapCount;
    private string lastHitPart = "None";
    private float lastReactionTime = -10f;
    private ExpressionManager expressionManager;
    private GameObject currentVRM;

    private void Start()
    {
        DebugLogger.InitLog(LOG_FILE);
        DebugLogger.LogSeparator(LOG_FILE, "WallpaperTouchTest Start");

        NormalizeReactionSettings();
        ResolveCamera();
        RefreshVrmBinding();
        UpdateStatus("Tap the avatar");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        tapCount++;
        HandleTap(Input.mousePosition);
    }

    private void HandleTap(Vector2 screenPosition)
    {
        ResolveCamera();
        if (mainCamera == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "Main camera is not available");
            UpdateStatus("Camera missing");
            return;
        }

        if (!EnsureVrmReady())
        {
            DebugLogger.LogWarning(LOG_FILE, "VRM is not ready for tap interaction");
            UpdateStatus("Avatar not ready");
            return;
        }

        Physics.SyncTransforms();

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            lastHitPart = "Miss";
            DebugLogger.Log(LOG_FILE, $"Tap missed at {screenPosition}");
            UpdateStatus("Tap the avatar");
            return;
        }

        if (!TryResolveBodyPart(hit.collider, out string bodyPart))
        {
            lastHitPart = hit.collider.gameObject.name;
            DebugLogger.Log(LOG_FILE, $"Tap hit {hit.collider.gameObject.name} but no VRM body part was resolved");
            UpdateStatus("No reaction target");
            return;
        }

        lastHitPart = bodyPart;
        PlayReaction(bodyPart);
    }

    private void ResolveCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private bool EnsureVrmReady()
    {
        if (currentVRM == null || expressionManager == null || !expressionManager.IsReady)
        {
            return RefreshVrmBinding();
        }

        return true;
    }

    private bool RefreshVrmBinding()
    {
        LoadedAvatarMarker avatarMarker = FindObjectOfType<LoadedAvatarMarker>();
        GameObject avatarRoot = avatarMarker != null ? avatarMarker.gameObject : null;
        if (avatarRoot == null)
        {
            VRMBlendShapeProxy vrmProxy = FindObjectOfType<VRMBlendShapeProxy>();
            if (vrmProxy != null)
            {
                avatarRoot = vrmProxy.transform.root.gameObject;
            }
        }

        if (avatarRoot == null)
        {
            currentVRM = null;
            expressionManager = null;
            return false;
        }

        bool avatarChanged = currentVRM != avatarRoot;
        currentVRM = avatarRoot;

        if (expressionManager == null)
        {
            expressionManager = new ExpressionManager(currentVRM, this);
        }
        else if (avatarChanged || !expressionManager.IsReady)
        {
            expressionManager.Rebind(currentVRM, this);
        }

        EnsureTouchTargets();
        DebugLogger.Log(LOG_FILE, $"Avatar ready: {currentVRM.name}");
        return expressionManager != null && expressionManager.IsReady;
    }

    private void EnsureTouchTargets()
    {
        if (currentVRM == null)
        {
            return;
        }

        Collider[] colliders = currentVRM.GetComponentsInChildren<Collider>(true);
        bool hasTaggedCollider = false;

        foreach (Collider collider in colliders)
        {
            if (collider.GetComponent<VRMBodyPart>() != null || collider.GetComponentInParent<VRMBodyPart>() != null)
            {
                hasTaggedCollider = true;
                break;
            }
        }

        if (colliders.Length == 0 || !hasTaggedCollider)
        {
            VRMColliderSetup.SetupColliders(currentVRM);
        }
    }

    private bool TryResolveBodyPart(Collider collider, out string bodyPart)
    {
        bodyPart = null;

        VRMBodyPart taggedBodyPart = collider.GetComponent<VRMBodyPart>();
        if (taggedBodyPart == null)
        {
            taggedBodyPart = collider.GetComponentInParent<VRMBodyPart>();
        }

        if (taggedBodyPart != null && !string.IsNullOrEmpty(taggedBodyPart.bodyPartName))
        {
            bodyPart = taggedBodyPart.bodyPartName;
            return true;
        }

        if (currentVRM == null || !collider.transform.IsChildOf(currentVRM.transform))
        {
            return false;
        }

        Transform current = collider.transform;
        while (current != null && current != currentVRM.transform.parent)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("hand") || name.Contains("wrist") || name.Contains("thumb") ||
                name.Contains("index") || name.Contains("middle") || name.Contains("ring") ||
                name.Contains("little"))
            {
                bodyPart = "Hand";
                return true;
            }

            if (name.Contains("head") || name.Contains("face") || name.Contains("neck"))
            {
                bodyPart = "Head";
                return true;
            }

            if (name.Contains("chest") || name.Contains("spine") || name.Contains("hips") || name.Contains("body"))
            {
                bodyPart = "Body";
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void PlayReaction(string bodyPart)
    {
        if (expressionManager == null || !expressionManager.IsReady)
        {
            return;
        }

        if (Time.unscaledTime - lastReactionTime < tapCooldownSeconds)
        {
            UpdateStatus("Tap cooldown");
            return;
        }

        string expressionName = PickExpression(bodyPart);
        if (string.IsNullOrEmpty(expressionName))
        {
            DebugLogger.LogWarning(LOG_FILE, $"No expression configured for body part: {bodyPart}");
            return;
        }

        lastReactionTime = Time.unscaledTime;
        expressionManager.ChangeExpression(expressionName, expressionResetSeconds);

        DebugLogger.Log(LOG_FILE, $"Tap reaction: {bodyPart} -> {expressionManager.GetCurrentExpression()}");
        UpdateStatus($"Tapped {bodyPart} -> {expressionName}");
    }

    private void NormalizeReactionSettings()
    {
        if (headExpressions == null || headExpressions.Length == 0)
        {
            headExpressions = new[] { "Joy", "Fun" };
        }

        if (bodyExpressions == null || bodyExpressions.Length == 0)
        {
            bodyExpressions = new[] { "Angry", "Sorrow" };
        }

        if (handExpressions == null || handExpressions.Length == 0)
        {
            handExpressions = new[] { "Fun", "Blink" };
        }

        if (expressionResetSeconds <= 0f)
        {
            expressionResetSeconds = 4f;
        }

        if (tapCooldownSeconds < 0f)
        {
            tapCooldownSeconds = 0f;
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        string expressionName = expressionManager != null ? expressionManager.GetCurrentExpression() : "None";
        statusText.text = $"Taps: {tapCount}\nLast: {lastHitPart}\nExpression: {expressionName}\n{message}";
        statusText.fontSize = 30;
        statusText.color = Color.yellow;
    }

    private string PickExpression(string bodyPart)
    {
        switch (bodyPart)
        {
            case "Head":
                return PickRandomExpression(headExpressions);
            case "Body":
                return PickRandomExpression(bodyExpressions);
            case "Hand":
                return PickRandomExpression(handExpressions);
            default:
                return null;
        }
    }

    private string PickRandomExpression(string[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, expressions.Length);
        return expressions[index];
    }
}
