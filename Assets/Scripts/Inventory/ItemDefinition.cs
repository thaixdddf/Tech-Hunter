using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Tech Hunter/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemName = "Item";
    public Sprite icon;
    public Color displayColor = Color.white;

    /// <summary>
    /// Optional prefab used to represent this item both as a world pickup
    /// and as the held-item visual. When null, falls back to a coloured cube.
    /// </summary>
    public GameObject pickupPrefab;
}
