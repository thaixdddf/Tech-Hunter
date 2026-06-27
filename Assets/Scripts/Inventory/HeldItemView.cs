using UnityEngine;

public class HeldItemView : MonoBehaviour
{
    [SerializeField] Camera viewCamera;
    /// <summary>Position of the held item relative to the camera (centered to look like 2-handed hold).</summary>
    [SerializeField] Vector3 heldLocalPosition = new Vector3(0f, -0.25f, 0.55f);
    /// <summary>Maximum size of the held item so it stays comfortably on screen.</summary>
    [SerializeField] float heldMaxSize = 0.22f;

    /// <summary>
    /// Single visual parented to the camera so it stays locked in the player's
    /// view regardless of body movement or rotation.
    /// </summary>
    GameObject heldVisual;

    void Awake()
    {
        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        SetVisible(false);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Build (or rebuild) the held visual from the slot's snapshotted data and show it.</summary>
    public void Show(PlayerInventory.Slot slot)
    {
        DestroyVisual();
        CreateVisual(slot);
        SetVisible(true);
    }

    /// <summary>Toggle visibility without rebuilding the visual.</summary>
    public void SetVisible(bool visible)
    {
        if (heldVisual != null)
            heldVisual.SetActive(visible);
    }

    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------

    void DestroyVisual()
    {
        if (heldVisual == null) return;
        Destroy(heldVisual);
        heldVisual = null;
    }

    void CreateVisual(PlayerInventory.Slot slot)
    {
        if (viewCamera == null) return;

        GameObject prefab = slot.definition != null ? slot.definition.pickupPrefab : null;

        // Derive held scale from the original world scale, preserving proportions ("same format").
        // Normalise so the longest axis equals heldMaxSize.
        Vector3 ws = slot.itemWorldScale == Vector3.zero ? Vector3.one : slot.itemWorldScale;
        float maxAxis = Mathf.Max(ws.x, ws.y, ws.z);
        Vector3 heldScale = (maxAxis > 0f)
            ? ws * (heldMaxSize / maxAxis)
            : Vector3.one * heldMaxSize;

        // Parent to the camera so the item is always visible in the player's hand
        heldVisual = BuildVisual(
            "HeldItem",
            viewCamera.transform,
            heldLocalPosition,
            heldScale,
            slot.itemColor,
            slot.itemMesh,
            slot.itemMaterials,
            prefab);
    }

    /// <summary>
    /// Builds a held-item GameObject using the following priority:
    /// 1. Snapshotted mesh + materials (exact copy of the world model)
    /// 2. Definition's pickupPrefab
    /// 3. Coloured cube fallback
    /// </summary>
    static GameObject BuildVisual(
        string goName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color fallbackColor,
        Mesh snapshotMesh,
        Material[] snapshotMaterials,
        GameObject prefab)
    {
        GameObject go;

        if (snapshotMesh != null)
        {
            // Exact copy of the model that was sitting on the ground
            go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.identity;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = snapshotMesh;

            var mr = go.AddComponent<MeshRenderer>();
            if (snapshotMaterials != null && snapshotMaterials.Length > 0)
                mr.sharedMaterials = snapshotMaterials;
        }
        else if (prefab != null)
        {
            go = Instantiate(prefab, parent);
            go.name = goName;
            go.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Last resort: coloured cube
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = goName;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            ApplyColor(go, fallbackColor);
        }

        // Strip physics — this is a purely visual object
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        go.transform.localPosition = localPosition;
        go.transform.localScale    = localScale;
        return go;
    }

    static void ApplyColor(GameObject go, Color color)
    {
        if (go == null) return;

        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || !mat.shader.isSupported)
            mat = new Material(Shader.Find("Standard"));

        mat.color = color;
        renderer.material = mat;
    }
}
