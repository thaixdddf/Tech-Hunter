using UnityEngine;
using UnityEngine.UI;

public class InventoryHUD : MonoBehaviour
{
    const float SlotSize = 64f;
    const float SlotGap = 8f;
    const float Padding = 12f;

    PlayerInventory inventory;
    Image[] icons;
    Image[] slotBorders;
    Text[] labels;

    public void Bind(PlayerInventory target)
    {
        if (inventory != null)
            inventory.OnChanged -= Refresh;

        inventory = target;
        Build();
        inventory.OnChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnChanged -= Refresh;
    }

    void Build()
    {
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = MakeRect("Panel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
        panel.anchoredPosition = new Vector2(-24f, 24f);

        float width = Padding * 2f + PlayerInventory.MaxSlots * SlotSize + (PlayerInventory.MaxSlots - 1) * SlotGap;
        panel.sizeDelta = new Vector2(width, Padding * 2f + SlotSize + 28f);

        var panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

        var title = MakeText("Title", panel, "Inventory", 16, FontStyle.Bold);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -Padding);
        title.rectTransform.sizeDelta = new Vector2(-Padding * 2f, 22f);
        title.alignment = TextAnchor.UpperCenter;
        title.color = new Color(0.92f, 0.92f, 0.95f);

        var row = MakeRect("Slots", panel);
        row.anchorMin = Vector2.zero;
        row.anchorMax = Vector2.one;
        row.offsetMin = new Vector2(Padding, Padding);
        row.offsetMax = new Vector2(-Padding, -Padding - 22f);

        icons = new Image[PlayerInventory.MaxSlots];
        labels = new Text[PlayerInventory.MaxSlots];
        slotBorders = new Image[PlayerInventory.MaxSlots];

        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
        {
            var slot = MakeRect($"Slot{i + 1}", row);
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 0.5f);
            slot.pivot = new Vector2(0f, 0.5f);
            slot.anchoredPosition = new Vector2(i * (SlotSize + SlotGap), 0f);
            slot.sizeDelta = new Vector2(SlotSize, SlotSize);

            slot.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.24f, 0.3f);
            slotBorders[i] = slot.GetComponent<Image>();

            var iconRect = MakeRect("Icon", slot);
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            icons[i] = iconRect.gameObject.AddComponent<Image>();
            icons[i].color = new Color(0.12f, 0.12f, 0.14f);

            var label = MakeText("Label", slot, (i + 1).ToString(), 12, FontStyle.Normal);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(1f, 0.3f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.LowerCenter;
            label.color = new Color(0.75f, 0.75f, 0.8f, 0.9f);
            labels[i] = label;
        }
    }

    void Refresh()
    {
        if (inventory == null || icons == null)
            return;

        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
        {
            PlayerInventory.Slot slot = inventory.GetSlot(i);
            bool isEquipped = inventory.EquippedSlotIndex == i;

            if (slotBorders[i] != null)
            {
                slotBorders[i].color = isEquipped
                    ? new Color(0.95f, 0.85f, 0.2f)
                    : new Color(0.22f, 0.24f, 0.3f);
            }

            if (slot == null || slot.IsEmpty)
            {
                icons[i].sprite = null;
                icons[i].color = new Color(0.12f, 0.12f, 0.14f);
                labels[i].text = (i + 1).ToString();
                continue;
            }

            icons[i].sprite = slot.icon;
            icons[i].color = slot.icon != null ? Color.white : slot.itemColor;
            labels[i].text = slot.itemName;
        }
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Text MakeText(string name, Transform parent, string value, int size, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return text;
    }
}
