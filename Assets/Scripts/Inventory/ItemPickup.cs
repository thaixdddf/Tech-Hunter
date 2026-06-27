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

        if (!inventory.AddItem(ItemName, ItemColor, definition != null ? definition.icon : null))
            return;

        Destroy(gameObject);
    }

    public static ItemPickup Spawn(Vector3 position, Quaternion rotation, string name, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Pickup_{name}";
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

        ApplyColor(go, color);

        var pickup = go.AddComponent<ItemPickup>();
        pickup.itemName = name;
        pickup.itemColor = color;
        pickup.definition = null;
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
