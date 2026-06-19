using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 발주 UI 패널을 런타임에 구성합니다. 씬에 SupplyPanel이 없을 때 사용합니다.
/// </summary>
public static class SupplyUIPanelFactory
{
    const string FontResourcePath = "Fonts/Pretendard-Medium SDF";
    const string PanelName = "SupplyPanel";
    const string ListContentName = "OrderListContent";

    public struct SupplyPanelRefs
    {
        public RectTransform panelRoot;
        public RectTransform listContentRoot;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI totalCostText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI criticalWarningText;
        public Button confirmButton;
    }

    public static SupplyPanelRefs EnsurePanel(Transform canvasRoot)
    {
        Transform existing = canvasRoot.Find(PanelName);
        if (existing != null)
            return BindExisting(existing);

        return CreatePanel(canvasRoot);
    }

    static SupplyPanelRefs BindExisting(Transform panelRoot)
    {
        SupplyPanelRefs refs = new SupplyPanelRefs
        {
            panelRoot = panelRoot as RectTransform,
            listContentRoot = panelRoot.Find($"ScrollViewport/{ListContentName}") as RectTransform,
            coinText = panelRoot.Find("CoinText")?.GetComponent<TextMeshProUGUI>(),
            totalCostText = panelRoot.Find("TotalCostText")?.GetComponent<TextMeshProUGUI>(),
            statusText = panelRoot.Find("StatusText")?.GetComponent<TextMeshProUGUI>(),
            criticalWarningText = panelRoot.Find("CriticalWarningText")?.GetComponent<TextMeshProUGUI>(),
            confirmButton = panelRoot.Find("ConfirmButton")?.GetComponent<Button>()
        };

        return refs;
    }

    static SupplyPanelRefs CreatePanel(Transform canvasRoot)
    {
        GameObject panelObject = CreateUIObject(PanelName, canvasRoot, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(24f, 0f);
        panelRect.sizeDelta = new Vector2(300f, 520f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);

        SupplyPanelRefs refs = new SupplyPanelRefs
        {
            panelRoot = panelRect,
            coinText = CreateLabel(panelRect, "CoinText", "보유 코인: 0", new Vector2(16f, -16f), new Vector2(268f, 28f), 18),
            totalCostText = CreateLabel(panelRect, "TotalCostText", "발주 합계: 0 Coin", new Vector2(16f, -48f), new Vector2(268f, 24f), 16),
            criticalWarningText = CreateLabel(
                panelRect,
                "CriticalWarningText",
                string.Empty,
                new Vector2(16f, -76f),
                new Vector2(268f, 40f),
                14),
            listContentRoot = CreateScrollList(panelRect),
            statusText = CreateLabel(panelRect, "StatusText", string.Empty, new Vector2(16f, -456f), new Vector2(268f, 24f), 14),
            confirmButton = CreateButton(panelRect, "ConfirmButton", "발주 확정", new Vector2(16f, -488f), new Vector2(268f, 40f))
        };

        CreateLabel(panelRect, "HeaderText", "발주 (하루 1회)", new Vector2(16f, -8f), new Vector2(200f, 24f), 20)
            .fontStyle = FontStyles.Bold;

        refs.criticalWarningText.color = new Color(1f, 0.75f, 0.35f, 1f);
        refs.criticalWarningText.alignment = TextAlignmentOptions.TopLeft;
        refs.statusText.alignment = TextAlignmentOptions.TopLeft;

        return refs;
    }

    static RectTransform CreateScrollList(RectTransform panelRect)
    {
        GameObject scrollObject = CreateUIObject("OrderScroll", panelRect, typeof(ScrollRect), typeof(Image));
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 1f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 1f);
        scrollRectTransform.anchoredPosition = new Vector2(0f, -124f);
        scrollRectTransform.sizeDelta = new Vector2(-32f, 320f);

        Image scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.2f);

        GameObject viewportObject = CreateUIObject("ScrollViewport", scrollRectTransform, typeof(RectMask2D), typeof(Image));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        viewportObject.GetComponent<Image>().color = Color.clear;

        GameObject contentObject = CreateUIObject(ListContentName, viewportRect, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return contentRect;
    }

    public static SupplyOrderRowUI CreateOrderRow(Transform parent)
    {
        GameObject rowObject = CreateUIObject("OrderRow", parent, typeof(Image), typeof(LayoutElement), typeof(SupplyOrderRowUI));
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 52f);

        Image rowImage = rowObject.GetComponent<Image>();
        rowImage.color = new Color(1f, 1f, 1f, 0.08f);

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 52f;
        layoutElement.minHeight = 52f;

        TextMeshProUGUI nameText = CreateLabel(rowRect, "NameText", "-", new Vector2(8f, -8f), new Vector2(120f, 22f), 15);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI priceText = CreateLabel(rowRect, "PriceText", "0 Coin", new Vector2(8f, -30f), new Vector2(120f, 18f), 13);
        priceText.alignment = TextAlignmentOptions.MidlineLeft;
        priceText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        Button decreaseButton = CreateButton(rowRect, "DecreaseButton", "-", new Vector2(168f, -10f), new Vector2(32f, 32f));
        Button increaseButton = CreateButton(rowRect, "IncreaseButton", "+", new Vector2(232f, -10f), new Vector2(32f, 32f));
        TextMeshProUGUI quantityText = CreateLabel(rowRect, "QuantityText", "0", new Vector2(204f, -10f), new Vector2(24f, 32f), 18);
        quantityText.alignment = TextAlignmentOptions.Center;

        SupplyOrderRowUI row = rowObject.GetComponent<SupplyOrderRowUI>();
        row.Bind(nameText, priceText, quantityText, decreaseButton, increaseButton);
        return row;
    }

    static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        foreach (System.Type componentType in components)
        {
            if (componentType != typeof(RectTransform) && gameObject.GetComponent(componentType) == null)
                gameObject.AddComponent(componentType);
        }

        return gameObject;
    }

    static TextMeshProUGUI CreateLabel(
        RectTransform parent,
        string name,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
    {
        GameObject labelObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        if (font != null)
            label.font = font;

        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    static Button CreateButton(
        RectTransform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject buttonObject = CreateUIObject(name, parent, typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.45f, 0.75f, 1f);

        TextMeshProUGUI buttonLabel = CreateLabel(buttonRect, "Label", label, Vector2.zero, size, 16f);
        StretchFull(buttonLabel.rectTransform);
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}