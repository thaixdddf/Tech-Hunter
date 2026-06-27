using UnityEngine;

public class HeldItemView : MonoBehaviour
{
    [SerializeField] Camera viewCamera;
    [SerializeField] Vector3 firstPersonLocalPosition = new Vector3(0.35f, -0.28f, 0.55f);
    [SerializeField] Vector3 firstPersonLocalScale = new Vector3(0.22f, 0.22f, 0.22f);
    [SerializeField] Vector3 bodyLocalPosition = new Vector3(0.25f, 0.1f, 0.45f);
    [SerializeField] Vector3 bodyLocalScale = new Vector3(0.3f, 0.3f, 0.3f);

    GameObject firstPersonVisual;
    GameObject bodyVisual;

    void Awake()
    {
        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        CreateVisuals();
        SetVisible(false);
    }

    void CreateVisuals()
    {
        if (viewCamera != null)
        {
            firstPersonVisual = CreateCube("HeldItem_FirstPerson", viewCamera.transform);
            firstPersonVisual.transform.localPosition = firstPersonLocalPosition;
            firstPersonVisual.transform.localScale = firstPersonLocalScale;
        }

        Transform body = FindBodyTransform();
        if (body != null)
        {
            bodyVisual = CreateCube("HeldItem_Body", body);
            bodyVisual.transform.localPosition = bodyLocalPosition;
            bodyVisual.transform.localScale = bodyLocalScale;
        }
    }

    Transform FindBodyTransform()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Camera>() != null)
                continue;

            return child;
        }

        return null;
    }

    static GameObject CreateCube(string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        Destroy(go.GetComponent<Collider>());
        return go;
    }

    public void Show(PlayerInventory.Slot slot)
    {
        ApplyColor(firstPersonVisual, slot.itemColor);
        ApplyColor(bodyVisual, slot.itemColor);
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (firstPersonVisual != null)
            firstPersonVisual.SetActive(visible);

        if (bodyVisual != null)
            bodyVisual.SetActive(visible);
    }

    static void ApplyColor(GameObject go, Color color)
    {
        if (go == null)
            return;

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
