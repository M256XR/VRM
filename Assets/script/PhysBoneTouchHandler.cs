using System;
using System.Collections;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

/// <summary>
/// Drives VRC PhysBone interaction through PhysBoneManager's grab path.
/// This keeps the chain simulation in charge instead of writing bone transforms directly.
/// </summary>
[DefaultExecutionOrder(32000)]
public class PhysBoneTouchHandler : MonoBehaviour
{
    [SerializeField] private bool enableExperimentalTouch = true;
    [SerializeField] private bool enableVerboseLogging = false;
    [SerializeField] private bool forceAllowGrabbing = true;
    [SerializeField] private bool createManagerIfMissing = true;
    [SerializeField] private float fallbackGrabRadius = 0f;
    [SerializeField] private float directGrabMaxScreenDistance = 32f;
    [SerializeField] private bool showDebugSphere = false;
    [SerializeField] private float debugSphereSize = 0.035f;

    private const string LOG_FILE = "physbone_touch.log";
    private const int GRABBER_ID = 0;

    private static readonly Type ManagerType = Type.GetType("VRC.Dynamics.PhysBoneManager, VRC.Dynamics");
    private static readonly FieldInfo ManagerInstanceField = ManagerType?.GetField("Inst", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
    private static readonly MethodInfo AttemptRayGrabMethod = FindAttemptRayGrabMethod();
    private static readonly MethodInfo AttemptSphereGrabMethod = FindAttemptSphereGrabMethod();
    private static readonly MethodInfo AttemptChainGrabMethod = FindAttemptChainGrabMethod();
    private static readonly MethodInfo ReleaseGrabMethod = FindReleaseGrabMethod();
    private static readonly MethodInfo UpdateGrabsMethod = ManagerType?.GetMethod("UpdateGrabs", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo AddPhysBoneMethod = ManagerType?.GetMethod("AddPhysBone", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo HasPhysBoneMethod = ManagerType?.GetMethod("HasPhysBone", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo ManagerAwakeMethod = ManagerType?.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo InitManagerMethod = ManagerType?.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo AddChainsMethod = ManagerType?.GetMethod("AddChains", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo GetChainsMethod = ManagerType?.GetMethod("GetChains", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo GetOrCreateRootMethod = FindGetOrCreateRootMethod();
    private static readonly FieldInfo EditorInfoField = ManagerType?.GetField("editorInfo", BindingFlags.Public | BindingFlags.Instance);
    private static readonly FieldInfo HasReportedCriticalErrorField = ManagerType?.GetField("hasReportedCriticalError", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo RootsToUpdateField = ManagerType?.GetField("rootsToUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo HasInitField = ManagerType?.GetField("hasInit", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo CompsToAddField = ManagerType?.GetField("compsToAdd", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly Type PhysBoneRootType = Type.GetType("VRC.Dynamics.PhysBoneRoot, VRC.Dynamics");
    private static readonly FieldInfo PhysBoneRootField = typeof(VRCPhysBoneBase).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo PhysBoneRootChainCountField = PhysBoneRootType?.GetField("chainCount", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PhysBoneRootStartMethod = PhysBoneRootType?.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo PhysBoneOnEnableMethod = typeof(VRCPhysBoneBase).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PhysBoneStartMethod = typeof(VRCPhysBoneBase).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo GrabGlobalPositionField = ManagerType?.GetNestedType("Grab", BindingFlags.Public | BindingFlags.NonPublic)
        ?.GetField("globalPosition", BindingFlags.Public | BindingFlags.Instance);

    private Camera mainCamera;
    private GameObject boundAvatar;
    private GameObject registeredAvatar;
    private bool registrationFailed;
    private Bounds avatarBounds;
    private object physBoneManager;
    private Component physBoneRoot;
    private object activeGrab;
    private Plane dragPlane;
    private Vector3 dragPosition;
    private GameObject debugSphere;

    private void Start()
    {
        if (!System.IO.File.Exists(DebugLogger.GetLogPath(LOG_FILE)))
        {
            DebugLogger.InitLog(LOG_FILE);
        }
        DebugLogger.LogSeparator(LOG_FILE, "PhysBoneTouchHandler Start");
        if (!enableExperimentalTouch)
        {
            DebugLogger.Log(LOG_FILE, "PhysBoneTouchHandler disabled by enableExperimentalTouch=false");
            enabled = false;
            return;
        }

        DebugLogger.Log(LOG_FILE, $"PhysBoneManager={ManagerType != null} AttemptRayGrab={AttemptRayGrabMethod != null} AttemptSphereGrab={AttemptSphereGrabMethod != null} AttemptChainGrab={AttemptChainGrabMethod != null} ReleaseGrab={ReleaseGrabMethod != null} AddPhysBone={AddPhysBoneMethod != null} AddChains={AddChainsMethod != null} GetOrCreateRoot={GetOrCreateRootMethod != null} ManagerAwake={ManagerAwakeMethod != null} InitManager={InitManagerMethod != null} PhysBoneRoot={PhysBoneRootType != null} RootField={PhysBoneRootField != null} RootFieldType={PhysBoneRootField?.FieldType.FullName} PhysBoneOnEnable={PhysBoneOnEnableMethod != null} PhysBoneStart={PhysBoneStartMethod != null} GrabGlobalPosition={GrabGlobalPositionField != null}");

        if (showDebugSphere)
        {
            CreateDebugSphere();
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        TryBindAvatar();
        physBoneManager = GetManager();
        TryRegisterAvatarPhysBones();

        if (Input.GetMouseButtonDown(0))
        {
            BeginGrab(Input.mousePosition);
        }

        if (Input.GetMouseButton(0) && activeGrab != null)
        {
            UpdateGrab(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0) || (!Input.GetMouseButton(0) && activeGrab != null))
        {
            ReleaseGrab();
        }

        if (debugSphere != null)
        {
            debugSphere.SetActive(activeGrab != null);
            debugSphere.transform.position = dragPosition;
        }
    }

    private void TryBindAvatar()
    {
        LoadedAvatarMarker marker = FindObjectOfType<LoadedAvatarMarker>();
        GameObject avatarRoot = marker != null ? marker.gameObject : null;
        if (avatarRoot == null || avatarRoot == boundAvatar)
        {
            return;
        }

        boundAvatar = avatarRoot;
        avatarBounds = ComputeAvatarBounds(boundAvatar);
        physBoneRoot = EnsurePhysBoneRoot(boundAvatar);

        VRCPhysBone[] physBones = boundAvatar.GetComponentsInChildren<VRCPhysBone>(true);
        if (forceAllowGrabbing)
        {
            foreach (VRCPhysBone physBone in physBones)
            {
                if (physBone == null)
                {
                    continue;
                }

                physBone.allowGrabbing = VRCPhysBoneBase.AdvancedBool.True;
                physBone.allowPosing = VRCPhysBoneBase.AdvancedBool.True;
                physBone.grabFilter.allowSelf = true;
                physBone.grabFilter.allowOthers = true;
                physBone.poseFilter.allowSelf = true;
                physBone.poseFilter.allowOthers = true;
                physBone.configHasUpdated = true;
            }
        }

        DebugLogger.Log(LOG_FILE, $"Bound avatar {boundAvatar.name}, physBones={physBones.Length}, forceAllowGrabbing={forceAllowGrabbing}, rootComponent={physBoneRoot != null}");
    }

    private void TryRegisterAvatarPhysBones()
    {
        if (boundAvatar == null || boundAvatar == registeredAvatar || registrationFailed || physBoneManager == null || AddPhysBoneMethod == null)
        {
            return;
        }

        try
        {
            InitManagerIfNeeded("before register");
            StartPhysBoneRoot();

            VRCPhysBone[] physBones = boundAvatar.GetComponentsInChildren<VRCPhysBone>(true);
            int added = 0;
            int enabled = 0;
            int roots = 0;
            int rootFieldBefore = 0;
            int rootFieldAfter = 0;
            int hasAfterAdd = 0;
            int nonZeroChainId = 0;
            int totalBones = 0;
            foreach (VRCPhysBone physBone in physBones)
            {
                if (physBone == null)
                {
                    continue;
                }

                if (physBone.enabled)
                {
                    enabled++;
                }

                if (physBone.GetRootTransform() != null)
                {
                    roots++;
                }

                if (PhysBoneRootField != null && PhysBoneRootField.GetValue(physBone) != null)
                {
                    rootFieldBefore++;
                }

                try
                {
                    AssignPhysBoneRoot(physBone);

                    physBone.InitParameters();
                    physBone.InitTransforms(true);
                    InvokePhysBoneLifecycle(physBone);
                }
                catch (Exception exception)
                {
                    DebugLogger.Log(LOG_FILE, $"PhysBone init failed for {physBone.name}: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
                    throw;
                }

                bool hasPhysBone = false;
                if (HasPhysBoneMethod != null)
                {
                    hasPhysBone = (bool)HasPhysBoneMethod.Invoke(physBoneManager, new object[] { physBone });
                }

                if (!hasPhysBone)
                {
                    AddPhysBoneMethod.Invoke(physBoneManager, new object[] { physBone });
                    added++;
                }

                if (HasPhysBoneMethod != null && (bool)HasPhysBoneMethod.Invoke(physBoneManager, new object[] { physBone }))
                {
                    hasAfterAdd++;
                }

                if (physBone.chainId != default)
                {
                    nonZeroChainId++;
                }

                if (physBone.bones != null)
                {
                    totalBones += physBone.bones.Count;
                }

                if (PhysBoneRootField != null && PhysBoneRootField.GetValue(physBone) != null)
                {
                    rootFieldAfter++;
                }
            }

            LogVerbose($"After AddPhysBone: added={added} hasAfterAdd={hasAfterAdd} enabled={enabled} roots={roots} rootBefore={rootFieldBefore} rootAfter={rootFieldAfter} nonZeroChainId={nonZeroChainId} totalBones={totalBones} compsToAdd={GetCollectionCount(CompsToAddField)} hasInit={GetManagerHasInit()} chains={CountChains()}");
            TryForceAddChains();

            registeredAvatar = boundAvatar;
            DebugLogger.Log(LOG_FILE, $"Registered avatar physBones to manager: total={physBones.Length} added={added} chains={CountChains()} rootChainCount={GetPhysBoneRootChainCount()}");
            StartCoroutine(LogChainCountAfterManagerUpdate());
        }
        catch (Exception exception)
        {
            registrationFailed = true;
            DebugLogger.Log(LOG_FILE, $"Register avatar physBones failed: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
        }
    }

    private void InitManagerIfNeeded(string reason)
    {
        if (physBoneManager == null || InitManagerMethod == null)
        {
            return;
        }

        try
        {
            InitManagerMethod.Invoke(physBoneManager, null);
            LogVerbose($"PhysBoneManager.Init called ({reason}), hasInit={GetManagerHasInit()} enabled={IsManagerEnabled()} compsToAdd={GetCollectionCount(CompsToAddField)} rootsToUpdate={GetCollectionCount(RootsToUpdateField)} chains={CountChains()} editorInfo={DescribeEditorInfo()} critical={GetManagerCriticalState()}");
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LOG_FILE, $"PhysBoneManager.Init failed ({reason}): {exception.GetBaseException().Message}");
        }
    }

    private void StartPhysBoneRoot()
    {
        if (physBoneRoot == null || PhysBoneRootStartMethod == null)
        {
            return;
        }

        try
        {
            PhysBoneRootStartMethod.Invoke(physBoneRoot, null);
            LogVerbose($"PhysBoneRoot.Start called, rootChainCount={GetPhysBoneRootChainCount()}");
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LOG_FILE, $"PhysBoneRoot.Start failed: {exception.GetBaseException().Message}");
        }
    }

    private void AssignPhysBoneRoot(VRCPhysBone physBone)
    {
        if (PhysBoneRootField == null)
        {
            return;
        }

        object currentRoot = PhysBoneRootField.GetValue(physBone);
        if (currentRoot != null)
        {
            return;
        }

        object root = CreateCompatiblePhysBoneRoot(physBone);
        if (root == null)
        {
            return;
        }

        PhysBoneRootField.SetValue(physBone, root);
    }

    private object CreateCompatiblePhysBoneRoot(VRCPhysBone physBone)
    {
        Type rootFieldType = PhysBoneRootField?.FieldType;
        if (rootFieldType == null)
        {
            return null;
        }

        Transform rootTransform = physBone.GetRootTransform();
        if (rootTransform == null)
        {
            rootTransform = physBone.rootTransform != null ? physBone.rootTransform : physBone.transform;
        }

        if (GetOrCreateRootMethod != null && physBoneManager != null)
        {
            try
            {
                object managerRoot = GetOrCreateRootMethod.Invoke(physBoneManager, new object[] { rootTransform, true, false });
                if (managerRoot != null && rootFieldType.IsInstanceOfType(managerRoot))
                {
                    return managerRoot;
                }

                DebugLogger.Log(LOG_FILE, $"GetOrCreateRoot returned incompatible root for {physBone.name}: returned={managerRoot?.GetType().FullName} expected={rootFieldType.FullName}");
            }
            catch (Exception exception)
            {
                DebugLogger.Log(LOG_FILE, $"GetOrCreateRoot failed for {physBone.name}: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
            }
        }

        if (physBoneRoot != null && rootFieldType.IsInstanceOfType(physBoneRoot))
        {
            return physBoneRoot;
        }

        DebugLogger.Log(LOG_FILE, $"No compatible PhysBone root for {physBone.name}: expected={rootFieldType.FullName} componentRoot={physBoneRoot?.GetType().FullName}");
        return null;
    }

    private void InvokePhysBoneLifecycle(VRCPhysBone physBone)
    {
        try
        {
            PhysBoneOnEnableMethod?.Invoke(physBone, null);
            PhysBoneStartMethod?.Invoke(physBone, null);
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LOG_FILE, $"PhysBone lifecycle invoke failed for {physBone.name}: {exception.GetBaseException().Message}");
        }
    }

    private void TryForceAddChains()
    {
        if (physBoneManager == null || AddChainsMethod == null)
        {
            return;
        }

        try
        {
            LogVerbose($"Before forced AddChains: hasInit={GetManagerHasInit()} enabled={IsManagerEnabled()} compsToAdd={GetCollectionCount(CompsToAddField)} rootsToUpdate={GetCollectionCount(RootsToUpdateField)} chains={CountChains()} editorInfo={DescribeEditorInfo()} critical={GetManagerCriticalState()}");
            AddChainsMethod.Invoke(physBoneManager, null);
            LogVerbose($"Forced PhysBoneManager.AddChains, hasInit={GetManagerHasInit()} enabled={IsManagerEnabled()} compsToAdd={GetCollectionCount(CompsToAddField)} rootsToUpdate={GetCollectionCount(RootsToUpdateField)} chains={CountChains()} rootChainCount={GetPhysBoneRootChainCount()} editorInfo={DescribeEditorInfo()} critical={GetManagerCriticalState()}");
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LOG_FILE, $"Forced PhysBoneManager.AddChains failed: {exception.GetBaseException().Message}");
        }
    }

    private IEnumerator LogChainCountAfterManagerUpdate()
    {
        for (int i = 1; i <= 5; i++)
        {
            yield return null;
            LogVerbose($"PhysBoneManager chains after {i} frame(s): {CountChains()}");
        }
    }

    private void BeginGrab(Vector2 screenPosition)
    {
        if (physBoneManager == null || AttemptRayGrabMethod == null)
        {
            LogVerbose("BeginGrab skipped: PhysBoneManager or AttemptGrab is missing");
            return;
        }

        ReleaseGrab();

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        object[] args = { GRABBER_ID, ray, Vector3.zero };
        activeGrab = AttemptRayGrabMethod.Invoke(physBoneManager, args);
        if (activeGrab == null)
        {
            activeGrab = TryDirectBoneGrab(screenPosition);
            if (activeGrab == null && fallbackGrabRadius > 0f)
            {
                activeGrab = TryFallbackSphereGrab(screenPosition, ray);
            }

            if (activeGrab == null)
            {
                LogVerbose("AttemptGrab returned null (ray/direct missed)");
                return;
            }
        }
        else
        {
            dragPosition = (Vector3)args[2];
            if (dragPosition == Vector3.zero && TryGetTouchWorldPosition(screenPosition, ray, out Vector3 fallbackPosition))
            {
                dragPosition = fallbackPosition;
            }
        }

        dragPlane = new Plane(mainCamera.transform.forward, dragPosition);
        SetGrabGlobalPosition(dragPosition);
        UpdateGrabsMethod?.Invoke(physBoneManager, null);

        LogVerbose($"Grab started: {DescribeGrab(activeGrab)} hit={dragPosition}");
    }

    private object TryDirectBoneGrab(Vector2 screenPosition)
    {
        if (AttemptChainGrabMethod == null || boundAvatar == null)
        {
            return null;
        }

        VRCPhysBone[] physBones = boundAvatar.GetComponentsInChildren<VRCPhysBone>(true);
        float bestDistanceSq = directGrabMaxScreenDistance * directGrabMaxScreenDistance;
        VRCPhysBone bestPhysBone = null;
        int bestBoneIndex = -1;
        Vector3 bestWorldPosition = default;

        foreach (VRCPhysBone physBone in physBones)
        {
            if (physBone == null || !physBone.enabled || physBone.bones == null || physBone.chainId == default)
            {
                continue;
            }

            for (int i = 0; i < physBone.bones.Count; i++)
            {
                Transform boneTransform = physBone.bones[i].transform;
                if (boneTransform == null)
                {
                    continue;
                }

                Vector3 boneScreenPosition = mainCamera.WorldToScreenPoint(boneTransform.position);
                if (boneScreenPosition.z <= 0f)
                {
                    continue;
                }

                float distanceSq = ((Vector2)boneScreenPosition - screenPosition).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                bestPhysBone = physBone;
                bestBoneIndex = i;
                bestWorldPosition = boneTransform.position;
            }
        }

        if (bestPhysBone == null)
        {
            LogVerbose($"Direct AttemptGrab skipped: no bone within {directGrabMaxScreenDistance:F1}px");
            return null;
        }

        object grab = AttemptChainGrabMethod.Invoke(physBoneManager, new object[] { GRABBER_ID, bestPhysBone.chainId, bestBoneIndex });
        if (grab == null)
        {
            LogVerbose($"Direct AttemptGrab returned null: physBone={bestPhysBone.name} bone={bestBoneIndex} distance={Mathf.Sqrt(bestDistanceSq):F1}px chainId={bestPhysBone.chainId}");
            return null;
        }

        dragPosition = bestWorldPosition;
        LogVerbose($"Direct AttemptGrab succeeded: physBone={bestPhysBone.name} bone={bestBoneIndex} distance={Mathf.Sqrt(bestDistanceSq):F1}px chainId={bestPhysBone.chainId}");
        return grab;
    }

    private object TryFallbackSphereGrab(Vector2 screenPosition, Ray ray)
    {
        if (AttemptSphereGrabMethod == null || !TryGetTouchWorldPosition(screenPosition, ray, out Vector3 grabPosition))
        {
            return null;
        }

        object grab = AttemptSphereGrabMethod.Invoke(physBoneManager, new object[] { GRABBER_ID, grabPosition, fallbackGrabRadius, ray.origin });
        if (grab != null)
        {
            dragPosition = grabPosition;
            LogVerbose($"Sphere AttemptGrab succeeded at {grabPosition} radius={fallbackGrabRadius}");
        }

        return grab;
    }

    private bool TryGetTouchWorldPosition(Vector2 screenPosition, Ray ray, out Vector3 position)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            position = hit.point;
            return true;
        }

        if (boundAvatar == null)
        {
            position = default;
            return false;
        }

        Plane plane = new Plane(mainCamera.transform.forward, avatarBounds.center);
        if (plane.Raycast(ray, out float distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }

        float depth = Mathf.Max(0.1f, Vector3.Dot(avatarBounds.center - ray.origin, mainCamera.transform.forward));
        position = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        return true;
    }

    private void UpdateGrab(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!dragPlane.Raycast(ray, out float distance))
        {
            return;
        }

        dragPosition = ray.GetPoint(distance);
        SetGrabGlobalPosition(dragPosition);
        UpdateGrabsMethod?.Invoke(physBoneManager, null);
    }

    private void ReleaseGrab()
    {
        if (activeGrab == null)
        {
            return;
        }

        try
        {
            if (physBoneManager != null)
            {
                // Prefer the current ReleaseGrab(ChainId) overload when available.
                FieldInfo chainIdField = activeGrab.GetType().GetField("chainId", BindingFlags.Public | BindingFlags.Instance);
                if (chainIdField != null)
                {
                    object chainId = chainIdField.GetValue(activeGrab);
                    MethodInfo releaseByChain = physBoneManager.GetType().GetMethod("ReleaseGrab",
                        new[] { chainIdField.FieldType });
                    if (releaseByChain != null)
                    {
                        releaseByChain.Invoke(physBoneManager, new[] { chainId });
                    }
                    else if (ReleaseGrabMethod != null)
                    {
                        ReleaseGrabMethod.Invoke(physBoneManager, new[] { activeGrab, (object)false });
                    }
                }
            }
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LOG_FILE, $"ReleaseGrab failed: {exception.GetBaseException().Message}");
        }

        activeGrab = null;
    }

    private void SetGrabGlobalPosition(Vector3 worldPosition)
    {
        if (activeGrab == null || GrabGlobalPositionField == null)
        {
            return;
        }

        GrabGlobalPositionField.SetValue(activeGrab, new float3(worldPosition.x, worldPosition.y, worldPosition.z));
    }

    private object GetManager()
    {
        if (ManagerType == null)
        {
            return null;
        }

        VrcSdkRuntimeDynamicsBootstrap.EnsureInitialized();

        object manager = null;
        if (ManagerInstanceField != null)
        {
            manager = ManagerInstanceField.IsStatic
                ? ManagerInstanceField.GetValue(null)
                : ManagerInstanceField.GetValue(FindObjectOfType(ManagerType));
        }

        if (manager == null)
        {
            manager = FindObjectOfType(ManagerType);
        }

        if (manager == null && createManagerIfMissing)
        {
            GameObject managerObject = new GameObject("PhysBoneManager");
            manager = managerObject.AddComponent(ManagerType);
            DontDestroyOnLoad(managerObject);
            TryAssignManagerInstance(manager);
            TryConfigureManagerForSdk(manager);
            DebugLogger.Log(LOG_FILE, "Created PhysBoneManager");
        }

        return manager;
    }

    private static void TryAssignManagerInstance(object manager)
    {
        if (manager == null || ManagerInstanceField == null)
        {
            return;
        }

        try
        {
            if (ManagerInstanceField.IsStatic)
            {
                ManagerInstanceField.SetValue(null, manager);
            }
        }
        catch
        {
        }
    }

    private static void TryConfigureManagerForSdk(object manager)
    {
        if (manager == null)
        {
            return;
        }

        try
        {
            Type type = manager.GetType();
            PropertyInfo isSdkProperty = type.GetProperty("IsSDK", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isSdkProperty != null && isSdkProperty.CanWrite)
            {
                isSdkProperty.SetValue(manager, true);
            }
            else
            {
                FieldInfo isSdkField = type.GetField("IsSDK", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                isSdkField?.SetValue(manager, true);
            }

            if (manager is Behaviour behaviour)
            {
                behaviour.enabled = true;
            }

            ManagerAwakeMethod?.Invoke(manager, null);
            InitManagerMethod?.Invoke(manager, null);
        }
        catch
        {
        }
    }


    private void LogVerbose(string message)
    {
        if (enableVerboseLogging)
        {
            DebugLogger.Log(LOG_FILE, message);
        }
    }

    public void SetVerboseLogging(bool enabled)
    {
        enableVerboseLogging = enabled;
    }

    private static Component EnsurePhysBoneRoot(GameObject avatarRoot)
    {
        if (avatarRoot == null || PhysBoneRootType == null)
        {
            return null;
        }

        Component root = avatarRoot.GetComponent(PhysBoneRootType);
        if (root == null)
        {
            root = avatarRoot.AddComponent(PhysBoneRootType);
            DebugLogger.Log(LOG_FILE, "Created PhysBoneRoot on avatar");
        }

        return root;
    }

    private object GetManagerHasInit()
    {
        return physBoneManager != null && HasInitField != null ? HasInitField.GetValue(physBoneManager) : "unknown";
    }

    private int GetCollectionCount(FieldInfo field)
    {
        if (physBoneManager == null || field == null)
        {
            return -1;
        }

        object value = field.GetValue(physBoneManager);
        if (value is ICollection collection)
        {
            return collection.Count;
        }

        return -1;
    }

    private object IsManagerEnabled()
    {
        return physBoneManager is Behaviour behaviour ? behaviour.enabled : "unknown";
    }

    private object GetManagerCriticalState()
    {
        return physBoneManager != null && HasReportedCriticalErrorField != null ? HasReportedCriticalErrorField.GetValue(physBoneManager) : "unknown";
    }

    private string DescribeEditorInfo()
    {
        if (physBoneManager == null || EditorInfoField == null)
        {
            return "unknown";
        }

        object editorInfo = EditorInfoField.GetValue(physBoneManager);
        if (editorInfo == null)
        {
            return "null";
        }

        Type type = editorInfo.GetType();
        return $"chains={GetFieldValue(type, editorInfo, "chainCount")},bones={GetFieldValue(type, editorInfo, "boneCount")},roots={GetFieldValue(type, editorInfo, "rootCount")},shapes={GetFieldValue(type, editorInfo, "shapeCount")},chainCap={GetFieldValue(type, editorInfo, "chainCapacity")},boneCap={GetFieldValue(type, editorInfo, "boneCapacity")},rootCap={GetFieldValue(type, editorInfo, "rootCapacity")}";
    }

    private static object GetFieldValue(Type type, object target, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? field.GetValue(target) : "missing";
    }

    private object GetPhysBoneRootChainCount()
    {
        return physBoneRoot != null && PhysBoneRootChainCountField != null ? PhysBoneRootChainCountField.GetValue(physBoneRoot) : "unknown";
    }

    private int CountChains()
    {
        if (physBoneManager == null || GetChainsMethod == null)
        {
            return -1;
        }

        int count = 0;
        object enumerable = GetChainsMethod.Invoke(physBoneManager, null);
        if (enumerable is IEnumerator enumerator)
        {
            int enumeratorCount = 0;
            while (enumerator.MoveNext())
            {
                enumeratorCount++;
            }

            return enumeratorCount;
        }

        if (enumerable is IEnumerable chains)
        {
            foreach (object _ in chains)
            {
                count++;
            }
        }

        return count;
    }

    private static MethodInfo FindAttemptRayGrabMethod()
    {
        if (ManagerType == null)
        {
            return null;
        }

        foreach (MethodInfo method in ManagerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name != "AttemptGrab")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 3 &&
                parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType == typeof(Ray) &&
                parameters[2].ParameterType.IsByRef)
            {
                return method;
            }
        }

        return null;
    }

    private static MethodInfo FindAttemptSphereGrabMethod()
    {
        if (ManagerType == null)
        {
            return null;
        }

        foreach (MethodInfo method in ManagerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name != "AttemptGrab")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType == typeof(Vector3) &&
                parameters[2].ParameterType == typeof(float) &&
                parameters[3].ParameterType == typeof(Vector3))
            {
                return method;
            }
        }

        return null;
    }

    private static MethodInfo FindAttemptChainGrabMethod()
    {
        if (ManagerType == null)
        {
            return null;
        }

        foreach (MethodInfo method in ManagerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name != "AttemptGrab")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 3 &&
                parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType.FullName == "VRC.Dynamics.ChainId" &&
                parameters[2].ParameterType == typeof(int))
            {
                return method;
            }
        }

        return null;
    }

    private static MethodInfo FindReleaseGrabMethod()
    {
        if (ManagerType == null)
        {
            return null;
        }

        Type grabType = ManagerType.GetNestedType("Grab", BindingFlags.Public | BindingFlags.NonPublic);
        foreach (MethodInfo method in ManagerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name != "ReleaseGrab")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 2 &&
                parameters[0].ParameterType == grabType &&
                parameters[1].ParameterType == typeof(bool))
            {
                return method;
            }
        }

        return null;
    }

    private static MethodInfo FindGetOrCreateRootMethod()
    {
        if (ManagerType == null)
        {
            return null;
        }

        foreach (MethodInfo method in ManagerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.Name != "GetOrCreateRoot")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 3 &&
                parameters[0].ParameterType == typeof(Transform) &&
                parameters[1].ParameterType == typeof(bool) &&
                parameters[2].ParameterType == typeof(bool))
            {
                return method;
            }
        }

        return null;
    }

    private static string DescribeGrab(object grab)
    {
        Type type = grab.GetType();
        FieldInfo chainId = type.GetField("chainId", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo bone = type.GetField("bone", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo playerId = type.GetField("playerId", BindingFlags.Public | BindingFlags.Instance);

        return $"chainId={chainId?.GetValue(grab)} bone={bone?.GetValue(grab)} playerId={playerId?.GetValue(grab)}";
    }

    private void CreateDebugSphere()
    {
        debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.name = "PhysBoneTouchDebug";
        debugSphere.transform.localScale = Vector3.one * debugSphereSize;

        Destroy(debugSphere.GetComponent<Collider>());

        MeshRenderer renderer = debugSphere.GetComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(1f, 0f, 0f, 0.5f)
        };
        renderer.material = material;

        debugSphere.SetActive(false);
        DontDestroyOnLoad(debugSphere);
    }

    private static Bounds ComputeAvatarBounds(GameObject avatarRoot)
    {
        Renderer[] renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(avatarRoot.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void OnDisable()
    {
        ReleaseGrab();
    }

    private void OnDestroy()
    {
        ReleaseGrab();

        if (debugSphere != null)
        {
            Destroy(debugSphere);
        }
    }
}
