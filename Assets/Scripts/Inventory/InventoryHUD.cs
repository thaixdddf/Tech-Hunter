using UnityEngine;
using UnityEngine.UI;

public class InventoryHUD : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Layout constants
    // ---------------------------------------------------------------
    const float SlotSize    = 64f;
    const float SlotGap     = 8f;
    const float Padding     = 12f;

    // Size of each RenderTexture used for 3-D slot previews
    const int   RT_Size     = 128;

    // How far the preview camera sits from the origin of the preview scene
    const float CamDist     = 1.2f;

    // ---------------------------------------------------------------
    // Per-slot state
    // ---------------------------------------------------------------
    struct SlotView
    {
        public Image       border;
        public RawImage    preview;    // shows the RenderTexture
        public Image       colorFill;  // fallback flat colour (hidden when preview active)
        public Text        label;
        public Camera      cam;
        public GameObject  previewGo;  // mesh clone rendered by cam
        public RenderTexture rt;
    }

    PlayerInventory inventory;
    SlotView[] views;

    // Shared off-screen root – everything lives far from the play area
    static readonly Vector3 PreviewOrigin = new Vector3(0f, -1000f, 0f);

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

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

        if (views == null) return;
        foreach (var v in views)
            CleanupSlot(v);
    }

    // ---------------------------------------------------------------
    // Build the canvas + all slot UIs
    // ---------------------------------------------------------------
    void Build()
    {
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Outer panel
        var panel = MakeRect("Panel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
        panel.anchoredPosition = new Vector2(-24f, 24f);

        float width = Padding * 2f + PlayerInventory.MaxSlots * SlotSize
                    + (PlayerInventory.MaxSlots - 1) * SlotGap;
        panel.sizeDelta = new Vector2(width, Padding * 2f + SlotSize + 28f);
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

        // "Inventory" title
        var title = MakeText("Title", panel, "Inventory", 16, FontStyle.Bold);
        title.rectTransform.anchorMin       = new Vector2(0f, 1f);
        title.rectTransform.anchorMax       = new Vector2(1f, 1f);
        title.rectTransform.pivot           = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition= new Vector2(0f, -Padding);
        title.rectTransform.sizeDelta       = new Vector2(-Padding * 2f, 22f);
        title.alignment = TextAnchor.UpperCenter;
        title.color     = new Color(0.92f, 0.92f, 0.95f);

        // Slot row
        var row = MakeRect("Slots", panel);
        row.anchorMin  = Vector2.zero;
        row.anchorMax  = Vector2.one;
        row.offsetMin  = new Vector2(Padding,  Padding);
        row.offsetMax  = new Vector2(-Padding, -Padding - 22f);

        views = new SlotView[PlayerInventory.MaxSlots];

        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
        {
            var slotRect = MakeRect($"Slot{i + 1}", row);
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(0f, 0.5f);
            slotRect.pivot     = new Vector2(0f, 0.5f);
            slotRect.anchoredPosition = new Vector2(i * (SlotSize + SlotGap), 0f);
            slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

            // Slot background / selection border
            var border = slotRect.gameObject.AddComponent<Image>();
            border.color = new Color(0.22f, 0.24f, 0.3f);

            // Flat-colour fallback (shown when no mesh preview is available)
            var colorRect = MakeRect("ColorFill", slotRect);
            colorRect.anchorMin = new Vector2(0.1f, 0.1f);
            colorRect.anchorMax = new Vector2(0.9f, 0.9f);
            colorRect.offsetMin = colorRect.offsetMax = Vector2.zero;
            var colorImg = colorRect.gameObject.AddComponent<Image>();
            colorImg.color = new Color(0.12f, 0.12f, 0.14f);

            // RawImage that displays the RenderTexture preview
            var rawRect = MakeRect("Preview3D", slotRect);
            rawRect.anchorMin = new Vector2(0.05f, 0.05f);
            rawRect.anchorMax = new Vector2(0.95f, 0.95f);
            rawRect.offsetMin = rawRect.offsetMax = Vector2.zero;
            var rawImg = rawRect.gameObject.AddComponent<RawImage>();
            rawImg.color = Color.white;

            // Slot number label
            var label = MakeText("Label", slotRect, (i + 1).ToString(), 12, FontStyle.Normal);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(1f, 0.3f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.LowerCenter;
            label.color     = new Color(0.75f, 0.75f, 0.8f, 0.9f);

            views[i] = new SlotView
            {
                border    = border,
                preview   = rawImg,
                colorFill = colorImg,
                label     = label,
            };
        }
    }

    // ---------------------------------------------------------------
    // Refresh every slot to match inventory state
    // ---------------------------------------------------------------
    void Refresh()
    {
        if (inventory == null || views == null) return;

        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
        {
            ref SlotView v = ref views[i];
            PlayerInventory.Slot slot = inventory.GetSlot(i);
            bool isEquipped = inventory.EquippedSlotIndex == i;

            // Selection highlight
            v.border.color = isEquipped
                ? new Color(0.95f, 0.85f, 0.2f)
                : new Color(0.22f, 0.24f, 0.3f);

            // Empty slot
            if (slot == null || slot.IsEmpty)
            {
                CleanupSlot(v);
                v.cam      = null;
                v.previewGo = null;
                v.rt       = null;
                v.preview.texture = null;
                v.preview.gameObject.SetActive(false);
                v.colorFill.color  = new Color(0.12f, 0.12f, 0.14f);
                v.colorFill.gameObject.SetActive(true);
                v.label.text = (i + 1).ToString();
                continue;
            }

            v.label.text = slot.itemName;

            // If the slot has a mesh, build / rebuild a 3-D preview
            if (slot.itemMesh != null)
            {
                RebuildPreview(ref v, slot, i);
                v.colorFill.gameObject.SetActive(false);
                v.preview.gameObject.SetActive(true);
            }
            else
            {
                // No mesh — show flat colour (sprite icon or item colour)
                CleanupSlot(v);
                v.cam       = null;
                v.previewGo = null;
                v.rt        = null;
                v.preview.texture = null;
                v.preview.gameObject.SetActive(false);
                v.colorFill.sprite = slot.icon;
                v.colorFill.color  = slot.icon != null ? Color.white : slot.itemColor;
                v.colorFill.gameObject.SetActive(true);
            }
        }
    }

    // ---------------------------------------------------------------
    // Per-slot 3-D preview helpers
    // ---------------------------------------------------------------

    /// <summary>
    /// Creates (or recreates) an off-screen camera + mesh clone for a slot,
    /// then assigns the resulting RenderTexture to the slot's RawImage.
    /// </summary>
    void RebuildPreview(ref SlotView v, PlayerInventory.Slot slot, int slotIndex)
    {
        CleanupSlot(v);

        // --- RenderTexture ---
        var rt = new RenderTexture(RT_Size, RT_Size, 16, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        rt.Create();
        v.rt = rt;
        v.preview.texture = rt;

        // --- Off-screen preview scene (far below the real world) ---
        // Each slot gets a unique X offset so cameras don't interfere
        Vector3 origin = PreviewOrigin + Vector3.right * slotIndex * 10f;

        // Clone of the item mesh
        var meshGo = new GameObject("SlotPreview_Mesh");
        meshGo.transform.position = origin;

        var mf = meshGo.AddComponent<MeshFilter>();
        mf.sharedMesh = slot.itemMesh;

        var mr = meshGo.AddComponent<MeshRenderer>();
        if (slot.itemMaterials != null && slot.itemMaterials.Length > 0)
            mr.sharedMaterials = slot.itemMaterials;

        // Scale so the preview fits nicely regardless of original size
        Bounds b = slot.itemMesh.bounds;
        float longest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        meshGo.transform.localScale = longest > 0f
            ? Vector3.one * (0.6f / longest)
            : Vector3.one;

        // Slow spin so the item looks alive
        var spinner = meshGo.AddComponent<PreviewSpinner>();
        spinner.enabled = true;

        v.previewGo = meshGo;

        // --- Dedicated preview camera ---
        var camGo = new GameObject("SlotPreview_Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.12f, 0.12f, 0.14f, 0f);   // transparent-ish
        cam.cullingMask      = ~0;           // see everything
        cam.orthographic     = false;
        cam.fieldOfView      = 35f;
        cam.nearClipPlane    = 0.1f;
        cam.farClipPlane     = 20f;
        cam.targetTexture    = rt;
        cam.enabled          = true;

        // Position the camera to look at the mesh from a nice angle
        camGo.transform.position = origin + new Vector3(0.3f, 0.4f, -CamDist);
        camGo.transform.LookAt(origin);

        // Prevent this camera from rendering the main scene
        cam.cullingMask = LayerMask.GetMask("Default") == 0
            ? ~0   // no named layers, allow all
            : ~0;  // keep all — the offset ensures no overlap with real geometry

        v.cam = cam;

        // Force an immediate render so the texture isn't blank on first frame
        cam.Render();
    }

    /// <summary>Destroy all off-screen objects owned by a slot view.</summary>
    static void CleanupSlot(SlotView v)
    {
        if (v.previewGo != null) Destroy(v.previewGo);
        if (v.cam       != null) Destroy(v.cam.gameObject);
        if (v.rt        != null) { v.rt.Release(); Destroy(v.rt); }
    }

    // ---------------------------------------------------------------
    // UI helpers
    // ---------------------------------------------------------------
    static RectTransform MakeRect(string name, Transform parent)
    {
        var go   = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Text MakeText(string name, Transform parent, string value, int size, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text      = value;
        text.fontSize  = size;
        text.fontStyle = style;
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return text;
    }
}
