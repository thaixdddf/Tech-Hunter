using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] ItemDefinition definition;
    [SerializeField] string itemName = "Item";
    [SerializeField] Color itemColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] bool useGravity;

    Rigidbody body;

    public string ItemName => definition != null ? definition.itemName : itemName;
    public Color ItemColor => definition != null ? definition.displayColor : itemColor;

    void Awake()
    {
        if (useGravity)
            EnablePhysics();
    }

    public void Collect(PlayerInventory inventory)
    {
        if (inventory == null || inventory.IsFull)
            return;

        // Snapshot the visual data from this object before it is destroyed
        // so the held-item view can reproduce the exact same model and size.
        Mesh mesh = null;
        Material[] mats = null;
        var mf = GetComponentInChildren<MeshFilter>();
        var mr = GetComponentInChildren<MeshRenderer>();
        if (mf != null) mesh = mf.sharedMesh;
        if (mr != null) mats = mr.sharedMaterials;

        // Use the renderer's transform scale if available (handles child meshes correctly)
        Vector3 worldScale = mf != null ? mf.transform.lossyScale : transform.lossyScale;

        if (!inventory.AddItem(ItemName, ItemColor,
                definition != null ? definition.icon : null,
                definition, mesh, mats, worldScale))
            return;

        Destroy(gameObject);
    }

    public static ItemPickup Spawn(Vector3 position, Quaternion rotation, string name, Color color,
        ItemDefinition definition = null, Mesh mesh = null, Material[] materials = null, Vector3 worldScale = default)
    {
        GameObject go;

        if (mesh != null)
        {
            // Rebuild the exact model that was picked up
            go = new GameObject($"Pickup_{name}");
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = worldScale == default ? new Vector3(0.45f, 0.45f, 0.45f) : worldScale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            if (materials != null && materials.Length > 0)
                mr.sharedMaterials = materials;

            // Need a collider so the player can pick it up again
            go.AddComponent<BoxCollider>();
        }
        else if (definition != null && definition.pickupPrefab != null)
        {
            go = Instantiate(definition.pickupPrefab, position, rotation);
            go.name = $"Pickup_{name}";
            go.transform.localScale = worldScale == default ? go.transform.localScale : worldScale;

            // Ensure there's a collider for the pickup trigger
            if (go.GetComponent<Collider>() == null)
                go.AddComponent<BoxCollider>();
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Pickup_{name}";
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = worldScale == default ? new Vector3(0.45f, 0.45f, 0.45f) : worldScale;
            ApplyColor(go, color);
        }

        var pickup = go.AddComponent<ItemPickup>();
        pickup.itemName = name;
        pickup.itemColor = color;
        pickup.definition = definition;
        pickup.useGravity = true;
        pickup.EnablePhysics();
        return pickup;
    }

    void EnablePhysics()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    static void ApplyColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (material.shader == null || !material.shader.isSupported)
            material = new Material(Shader.Find("Standard"));

        material.color = color;
        renderer.material = material;
    }
}
