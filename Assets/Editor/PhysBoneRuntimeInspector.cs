using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

public static class PhysBoneRuntimeInspector
{
    private static readonly Type ManagerType = Type.GetType("VRC.Dynamics.PhysBoneManager, VRC.Dynamics");
    private static readonly FieldInfo ManagerInstanceField = ManagerType?.GetField("Inst", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
    private static readonly MethodInfo HasPhysBoneMethod = ManagerType?.GetMethod("HasPhysBone", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo GetChainsMethod = ManagerType?.GetMethod("GetChains", BindingFlags.Public | BindingFlags.Instance);
    private static readonly FieldInfo CompsToAddField = ManagerType?.GetField("compsToAdd", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo CompsToRemoveField = ManagerType?.GetField("compsToRemove", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo HasInitField = ManagerType?.GetField("hasInit", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo RootField = typeof(VRCPhysBoneBase).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo HasInitParamsField = typeof(VRCPhysBoneBase).GetField("hasInitParams", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo HasInitTransformField = typeof(VRCPhysBoneBase).GetField("hasInitTransform", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly Type PhysBoneRootType = Type.GetType("VRC.Dynamics.PhysBoneRoot, VRC.Dynamics");
    private static readonly MethodInfo ManagerAwakeMethod = ManagerType?.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo ManagerInitMethod = ManagerType?.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo ManagerAddPhysBoneMethod = ManagerType?.GetMethod("AddPhysBone", BindingFlags.Public | BindingFlags.Instance);
    private static readonly MethodInfo ManagerAddChainsMethod = ManagerType?.GetMethod("AddChains", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PbOnEnableMethod = typeof(VRCPhysBoneBase).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PbStartMethod = typeof(VRCPhysBoneBase).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo RootStartMethod = PhysBoneRootType?.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);

    [MenuItem("Tools/VRM Wallpaper/Dump Runtime PhysBone State")]
    public static void DumpRuntimeState()
    {
        DumpRuntimeStateNow("manual");
    }

    [MenuItem("Tools/VRM Wallpaper/Dump Runtime PhysBone State After 10 Frames")]
    public static void DumpRuntimeStateAfterFrames()
    {
        EditorApplication.delayCall += () => WaitFramesThenDump(10);
    }

    [MenuItem("Tools/VRM Wallpaper/Force Init VRC PhysBone Then Dump")]
    public static void ForceInitThenDump()
    {
        GameObject avatar = Selection.activeGameObject;
        if (avatar == null)
        {
            LoadedAvatarMarker marker = UnityEngine.Object.FindObjectOfType<LoadedAvatarMarker>();
            avatar = marker != null ? marker.gameObject : null;
        }

        if (avatar == null)
        {
            Debug.LogWarning("[PhysBoneRuntimeInspector] Select an avatar root or enter Play Mode with a loaded avatar.");
            return;
        }

        object manager = EnsureManager();
        Component root = EnsurePhysBoneRoot(avatar);
        VRCPhysBone[] physBones = avatar.GetComponentsInChildren<VRCPhysBone>(true);

        ManagerAwakeMethod?.Invoke(manager, null);
        ManagerInitMethod?.Invoke(manager, null);
        RootStartMethod?.Invoke(root, null);

        int invoked = 0;
        foreach (VRCPhysBone pb in physBones)
        {
            if (RootField != null && root != null)
            {
                RootField.SetValue(pb, root);
            }

            pb.InitParameters();
            pb.InitTransforms(true);
            PbOnEnableMethod?.Invoke(pb, null);
            PbStartMethod?.Invoke(pb, null);
            ManagerAddPhysBoneMethod?.Invoke(manager, new object[] { pb });
            invoked++;
        }

        ManagerAddChainsMethod?.Invoke(manager, null);
        Debug.Log($"[PhysBoneRuntimeInspector] Force init invoked for {invoked} PhysBones.");
        DumpRuntimeStateNow("force_init");
    }

    private static async void WaitFramesThenDump(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        DumpRuntimeStateNow($"after_{frames}_frames");
    }

    private static void DumpRuntimeStateNow(string suffix)
    {
        GameObject avatar = Selection.activeGameObject;
        if (avatar == null)
        {
            LoadedAvatarMarker marker = UnityEngine.Object.FindObjectOfType<LoadedAvatarMarker>();
            avatar = marker != null ? marker.gameObject : null;
        }

        if (avatar == null)
        {
            Debug.LogWarning("[PhysBoneRuntimeInspector] Select an avatar root or enter Play Mode with a loaded avatar.");
            return;
        }

        object manager = GetManager();
        VRCPhysBone[] physBones = avatar.GetComponentsInChildren<VRCPhysBone>(true);
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("PhysBone Runtime State");
        sb.AppendLine($"time,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"playMode,{Application.isPlaying}");
        sb.AppendLine($"avatar,{avatar.name}");
        sb.AppendLine($"managerExists,{manager != null}");
        sb.AppendLine($"managerHasInit,{GetFieldValue(HasInitField, manager)}");
        sb.AppendLine($"managerCompsToAdd,{GetCollectionCount(CompsToAddField, manager)}");
        sb.AppendLine($"managerCompsToRemove,{GetCollectionCount(CompsToRemoveField, manager)}");
        sb.AppendLine($"managerChains,{CountChains(manager)}");
        sb.AppendLine($"physBoneCount,{physBones.Length}");
        sb.AppendLine();
        sb.AppendLine("index,name,enabled,gameObjectActive,rootTransform,rootField,chainId,bonesCount,hasPhysBone,hasInitParams,hasInitTransform,allowGrabbing,allowPosing,grabMovement,radius,pull,spring,stiffness");

        for (int i = 0; i < physBones.Length; i++)
        {
            VRCPhysBone pb = physBones[i];
            object root = RootField != null ? RootField.GetValue(pb) : null;
            bool hasPhysBone = manager != null && HasPhysBoneMethod != null && (bool)HasPhysBoneMethod.Invoke(manager, new object[] { pb });
            Transform rootTransform = pb.GetRootTransform();

            sb.AppendLine(string.Join(",",
                i,
                Csv(pb.name),
                pb.enabled,
                pb.gameObject.activeInHierarchy,
                Csv(rootTransform != null ? rootTransform.name : ""),
                Csv(root != null ? root.GetType().FullName : ""),
                pb.chainId,
                pb.bones != null ? pb.bones.Count : -1,
                hasPhysBone,
                GetFieldValue(HasInitParamsField, pb),
                GetFieldValue(HasInitTransformField, pb),
                pb.allowGrabbing,
                pb.allowPosing,
                pb.grabMovement,
                pb.radius,
                pb.pull,
                pb.spring,
                pb.stiffness));
        }

        string outputDir = Path.Combine(Application.dataPath, "..", "Logs", "EditorPerf");
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, $"physbone_runtime_state_{suffix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

        Debug.Log($"[PhysBoneRuntimeInspector] Wrote {outputPath}\n{sb}");
    }

    private static object GetManager()
    {
        if (ManagerType == null)
        {
            return null;
        }

        object manager = null;
        if (ManagerInstanceField != null)
        {
            manager = ManagerInstanceField.IsStatic
                ? ManagerInstanceField.GetValue(null)
                : ManagerInstanceField.GetValue(UnityEngine.Object.FindObjectOfType(ManagerType));
        }

        return manager ?? UnityEngine.Object.FindObjectOfType(ManagerType);
    }

    private static object EnsureManager()
    {
        object manager = GetManager();
        if (manager != null || ManagerType == null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("PhysBoneManager");
        manager = managerObject.AddComponent(ManagerType);
        return manager;
    }

    private static Component EnsurePhysBoneRoot(GameObject avatar)
    {
        if (avatar == null || PhysBoneRootType == null)
        {
            return null;
        }

        Component root = avatar.GetComponent(PhysBoneRootType);
        if (root == null)
        {
            root = avatar.AddComponent(PhysBoneRootType);
        }

        return root;
    }

    private static object GetFieldValue(FieldInfo field, object target)
    {
        return field != null && target != null ? field.GetValue(target) : "unknown";
    }

    private static int GetCollectionCount(FieldInfo field, object target)
    {
        if (field == null || target == null)
        {
            return -1;
        }

        object value = field.GetValue(target);
        if (value is ICollection collection)
        {
            return collection.Count;
        }

        return -1;
    }

    private static int CountChains(object manager)
    {
        if (manager == null || GetChainsMethod == null)
        {
            return -1;
        }

        object chainsObject = GetChainsMethod.Invoke(manager, null);
        if (chainsObject is IEnumerator enumerator)
        {
            int enumeratorCount = 0;
            while (enumerator.MoveNext())
            {
                enumeratorCount++;
            }

            return enumeratorCount;
        }

        if (!(chainsObject is IEnumerable chains))
        {
            return -1;
        }

        int count = 0;
        foreach (object _ in chains)
        {
            count++;
        }

        return count;
    }

    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
