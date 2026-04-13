using System;
using System.Collections.Generic;
using UnityEngine;
using VRM;

public static class SpringBoneSetup
{
    public const string ManifestAssetName = "wallpaper_springbone_manifest";
    private const string LOG_FILE = "springbone_setup.log";

    [Serializable]
    private class SpringBoneManifestData
    {
        public SpringBoneGroupData[] springBoneGroups;
        public ColliderGroupData[] colliderGroups;
    }

    [Serializable]
    private class SpringBoneGroupData
    {
        public string comment;
        public float stiffnessForce;
        public float gravityPower;
        public float dragForce;
        public float hitRadius;
        public string[] rootBonePaths;
        public int[] colliderGroupIndices;
    }

    [Serializable]
    private class ColliderGroupData
    {
        public string bonePath;
        public SphereColliderData[] colliders;
    }

    [Serializable]
    private class SphereColliderData
    {
        public float x, y, z;
        public float radius;
    }

    public static void Apply(GameObject avatarRoot, TextAsset manifestAsset)
    {
        DebugLogger.InitLog(LOG_FILE);

        if (avatarRoot == null || manifestAsset == null)
        {
            DebugLogger.LogWarning(LOG_FILE, "SpringBoneSetup: avatarRoot or manifest is null");
            return;
        }

        DebugLogger.LogSeparator(LOG_FILE, "SpringBoneSetup Apply");

        SpringBoneManifestData manifest;
        try
        {
            manifest = JsonUtility.FromJson<SpringBoneManifestData>(manifestAsset.text);
        }
        catch (Exception e)
        {
            DebugLogger.LogError(LOG_FILE, $"Failed to parse spring bone manifest: {e.Message}");
            return;
        }

        if (manifest == null)
        {
            DebugLogger.LogError(LOG_FILE, "Spring bone manifest is null after parsing");
            return;
        }

        Transform root = avatarRoot.transform;

        // コライダーグループを先に作成
        VRMSpringBoneColliderGroup[] colliderGroups = CreateColliderGroups(root, manifest.colliderGroups);

        // SpringBone を作成
        int createdCount = 0;
        if (manifest.springBoneGroups != null)
        {
            foreach (SpringBoneGroupData group in manifest.springBoneGroups)
            {
                if (CreateSpringBone(root, group, colliderGroups))
                {
                    createdCount++;
                }
            }
        }

        DebugLogger.Log(LOG_FILE, $"SpringBoneSetup complete: {createdCount} spring bones, {colliderGroups.Length} collider groups");
    }

    private static VRMSpringBoneColliderGroup[] CreateColliderGroups(Transform root, ColliderGroupData[] groupsData)
    {
        if (groupsData == null || groupsData.Length == 0)
        {
            return Array.Empty<VRMSpringBoneColliderGroup>();
        }

        List<VRMSpringBoneColliderGroup> groups = new List<VRMSpringBoneColliderGroup>();

        foreach (ColliderGroupData data in groupsData)
        {
            Transform bone = string.IsNullOrWhiteSpace(data.bonePath) ? root : root.Find(data.bonePath);
            if (bone == null)
            {
                DebugLogger.LogWarning(LOG_FILE, $"Collider group bone not found: '{data.bonePath}'");
                groups.Add(null);
                continue;
            }

            VRMSpringBoneColliderGroup group = bone.gameObject.AddComponent<VRMSpringBoneColliderGroup>();

            if (data.colliders != null && data.colliders.Length > 0)
            {
                group.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[data.colliders.Length];
                for (int i = 0; i < data.colliders.Length; i++)
                {
                    SphereColliderData col = data.colliders[i];
                    group.Colliders[i] = new VRMSpringBoneColliderGroup.SphereCollider
                    {
                        Offset = new Vector3(col.x, col.y, col.z),
                        Radius = col.radius
                    };
                }
            }

            groups.Add(group);
            DebugLogger.Log(LOG_FILE, $"  ColliderGroup on '{data.bonePath}' ({data.colliders?.Length ?? 0} spheres)");
        }

        return groups.ToArray();
    }

    private static bool CreateSpringBone(Transform root, SpringBoneGroupData data, VRMSpringBoneColliderGroup[] colliderGroups)
    {
        if (data.rootBonePaths == null || data.rootBonePaths.Length == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, $"Spring bone '{data.comment}' has no root bone paths");
            return false;
        }

        List<Transform> rootBones = new List<Transform>();
        foreach (string path in data.rootBonePaths)
        {
            Transform bone = string.IsNullOrWhiteSpace(path) ? root : root.Find(path);
            if (bone != null)
            {
                rootBones.Add(bone);
            }
            else
            {
                DebugLogger.LogWarning(LOG_FILE, $"Spring bone root not found: '{path}'");
            }
        }

        if (rootBones.Count == 0)
        {
            DebugLogger.LogWarning(LOG_FILE, $"Spring bone '{data.comment}' - no valid root bones resolved");
            return false;
        }

        // SpringBone コンポーネントをルートに追加
        VRMSpringBone springBone = root.gameObject.AddComponent<VRMSpringBone>();
        springBone.m_comment = data.comment;
        springBone.m_stiffnessForce = data.stiffnessForce;
        springBone.m_gravityPower = data.gravityPower;
        springBone.m_gravityDir = new Vector3(0, -1, 0);
        springBone.m_dragForce = data.dragForce;
        springBone.m_hitRadius = data.hitRadius;
        springBone.RootBones = rootBones;

        // コライダーグループを割り当て
        if (data.colliderGroupIndices != null && data.colliderGroupIndices.Length > 0)
        {
            List<VRMSpringBoneColliderGroup> assignedGroups = new List<VRMSpringBoneColliderGroup>();
            foreach (int idx in data.colliderGroupIndices)
            {
                if (idx >= 0 && idx < colliderGroups.Length && colliderGroups[idx] != null)
                {
                    assignedGroups.Add(colliderGroups[idx]);
                }
            }
            springBone.ColliderGroups = assignedGroups.ToArray();
        }

        DebugLogger.Log(LOG_FILE, $"  SpringBone '{data.comment}' stiff={data.stiffnessForce:F2} drag={data.dragForce:F2} grav={data.gravityPower:F2} radius={data.hitRadius:F3} roots={rootBones.Count}");
        return true;
    }
}
