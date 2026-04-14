using System;
using System.Reflection;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

public static class VrcSdkRuntimeDynamicsBootstrap
{
    private const string LogFile = "physbone_touch.log";
    private static bool driverCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInit()
    {
        SetStaticMember(typeof(DynamicsComponent), "DefaultUsage", DynamicsUsage.Avatar);

        ContactBase.OnInitialize = contact =>
        {
            SetInstanceMember(contact, "Usage", DynamicsUsage.Avatar);
            return true;
        };

        VRCPhysBoneBase.OnInitialize = physBone =>
        {
            SetInstanceMember(physBone, "Usage", DynamicsUsage.Avatar);
        };

        VRCPhysBoneColliderBase.OnPreShapeInitialize += collider =>
        {
            SetInstanceMember(collider, "Usage", DynamicsUsage.Avatar);
        };

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

        DebugLogger.InitLog(LogFile);
        DebugLogger.Log(LogFile, "VRC SDK runtime dynamics bootstrap initialized for player");
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

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        TryInitializeScheduler();
        DebugLogger.InitLog(LogFile);
        DebugLogger.Log(LogFile, $"VRC SDK runtime dynamics driver initialized: scheduler={SchedulerType != null} initialize={SchedulerInitializeMethod != null} updateConstraints={UpdateConstraintsMethod != null}");
    }

    private void LateUpdate()
    {
        if (PhysBoneManager.Inst == null)
        {
            return;
        }

        if (UpdateConstraintsMethod == null)
        {
            if (!loggedMissingMethod)
            {
                loggedMissingMethod = true;
                DebugLogger.Log(LogFile, "VRC SDK runtime dynamics driver skipped: UpdateConstraints method missing");
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
}
