using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class PlayerInventory : MonoBehaviour
{
    public const int MaxSlots = 4;

    [Serializable]
    public class Slot
    {
        public string itemName = "";
        public Color itemColor = Color.gray;
        public Sprite icon;
        /// <summary>Reference to the original ItemDefinition, used to drive the held-item visual model.</summary>
        public ItemDefinition definition;
        /// <summary>Mesh snapshotted from the world pickup so the held visual matches exactly.</summary>
        public Mesh itemMesh;
        /// <summary>Materials snapshotted from the world pickup so the held visual matches exactly.</summary>
        public Material[] itemMaterials;
        /// <summary>World-space lossy scale of the pickup object, used to preserve relative size when held.</summary>
        public Vector3 itemWorldScale;

        public bool IsEmpty => string.IsNullOrEmpty(itemName);

        public void Clear()
        {
            itemName = "";
            icon = null;
            itemColor = Color.gray;
            definition = null;
            itemMesh = null;
            itemMaterials = null;
            itemWorldScale = Vector3.one;
        }
    }

    [SerializeField] Camera viewCamera;
    [SerializeField] float pickupRange = 3f;
    [SerializeField] float dropDistance = 1.2f;
    [SerializeField] LayerMask pickupMask = ~0;
    [SerializeField] Slot[] slots = new Slot[MaxSlots];

    public event Action OnChanged;

    public int EquippedSlotIndex => equippedSlotIndex;
    public bool IsHolding => equippedSlotIndex >= 0;

    int equippedSlotIndex = -1;

    InventoryHUD hud;
    PickupPromptUI pickupPrompt;
    HeldItemView heldItemView;

    void Awake()
    {
        EnsureSlots();

        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        heldItemView = GetComponent<HeldItemView>();
        if (heldItemView == null)
            heldItemView = gameObject.AddComponent<HeldItemView>();
    }

    void Start()
    {
        EnsureUI();
        hud.Bind(this);
    }

    void EnsureUI()
    {
        hud = FindFirstObjectByType<InventoryHUD>();
        pickupPrompt = FindFirstObjectByType<PickupPromptUI>();

        if (hud != null)
            return;

        var uiRoot = new GameObject("GameUI");
        hud = uiRoot.AddComponent<InventoryHUD>();
        uiRoot.AddComponent<CrosshairUI>();
        pickupPrompt = uiRoot.AddComponent<PickupPromptUI>();
    }

    void Update()
    {
        HandleHoldInput();
        HandleDropInput();
        UpdatePrompt();
        HandlePickupInput();
    }

    void HandleHoldInput()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (!GetNumberKeyPressed(i))
                continue;

            TryEquipSlot(i);
            break;
        }
    }

    void HandleDropInput()
    {
        if (GetDropPressed())
            TryDropHeld();
    }

    void HandlePickupInput()
    {
        if (!GetInteractPressed())
            return;

        ItemPickup pickup = GetTargetedPickup();
        pickup?.Collect(this);
    }

    void UpdatePrompt()
    {
        if (pickupPrompt == null)
            return;

        if (IsHolding)
        {
            pickupPrompt.SetPrompt("Q to drop", true);
            return;
        }

        ItemPickup pickup = GetTargetedPickup();
        pickupPrompt.SetPrompt("E to pickup", pickup != null);
    }

    void EnsureSlots()
    {
        if (slots != null && slots.Length == MaxSlots)
            return;

        slots = new Slot[MaxSlots];
        for (int i = 0; i < MaxSlots; i++)
            slots[i] = new Slot();
    }

    public Slot GetSlot(int index)
    {
        EnsureSlots();

        if (index < 0 || index >= MaxSlots)
            return null;

        return slots[index];
    }

    public bool IsFull
    {
        get
        {
            EnsureSlots();

            for (int i = 0; i < MaxSlots; i++)
            {
                if (slots[i].IsEmpty)
                    return false;
            }

            return true;
        }
    }

    public bool AddItem(string name, Color color, Sprite icon = null, ItemDefinition definition = null,
        Mesh mesh = null, Material[] materials = null, Vector3 worldScale = default)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        EnsureSlots();

        for (int i = 0; i < MaxSlots; i++)
        {
            if (!slots[i].IsEmpty)
                continue;

            slots[i].itemName = name;
            slots[i].itemColor = color;
            slots[i].icon = icon;
            slots[i].definition = definition;
            slots[i].itemMesh = mesh;
            slots[i].itemMaterials = materials;
            slots[i].itemWorldScale = worldScale == default ? Vector3.one : worldScale;
            OnChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void TryEquipSlot(int index)
    {
        EnsureSlots();

        if (index < 0 || index >= MaxSlots)
            return;

        if (slots[index].IsEmpty)
        {
            Unequip();
            return;
        }

        equippedSlotIndex = index;
        heldItemView.Show(slots[index]);
        OnChanged?.Invoke();
    }

    public void TryDropHeld()
    {
        if (!IsHolding)
            return;

        Slot slot = slots[equippedSlotIndex];
        if (slot.IsEmpty)
        {
            Unequip();
            return;
        }

        Vector3 dropPosition = transform.position + transform.forward * dropDistance;
        dropPosition.y = transform.position.y + 0.45f;

        ItemPickup.Spawn(dropPosition, Quaternion.identity, slot.itemName, slot.itemColor, slot.definition, slot.itemMesh, slot.itemMaterials, slot.itemWorldScale);
        slot.Clear();
        Unequip();
        OnChanged?.Invoke();
    }

    void Unequip()
    {
        equippedSlotIndex = -1;
        heldItemView.SetVisible(false);
        OnChanged?.Invoke();
    }

    ItemPickup GetTargetedPickup()
    {
        if (viewCamera == null)
            return null;

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<ItemPickup>();
    }

    static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    static bool GetDropPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    static bool GetNumberKeyPressed(int index)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current == null)
            return false;

        return index switch
        {
            0 => Keyboard.current.digit1Key.wasPressedThisFrame,
            1 => Keyboard.current.digit2Key.wasPressedThisFrame,
            2 => Keyboard.current.digit3Key.wasPressedThisFrame,
            3 => Keyboard.current.digit4Key.wasPressedThisFrame,
            _ => false
        };
#else
        return Input.GetKeyDown(KeyCode.Alpha1 + index);
#endif
    }
}
