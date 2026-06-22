using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>런타임 UI 생성용 공통 헬퍼입니다.</summary>
public static class UIFactoryUtility
{
    public static GameObject CreateUIObject(string name, Transform parent, params Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        foreach (Type componentType in components)
        {
            if (componentType != typeof(RectTransform) && gameObject.GetComponent(componentType) == null)
                gameObject.AddComponent(componentType);
        }

        return gameObject;
    }

    public static Image CreateImage(RectTransform parent, string name, Color color)
    {
        GameObject imageObject = CreateUIObject(name, parent, typeof(Image));
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize)
    {
        GameObject labelObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = UIFontUtility.Sanitize(text);
        UIFontUtility.Apply(label);
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    public static Button CreateButton(RectTransform parent, string name, string label, Color color)
    {
        GameObject buttonObject = CreateUIObject(name, parent, typeof(Image), typeof(Button), typeof(UIClickSound));
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        button.colors = cb;

        TextMeshProUGUI labelText = CreateLabel(buttonObject.GetComponent<RectTransform>(), "Label", label, 16f);
        StretchFull(labelText.rectTransform);
        labelText.alignment = TextAlignmentOptions.Center;

        return button;
    }

    public static Image CreateFilledBar(RectTransform parent, string name, Color backColor, Color fillColor)
    {
        Image back = CreateImage(parent, name, backColor);
        back.raycastTarget = false;

        Image fill = CreateImage(back.rectTransform, "Fill", fillColor);
        StretchFull(fill.rectTransform);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        return fill;
    }

    public static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    public static void StretchHost(RectTransform host)
    {
        if (host == null)
            return;

        host.anchorMin = Vector2.zero;
        host.anchorMax = Vector2.one;
        host.pivot = new Vector2(0.5f, 0.5f);
        host.anchoredPosition = Vector2.zero;
        host.sizeDelta = Vector2.zero;
        host.localScale = Vector3.one;
    }
}
