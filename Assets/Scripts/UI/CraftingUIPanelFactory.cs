using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 UI 패널을 런타임에 구성합니다.
/// </summary>
public static class CraftingUIPanelFactory
{
    const string PanelName = "CraftingPanel";
    const string ListContentName = "IngredientListContent";
    const float PanelWidth = 520f;
    const float PanelHeight = 420f;
    const float HorizontalPadding = 20f;
    const float HeaderHeight = 120f;
    const float FooterHeight = 96f;
    const float IngredientRowHeight = 64f;

    public struct CraftingPanelRefs
    {
        public RectTransform panelRoot;
        public RectTransform listContentRoot;
        public TextMeshProUGUI headerText;
        public TextMeshProUGUI stageText;
        public TextMeshProUGUI successRateText;
        public TextMeshProUGUI statusText;
        public Image progressFill;
        public Image successFill;
        public Button startButton;
        public Button skipButton;
        public Button deliverButton;
        public Button resetButton;
    }

    public static CraftingPanelRefs EnsurePanel(Transform hostRoot)
    {
        Transform existing = ManagerUtility.FindDeepChild(hostRoot, PanelName);
        CraftingPanelRefs refs = existing != null ? BindExisting(existing) : CreatePanel(hostRoot);
        ApplyPanelLayout(refs);
        return refs;
    }

    static CraftingPanelRefs BindExisting(Transform panelRoot)
    {
        Transform body = panelRoot.Find("PanelBody");
        return new CraftingPanelRefs
        {
            panelRoot = panelRoot as RectTransform,
            listContentRoot = body != null
                ? body.Find("IngredientScroll/ScrollViewport/IngredientListContent") as RectTransform
                : null,
            headerText = body?.Find("HeaderText")?.GetComponent<TextMeshProUGUI>(),
            stageText = body?.Find("StageText")?.GetComponent<TextMeshProUGUI>(),
            successRateText = body?.Find("SuccessRateText")?.GetComponent<TextMeshProUGUI>(),
            statusText = body?.Find("StatusText")?.GetComponent<TextMeshProUGUI>(),
            progressFill = body?.Find("ProgressBar/Fill")?.GetComponent<Image>(),
            successFill = body?.Find("SuccessBar/Fill")?.GetComponent<Image>(),
            startButton = body?.Find("StartButton")?.GetComponent<Button>(),
            skipButton = body?.Find("SkipButton")?.GetComponent<Button>(),
            deliverButton = body?.Find("DeliverButton")?.GetComponent<Button>(),
            resetButton = body?.Find("ResetButton")?.GetComponent<Button>()
        };
    }

    static void ApplyPanelLayout(CraftingPanelRefs refs)
    {
        if (refs.panelRoot == null)
            return;

        RectTransform panelRect = refs.panelRoot;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 24f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRect.localScale = Vector3.one;

        Transform body = panelRect.Find("PanelBody");
        if (body == null)
            return;

        RectTransform bodyRect = body as RectTransform;
        StretchFull(bodyRect);

        ApplyHeaderLayout(refs, bodyRect);
        ApplyFooterLayout(refs, bodyRect);
        ApplyScrollLayout(bodyRect);
    }

    static void ApplyHeaderLayout(CraftingPanelRefs refs, RectTransform bodyRect)
    {
        float contentWidth = PanelWidth - HorizontalPadding * 2f;
        float y = -12f;

        if (refs.headerText != null)
        {
            ApplyTopAnchoredLabel(refs.headerText.rectTransform, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 30f));
            refs.headerText.fontStyle = FontStyles.Bold;
            refs.headerText.fontSize = 22f;
            y -= 34f;
        }

        if (refs.stageText != null)
        {
            ApplyTopAnchoredLabel(refs.stageText.rectTransform, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 24f));
            refs.stageText.fontSize = 17f;
            y -= 28f;
        }

        if (refs.successRateText != null)
        {
            ApplyTopAnchoredLabel(refs.successRateText.rectTransform, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 22f));
            refs.successRateText.fontSize = 15f;
            y -= 26f;
        }

        ConfigureBar(bodyRect.Find("SuccessBar") as RectTransform, refs.successFill, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 14f));
        y -= 22f;
        ConfigureBar(bodyRect.Find("ProgressBar") as RectTransform, refs.progressFill, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 16f));
    }

    static void ApplyFooterLayout(CraftingPanelRefs refs, RectTransform bodyRect)
    {
        float buttonWidth = (PanelWidth - HorizontalPadding * 2f - 12f) * 0.5f;

        ConfigureFooterButton(refs.startButton, new Vector2(HorizontalPadding, 14f), new Vector2(buttonWidth, 40f));
        ConfigureFooterButton(refs.skipButton, new Vector2(HorizontalPadding + buttonWidth + 12f, 14f), new Vector2(buttonWidth, 40f));
        ConfigureFooterButton(refs.deliverButton, new Vector2(HorizontalPadding, 60f), new Vector2(buttonWidth, 40f));
        ConfigureFooterButton(refs.resetButton, new Vector2(HorizontalPadding + buttonWidth + 12f, 60f), new Vector2(buttonWidth, 40f));

        if (refs.statusText != null)
        {
            RectTransform statusRect = refs.statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 106f);
            statusRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, 28f);
            refs.statusText.fontSize = 14f;
            refs.statusText.alignment = TextAlignmentOptions.BottomLeft;
        }
    }

    static void ApplyScrollLayout(RectTransform bodyRect)
    {
        Transform scrollTransform = bodyRect.Find("IngredientScroll");
        if (scrollTransform == null)
            return;

        RectTransform scrollRect = scrollTransform as RectTransform;
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = new Vector2(HorizontalPadding, FooterHeight);
        scrollRect.offsetMax = new Vector2(-HorizontalPadding, -HeaderHeight);
    }

    static CraftingPanelRefs CreatePanel(Transform hostRoot)
    {
        GameObject panelObject = CreateUIObject(PanelName, hostRoot);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();

        GameObject bodyObject = CreateUIObject("PanelBody", panelRect, typeof(Image));
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyObject.GetComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 0.94f);
        StretchFull(bodyRect);

        CraftingPanelRefs refs = new CraftingPanelRefs
        {
            panelRoot = panelRect,
            headerText = CreateLabel(bodyRect, "HeaderText", "음식 제작", Vector2.zero, Vector2.zero, 20),
            stageText = CreateLabel(bodyRect, "StageText", "단계: 베이스", Vector2.zero, Vector2.zero, 16),
            successRateText = CreateLabel(bodyRect, "SuccessRateText", "성공도: 0%", Vector2.zero, Vector2.zero, 14),
            progressFill = CreateBar(bodyRect, "ProgressBar", new Color(0.25f, 0.55f, 0.9f, 1f)),
            successFill = CreateBar(bodyRect, "SuccessBar", new Color(0.35f, 0.82f, 0.45f, 1f)),
            listContentRoot = CreateScrollList(bodyRect),
            statusText = CreateLabel(bodyRect, "StatusText", string.Empty, Vector2.zero, Vector2.zero, 14),
            startButton = CreateButton(bodyRect, "StartButton", "제작 시작", Vector2.zero, Vector2.zero),
            skipButton = CreateButton(bodyRect, "SkipButton", "토핑 생략", Vector2.zero, Vector2.zero),
            deliverButton = CreateButton(bodyRect, "DeliverButton", "손님 전달", Vector2.zero, Vector2.zero),
            resetButton = CreateButton(bodyRect, "ResetButton", "초기화", Vector2.zero, Vector2.zero)
        };

        refs.headerText.fontStyle = FontStyles.Bold;
        return refs;
    }

    public static CraftingIngredientSlotUI CreateIngredientRow(Transform parent)
    {
        GameObject rowObject = CreateUIObject(
            "IngredientRow",
            parent,
            typeof(Image),
            typeof(LayoutElement),
            typeof(Button),
            typeof(CraftingIngredientSlotUI));

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, IngredientRowHeight);

        Image rowImage = rowObject.GetComponent<Image>();
        rowImage.color = new Color(1f, 1f, 1f, 0.08f);

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = IngredientRowHeight;
        layoutElement.minHeight = IngredientRowHeight;

        GameObject iconObject = CreateUIObject("Icon", rowRect, typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(44f, 44f);
        iconObject.GetComponent<Image>().preserveAspect = true;

        TextMeshProUGUI nameText = CreateLabel(rowRect, "NameText", "-", Vector2.zero, Vector2.zero, 16);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.offsetMin = new Vector2(64f, 8f);
        nameRect.offsetMax = new Vector2(-12f, 28f);

        TextMeshProUGUI metaText = CreateLabel(rowRect, "MetaText", "Lv1", Vector2.zero, Vector2.zero, 13);
        metaText.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        metaText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform metaRect = metaText.rectTransform;
        metaRect.anchorMin = new Vector2(0f, 0.5f);
        metaRect.anchorMax = new Vector2(1f, 0.5f);
        metaRect.pivot = new Vector2(0f, 0.5f);
        metaRect.offsetMin = new Vector2(64f, -24f);
        metaRect.offsetMax = new Vector2(-12f, -6f);

        Button button = rowObject.GetComponent<Button>();
        CraftingIngredientSlotUI slot = rowObject.GetComponent<CraftingIngredientSlotUI>();
        slot.Bind(
            iconObject.GetComponent<Image>(),
            rowImage,
            nameText,
            metaText,
            button);

        return slot;
    }

    static RectTransform CreateScrollList(RectTransform panelRect)
    {
        GameObject scrollObject = CreateUIObject("IngredientScroll", panelRect, typeof(ScrollRect), typeof(Image));
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);

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

    static Image CreateBar(RectTransform parent, string name, Color fillColor)
    {
        GameObject barObject = CreateUIObject(name, parent, typeof(Image));
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        GameObject fillObject = CreateUIObject("Fill", barRect, typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;
        return fillImage;
    }

    static void ConfigureBar(RectTransform barRect, Image fillImage, Vector2 anchoredPosition, Vector2 size)
    {
        if (barRect == null)
            return;

        ApplyTopAnchoredLabel(barRect, anchoredPosition, size);

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
        }
    }

    static void ConfigureFooterButton(Button button, Vector2 anchoredPosition, Vector2 size)
    {
        if (button == null)
            return;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 0f);
        buttonRect.pivot = new Vector2(0f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;
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
        label.text = UIFontUtility.Sanitize(text);
        UIFontUtility.Apply(label);
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
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
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 0f);
        buttonRect.pivot = new Vector2(0f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.75f, 1f);

        TextMeshProUGUI buttonLabel = CreateLabel(buttonRect, "Label", label, Vector2.zero, size, 15f);
        StretchFull(buttonLabel.rectTransform);
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    static void ApplyTopAnchoredLabel(RectTransform labelRect, Vector2 anchoredPosition, Vector2 size)
    {
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;
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