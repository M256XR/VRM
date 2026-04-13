using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ColliderVisualizer : MonoBehaviour
{
    [SerializeField] private bool showColliders;
    [SerializeField] private Color headColor = new Color(0.1f, 1.0f, 0.4f, 0.22f);
    [SerializeField] private Color bodyColor = new Color(0.1f, 0.5f, 1.0f, 0.18f);
    [SerializeField] private Color handColor = new Color(1.0f, 0.75f, 0.2f, 0.24f);
    [SerializeField] private Color fallbackColor = new Color(1.0f, 1.0f, 1.0f, 0.12f);
    [SerializeField] private Color highlightColor = new Color(1.0f, 0.25f, 0.25f, 0.8f);
    [SerializeField] private float highlightSeconds = 0.6f;

    private readonly Dictionary<SphereCollider, DebugSphere> debugSpheres = new Dictionary<SphereCollider, DebugSphere>();

    private Transform debugRoot;
    private SphereCollider highlightedCollider;
    private float highlightUntil;

    private struct DebugSphere
    {
        public GameObject gameObject;
        public Renderer renderer;
        public Color baseColor;
    }

    private void Awake()
    {
        EnsureDebugRoot();
        Rebuild();
        ApplyVisibility();
    }

    private void LateUpdate()
    {
        SyncDebugSpheres();

        if (highlightedCollider != null && Time.unscaledTime >= highlightUntil)
        {
            ClearHighlight();
        }
    }

    public void Rebuild()
    {
        EnsureDebugRoot();
        ClearDebugSpheres();

        SphereCollider[] colliders = GetComponentsInChildren<SphereCollider>(true);
        foreach (SphereCollider source in colliders)
        {
            if (source == null)
            {
                continue;
            }

            GameObject sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObject.name = $"{source.gameObject.name}_DebugSphere";
            sphereObject.transform.SetParent(debugRoot, false);

            Collider createdCollider = sphereObject.GetComponent<Collider>();
            if (createdCollider != null)
            {
                Destroy(createdCollider);
            }

            Renderer renderer = sphereObject.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateDebugMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            Color baseColor = ResolveColor(source);
            renderer.sharedMaterial.color = baseColor;

            debugSpheres[source] = new DebugSphere
            {
                gameObject = sphereObject,
                renderer = renderer,
                baseColor = baseColor
            };
        }

        SyncDebugSpheres();
        ApplyVisibility();
    }

    public void SetVisible(bool visible)
    {
        showColliders = visible;
        ApplyVisibility();
    }

    public void Highlight(Collider collider)
    {
        ClearHighlight();

        if (!(collider is SphereCollider sphereCollider))
        {
            return;
        }

        if (!debugSpheres.TryGetValue(sphereCollider, out DebugSphere debugSphere) || debugSphere.renderer == null)
        {
            return;
        }

        debugSphere.renderer.sharedMaterial.color = highlightColor;
        highlightedCollider = sphereCollider;
        highlightUntil = Time.unscaledTime + Mathf.Max(0.05f, highlightSeconds);
    }

    private void ApplyVisibility()
    {
        if (debugRoot != null)
        {
            debugRoot.gameObject.SetActive(showColliders);
        }
    }

    private void SyncDebugSpheres()
    {
        foreach (KeyValuePair<SphereCollider, DebugSphere> pair in debugSpheres)
        {
            SphereCollider source = pair.Key;
            DebugSphere debugSphere = pair.Value;
            if (source == null || debugSphere.gameObject == null)
            {
                continue;
            }

            debugSphere.gameObject.SetActive(showColliders && source.enabled && source.gameObject.activeInHierarchy);
            debugSphere.gameObject.transform.position = source.transform.TransformPoint(source.center);

            Vector3 lossyScale = source.transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
            float diameter = source.radius * 2f * maxScale;
            debugSphere.gameObject.transform.localScale = Vector3.one * diameter;
        }
    }

    private void ClearHighlight()
    {
        if (highlightedCollider != null &&
            debugSpheres.TryGetValue(highlightedCollider, out DebugSphere debugSphere) &&
            debugSphere.renderer != null)
        {
            debugSphere.renderer.sharedMaterial.color = debugSphere.baseColor;
        }

        highlightedCollider = null;
        highlightUntil = 0f;
    }

    private void EnsureDebugRoot()
    {
        if (debugRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("__ColliderDebug");
        if (existing != null)
        {
            debugRoot = existing;
            return;
        }

        GameObject rootObject = new GameObject("__ColliderDebug");
        rootObject.transform.SetParent(transform, false);
        debugRoot = rootObject.transform;
    }

    private void ClearDebugSpheres()
    {
        ClearHighlight();

        foreach (KeyValuePair<SphereCollider, DebugSphere> pair in debugSpheres)
        {
            DebugSphere debugSphere = pair.Value;
            if (debugSphere.renderer != null && debugSphere.renderer.sharedMaterial != null)
            {
                Destroy(debugSphere.renderer.sharedMaterial);
            }

            if (debugSphere.gameObject != null)
            {
                Destroy(debugSphere.gameObject);
            }
        }

        debugSpheres.Clear();
    }

    private Color ResolveColor(SphereCollider collider)
    {
        VRMBodyPart bodyPart = collider.GetComponent<VRMBodyPart>();
        if (bodyPart == null)
        {
            bodyPart = collider.GetComponentInParent<VRMBodyPart>();
        }

        if (bodyPart == null || string.IsNullOrEmpty(bodyPart.bodyPartName))
        {
            return fallbackColor;
        }

        switch (bodyPart.bodyPartName)
        {
            case "Head":
                return headColor;
            case "Body":
                return bodyColor;
            case "Hand":
                return handColor;
            default:
                return fallbackColor;
        }
    }

    private Material CreateDebugMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = fallbackColor;

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        return material;
    }
}
