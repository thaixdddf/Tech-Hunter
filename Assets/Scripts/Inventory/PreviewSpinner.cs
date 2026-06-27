using UnityEngine;

/// <summary>
/// Slowly rotates a preview mesh so it looks alive inside the inventory slot.
/// Attached automatically by InventoryHUD to each off-screen preview object.
/// </summary>
public class PreviewSpinner : MonoBehaviour
{
    [SerializeField] float degreesPerSecond = 45f;

    void Update()
    {
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
    }
}
