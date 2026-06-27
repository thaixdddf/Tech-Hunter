using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Tech Hunter/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemName = "Item";
    public Sprite icon;
    public Color displayColor = Color.white;
}
