using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] float dotSize = 6f;
    [SerializeField] Color dotColor = new Color(1f, 1f, 1f, 0.9f);
    
    Image chargeBarFill;

    void Awake()
    {
        Build();
    }

    void Build()
    {
        var canvasGo = new GameObject("CrosshairCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dotGo = new GameObject("Dot", typeof(RectTransform));
        dotGo.transform.SetParent(canvasGo.transform, false);

        var rect = dotGo.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(dotSize, dotSize);

        var image = dotGo.AddComponent<Image>();
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        image.color = dotColor;
        image.raycastTarget = false;

        // Charge Bar Background (Small Vertical Line)
        var chargeBgGo = new GameObject("ChargeBarBg", typeof(RectTransform));
        chargeBgGo.transform.SetParent(canvasGo.transform, false);
        var chargeBgRect = chargeBgGo.GetComponent<RectTransform>();
        chargeBgRect.anchorMin = chargeBgRect.anchorMax = chargeBgRect.pivot = new Vector2(0.5f, 0.5f);
        chargeBgRect.anchoredPosition = new Vector2(dotSize + 12f, 0f); // Just to the right of the dot
        chargeBgRect.sizeDelta = new Vector2(4f, 20f); // Small line
        var chargeBgImg = chargeBgGo.AddComponent<Image>();
        chargeBgImg.color = new Color(0f, 0f, 0f, 0.5f);
        chargeBgImg.raycastTarget = false;

        // Charge Bar Fill
        var chargeFillGo = new GameObject("ChargeBarFill", typeof(RectTransform));
        chargeFillGo.transform.SetParent(chargeBgGo.transform, false);
        var chargeFillRect = chargeFillGo.GetComponent<RectTransform>();
        chargeFillRect.anchorMin = Vector2.zero;
        chargeFillRect.anchorMax = Vector2.one;
        chargeFillRect.offsetMin = chargeFillRect.offsetMax = Vector2.zero;
        chargeBarFill = chargeFillGo.AddComponent<Image>();
        chargeBarFill.color = new Color(1f, 0.6f, 0f, 0.9f);
        chargeBarFill.type = Image.Type.Filled;
        chargeBarFill.fillMethod = Image.FillMethod.Vertical;
        chargeBarFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        chargeBarFill.fillAmount = 0f;
        chargeBarFill.raycastTarget = false;

        chargeBgGo.SetActive(false); // Hidden by default
    }

    public void SetCharge(float normalizedCharge)
    {
        if (chargeBarFill == null) return;

        bool isCharging = normalizedCharge > 0f;
        chargeBarFill.transform.parent.gameObject.SetActive(isCharging);
        if (isCharging)
        {
            chargeBarFill.fillAmount = normalizedCharge;
            // Interpolate color from yellow to red as it charges
            chargeBarFill.color = Color.Lerp(new Color(1f, 0.8f, 0f), new Color(1f, 0.2f, 0f), normalizedCharge);
        }
    }
}
