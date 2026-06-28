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
    CrosshairUI crosshair;
    HeldItemView heldItemView;

    [Header("Throwing")]
    [SerializeField] float minThrowForce = 2f;
    [SerializeField] float maxThrowForce = 15f;
    [SerializeField] float maxChargeTime = 5.0f;
    
    float currentChargeTime = 0f;
    bool isChargingThrow = false;
    Vector3 originalCamLocalPos;

    void Awake()
    {
        EnsureSlots();

        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        if (viewCamera != null)
            originalCamLocalPos = viewCamera.transform.localPosition;

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
        crosshair = FindFirstObjectByType<CrosshairUI>();

        if (hud != null)
            return;

        var uiRoot = new GameObject("GameUI");
        hud = uiRoot.AddComponent<InventoryHUD>();
        crosshair = uiRoot.AddComponent<CrosshairUI>();
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
        if (!IsHolding)
        {
            if (isChargingThrow) ResetCharge();
            return;
        }

        if (GetDropHeld())
        {
            isChargingThrow = true;
            currentChargeTime += Time.deltaTime;
            float normalized = Mathf.Clamp01(currentChargeTime / maxChargeTime);
            if (crosshair != null) crosshair.SetCharge(normalized);

            if (currentChargeTime > 10f && viewCamera != null)
            {
                // Intense shaking for overcharging
                float shakeIntensity = 0.05f + (currentChargeTime - 10f) * 0.01f;
                viewCamera.transform.localPosition = originalCamLocalPos + UnityEngine.Random.insideUnitSphere * shakeIntensity;
            }
        }
        else if (GetDropReleased() && isChargingThrow)
        {
            float normalized = Mathf.Clamp01(currentChargeTime / maxChargeTime);
            float force = Mathf.Lerp(minThrowForce, maxThrowForce, normalized);
            TryThrowHeld(force);
            ResetCharge();
        }
        else if (isChargingThrow && !GetDropHeld()) // failsafe
        {
            ResetCharge();
        }
    }

    void ResetCharge()
    {
        isChargingThrow = false;
        currentChargeTime = 0f;
        if (crosshair != null) crosshair.SetCharge(0f);
        if (viewCamera != null) viewCamera.transform.localPosition = originalCamLocalPos;
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
            pickupPrompt.SetPrompt("Hold Q to throw", true);
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

    public void TryThrowHeld(float force)
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

        ItemPickup pickup = ItemPickup.Spawn(dropPosition, Quaternion.identity, slot.itemName, slot.itemColor, slot.definition, slot.itemMesh, slot.itemMaterials, slot.itemWorldScale);
        
        if (pickup.Body != null)
        {
            // Throw forward and slightly up based on camera look direction
            Vector3 throwDir = (viewCamera != null ? viewCamera.transform.forward : transform.forward);
            pickup.Body.AddForce(throwDir * force, ForceMode.Impulse);
            
            // Add some spin
            pickup.Body.AddTorque(UnityEngine.Random.insideUnitSphere * force * 0.5f, ForceMode.Impulse);
        }

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

    static bool GetDropHeld()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.qKey.isPressed;
#else
        return Input.GetKey(KeyCode.Q);
#endif
    }

    static bool GetDropReleased()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.qKey.wasReleasedThisFrame;
#else
        return Input.GetKeyUp(KeyCode.Q);
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
