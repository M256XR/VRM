using UnityEngine;

public class VRMColliderSetup : MonoBehaviour
{
    public static void SetupColliders(GameObject vrmRoot)
    {
        if (vrmRoot == null)
        {
            Debug.LogError("Cannot set up colliders because VRM root is null");
            return;
        }

        Animator animator = vrmRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = vrmRoot.GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator not found on VRM");
            return;
        }

        AddBoneCollider(animator, HumanBodyBones.Head, "Head", 0.12f);
        AddBoneCollider(animator, HumanBodyBones.Neck, "Head", 0.08f);

        AddFirstAvailableBoneCollider(animator, "Body", 0.12f,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine);

        AddBoneCollider(animator, HumanBodyBones.Hips, "Body", 0.12f);
        AddBoneCollider(animator, HumanBodyBones.LeftUpperArm, "Body", 0.08f);
        AddBoneCollider(animator, HumanBodyBones.RightUpperArm, "Body", 0.08f);
        AddBoneCollider(animator, HumanBodyBones.LeftLowerArm, "Hand", 0.07f);
        AddBoneCollider(animator, HumanBodyBones.RightLowerArm, "Hand", 0.07f);
        AddBoneCollider(animator, HumanBodyBones.LeftHand, "Hand", 0.06f);
        AddBoneCollider(animator, HumanBodyBones.RightHand, "Hand", 0.06f);

        Debug.Log("VRM colliders set up");
    }

    private static bool AddFirstAvailableBoneCollider(Animator animator, string bodyPart, float radius, params HumanBodyBones[] bones)
    {
        foreach (HumanBodyBones bone in bones)
        {
            if (AddBoneCollider(animator, bone, bodyPart, radius))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AddBoneCollider(Animator animator, HumanBodyBones bone, string bodyPart, float radius)
    {
        Transform target = animator.GetBoneTransform(bone);
        if (target == null)
        {
            return false;
        }

        SphereCollider existingCollider = target.GetComponent<SphereCollider>();
        if (existingCollider != null)
        {
            Object.Destroy(existingCollider);
        }

        SphereCollider collider = target.gameObject.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = false;

        VRMBodyPart bodyPartComponent = target.GetComponent<VRMBodyPart>();
        if (bodyPartComponent == null)
        {
            bodyPartComponent = target.gameObject.AddComponent<VRMBodyPart>();
        }

        bodyPartComponent.bodyPartName = bodyPart;
        Debug.Log($"Added collider: {target.name} -> {bodyPart} ({radius:F2})");
        return true;
    }
}

public class VRMBodyPart : MonoBehaviour
{
    public string bodyPartName;
}
