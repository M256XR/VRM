using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

public static class VrcSdkRuntimeDynamicsBootstrap
{
    private const string LogFile = "physbone_touch.log";
    private static bool driverCreated;
    private static bool initialized;
    private static readonly Type DynamicsComponentType = Type.GetType("VRC.Dynamics.DynamicsComponent, VRC.Dynamics");
    private static readonly Type DynamicsUsageType = Type.GetType("VRC.Dynamics.DynamicsUsage, VRC.Dynamics");
    private static readonly object AvatarDynamicsUsage = DynamicsUsageType != null ? Enum.Parse(DynamicsUsageType, "Avatar") : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInit()
    {
        EnsureInitialized();
    }

    [Preserve]
    public static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        DebugLogger.InitLog(LogFile);
        DebugLogger.Log(LogFile, $"EnsureInitialized enter: dynamicsComponent={DynamicsComponentType != null} dynamicsUsage={DynamicsUsageType != null} avatarUsage={AvatarDynamicsUsage != null}");

        try
        {
            initialized = true;
            SetStaticMember(DynamicsComponentType, "DefaultUsage", AvatarDynamicsUsage);

            ContactBase.OnInitialize = contact =>
            {
                SetInstanceMember(contact, "Usage", AvatarDynamicsUsage);
                return true;
            };

            VRCPhysBoneBase.OnInitialize = physBone =>
            {
                SetInstanceMember(physBone, "Usage", AvatarDynamicsUsage);
            };

            SubscribeColliderPreShapeInitialize();

            if (ContactManager.Inst == null)
            {
                GameObject contactManagerObject = new GameObject("ContactManager");
                UnityEngine.Object.DontDestroyOnLoad(contactManagerObject);
                ContactManager.Inst = contactManagerObject.AddComponent<ContactManager>();
                contactManagerObject.hideFlags = HideFlags.HideInHierarchy;
            }

            if (PhysBoneManager.Inst == null)
            {
                GameObject physBoneManagerObject = new GameObject("PhysBoneManager");
                UnityEngine.Object.DontDestroyOnLoad(physBoneManagerObject);
                PhysBoneManager.Inst = physBoneManagerObject.AddComponent<PhysBoneManager>();
                physBoneManagerObject.hideFlags = HideFlags.HideInHierarchy;
            }

            PhysBoneManager.Inst.IsSDK = true;
            PhysBoneManager.Inst.Init();
            EnsureDriver();

            DebugLogger.Log(LogFile, $"VRC SDK runtime dynamics bootstrap initialized for player: physBoneManager={PhysBoneManager.Inst != null} enabled={PhysBoneManager.Inst != null && PhysBoneManager.Inst.enabled}");
        }
        catch (Exception exception)
        {
            initialized = false;
            DebugLogger.Log(LogFile, $"EnsureInitialized failed: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
            throw;
        }
    }

    private static void EnsureDriver()
    {
        if (driverCreated || UnityEngine.Object.FindObjectOfType<VrcSdkRuntimeDynamicsDriver>() != null)
        {
            driverCreated = true;
            return;
        }

        GameObject driverObject = new GameObject("VrcSdkRuntimeDynamicsDriver");
        UnityEngine.Object.DontDestroyOnLoad(driverObject);
        driverObject.hideFlags = HideFlags.HideInHierarchy;
        driverObject.AddComponent<VrcSdkRuntimeDynamicsDriver>();
        driverCreated = true;
    }

    private static void SetStaticMember(Type type, string memberName, object value)
    {
        if (type == null)
        {
            DebugLogger.Log(LogFile, $"SetStaticMember skipped: type missing for {memberName}");
            return;
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        PropertyInfo property = type.GetProperty(memberName, flags);
        if (property != null && property.CanWrite)
        {
            property.SetValue(null, value);
            return;
        }

        FieldInfo field = type.GetField(memberName, flags);
        field?.SetValue(null, value);
    }

    private static void SetInstanceMember(object target, string memberName, object value)
    {
        if (target == null)
        {
            return;
        }

        Type type = target.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        while (type != null)
        {
            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return;
            }

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }
    }

    private static void SubscribeColliderPreShapeInitialize()
    {
        EventInfo evt = typeof(VRCPhysBoneColliderBase).GetEvent("OnPreShapeInitialize", BindingFlags.Public | BindingFlags.Static);
        if (evt == null)
        {
            DebugLogger.Log(LogFile, "SubscribeColliderPreShapeInitialize skipped: event missing");
            return;
        }

        MethodInfo invokeMethod = typeof(VrcSdkRuntimeDynamicsBootstrap).GetMethod(nameof(OnColliderPreShapeInitialize), BindingFlags.NonPublic | BindingFlags.Static);
        if (invokeMethod == null)
        {
            DebugLogger.Log(LogFile, "SubscribeColliderPreShapeInitialize skipped: handler missing");
            return;
        }

        Delegate callback = Delegate.CreateDelegate(evt.EventHandlerType, invokeMethod);
        evt.AddEventHandler(null, callback);
    }

    private static void OnColliderPreShapeInitialize(VRCPhysBoneColliderBase collider)
    {
        SetInstanceMember(collider, "Usage", AvatarDynamicsUsage);
    }
}

[DefaultExecutionOrder(32100)]
public sealed class VrcSdkRuntimeDynamicsDriver : MonoBehaviour
{
    private const string LogFile = "physbone_touch.log";
    private static readonly Type SchedulerType = Type.GetType("VRC.Dynamics.VRCDynamicsScheduler, VRC.Dynamics");
    private static readonly MethodInfo SchedulerInitializeMethod = SchedulerType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo UpdateConstraintsMethod = SchedulerType?.GetMethod("UpdateConstraints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(bool) }, null);

    private int tickCount;
    private bool loggedFirstTick;
    private bool loggedMissingMethod;
    private bool loggedAssemblyProbe;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DebugLogger.InitLog(LogFile);
        TryInitializeScheduler();
        DebugLogger.Log(LogFile, $"VRC SDK runtime dynamics driver initialized: scheduler={SchedulerType != null} initialize={SchedulerInitializeMethod != null} updateConstraints={UpdateConstraintsMethod != null}");
    }

    private void LateUpdate()
    {
        if (PhysBoneManager.Inst == null)
        {
            if (!loggedMissingMethod)
            {
                loggedMissingMethod = true;
                DebugLogger.Log(LogFile, "VRC SDK runtime dynamics driver waiting: PhysBoneManager.Inst is null");
            }
            return;
        }

        if (!PhysBoneManager.Inst.enabled)
        {
            PhysBoneManager.Inst.enabled = true;
            DebugLogger.Log(LogFile, "VRC SDK runtime dynamics driver re-enabled PhysBoneManager.Inst");
        }

        if (UpdateConstraintsMethod == null)
        {
            if (!loggedMissingMethod)
            {
                loggedMissingMethod = true;
                DebugLogger.Log(LogFile, "VRC SDK runtime dynamics driver skipped: UpdateConstraints method missing");
                LogDynamicsAssemblyProbe();
            }

            return;
        }

        try
        {
            UpdateConstraintsMethod.Invoke(null, new object[] { true });
            tickCount++;

            if (!loggedFirstTick || tickCount % 120 == 0)
            {
                loggedFirstTick = true;
                DebugLogger.Log(LogFile, $"VRC SDK runtime dynamics driver ticked: frames={tickCount} physBoneManagerEnabled={PhysBoneManager.Inst.enabled}");
            }
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LogFile, $"VRC SDK runtime dynamics driver tick failed: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
            enabled = false;
        }
    }

    private static void TryInitializeScheduler()
    {
        try
        {
            SchedulerInitializeMethod?.Invoke(null, null);
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LogFile, $"VRC SDK runtime dynamics scheduler initialize failed: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
        }
    }

    private void LogDynamicsAssemblyProbe()
    {
        if (loggedAssemblyProbe)
        {
            return;
        }

        loggedAssemblyProbe = true;

        try
        {
            Assembly dynamicsAssembly = typeof(PhysBoneManager).Assembly;
            DebugLogger.Log(LogFile, $"Dynamics assembly probe: {dynamicsAssembly.FullName}");

            Type[] interestingTypes = dynamicsAssembly.GetTypes()
                .Where(type =>
                    type.FullName != null &&
                    (type.FullName.Contains("Scheduler", StringComparison.OrdinalIgnoreCase) ||
                     type.FullName.Contains("Constraint", StringComparison.OrdinalIgnoreCase) ||
                     type.FullName.Contains("Dynamics", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(type => type.FullName)
                .Take(32)
                .ToArray();

            foreach (Type type in interestingTypes)
            {
                string methods = string.Join(", ",
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Select(method => method.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .Take(24));
                DebugLogger.Log(LogFile, $"Dynamics type: {type.FullName} methods=[{methods}]");
            }

            string managerMethods = string.Join(", ",
                typeof(PhysBoneManager)
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(method => method.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .Take(64));
            DebugLogger.Log(LogFile, $"PhysBoneManager methods=[{managerMethods}]");
        }
        catch (Exception exception)
        {
            DebugLogger.Log(LogFile, $"Dynamics assembly probe failed: {exception.GetBaseException().GetType().Name}: {exception.GetBaseException().Message}");
        }
    }
}
