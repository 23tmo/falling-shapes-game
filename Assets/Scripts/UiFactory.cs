// Centralizes common UI creation and layout helpers so gameplay scripts can build HUD elements in code cleanly.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiFactory
{
    public static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        // A shared scaler setup keeps runtime-built UI consistent across different screen sizes.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    public static void SetRect(RectTransform rectTransform, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rectTransform == null)
        {
            return;
        }

        // This helper wraps the repetitive anchor/pivot boilerplate used when placing HUD widgets.
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    public static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        TMP_Text template,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TextAlignmentOptions alignment,
        bool wordWrap)
    {
        // New text elements are created from code and then styled from a scene template for consistent typography.
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        SetRect(rectTransform, anchor, new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableWordWrapping = wordWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.text = string.Empty;

        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.color = template.color;
            text.fontStyle = template.fontStyle;
            text.extraPadding = template.extraPadding;
        }

        return text;
    }

    public static Image CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        // Panels are plain Images placed behind other HUD elements.
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        panelObject.transform.SetAsFirstSibling();
        return image;
    }
}
