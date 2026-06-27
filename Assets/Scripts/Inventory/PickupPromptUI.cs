using UnityEngine;
using UnityEngine.UI;

public class PickupPromptUI : MonoBehaviour
{
    [SerializeField] string promptText = "E to pickup";
    [SerializeField] float offsetY = -36f;

    Text label;

    void Awake()
    {
        Build();
        SetVisible(false);
    }

    void Build()
    {
        var canvasGo = new GameObject("PickupPromptCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var textGo = new GameObject("Prompt", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);

        var rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, offsetY);
        rect.sizeDelta = new Vector2(400f, 30f);

        label = textGo.AddComponent<Text>();
        label.text = promptText;
        label.fontSize = 18;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.color = new Color(1f, 1f, 1f, 0.95f);
        label.raycastTarget = false;
    }

    public void SetPrompt(string text, bool visible)
    {
        if (label == null)
            return;

        label.text = text;
        label.enabled = visible;
    }

    public void SetVisible(bool visible)
    {
        SetPrompt(promptText, visible);
    }
}
