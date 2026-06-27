using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] float dotSize = 6f;
    [SerializeField] Color dotColor = new Color(1f, 1f, 1f, 0.9f);

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
    }
}
