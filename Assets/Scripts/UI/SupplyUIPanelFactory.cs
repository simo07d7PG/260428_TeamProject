using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>발주 UI 패널을 런타임에 구성합니다.</summary>
public static class SupplyUIPanelFactory
{
    const string PanelName = "SupplyPanel";
    const string ListContentName = "OrderListContent";
    const string PanelBodyName = "PanelBody";
    const string ToggleButtonName = "ToggleButton";
    const float PanelWidth = 400f;
    const float PanelHeight = 680f;
    const float PanelRightMargin = 0f;
    const float ToggleWidth = 36f;
    const float ToggleHeight = 120f;
    const float HorizontalPadding = 20f;
    const float HeaderTopPadding = 16f;
    const float FooterHeight = 120f;
    const float HeaderBlockHeight = 190f;
    const float OrderRowHeight = 70f;
    const float OrderRowButtonSize = 42f;
    const float OrderRowQuantityWidth = 44f;
    const float OrderRowControlSpacing = 6f;
    const float OrderRowRightPadding = 12f;
    const float OrderRowLeftPadding = 14f;

    public struct SupplyPanelRefs
    {
        public RectTransform drawerRoot;
        public RectTransform panelBody;
        public RectTransform listContentRoot;
        public Button toggleButton;
        public TextMeshProUGUI toggleLabel;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI totalCostText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI criticalWarningText;
        public Button confirmButton;
        public SupplyPanelDrawer drawer;
    }

    public static SupplyPanelRefs EnsurePanel(Transform canvasRoot)
    {
        Transform existing = FindSupplyPanel(canvasRoot);
        if (existing != null && !IsValidDrawer(existing))
        {
            Object.Destroy(existing.gameObject);
            existing = null;
        }

        SupplyPanelRefs refs = existing != null
            ? BindExisting(existing)
            : CreatePanel(canvasRoot);

        ApplyPanelLayout(refs);
        InitializeDrawer(refs);
        return refs;
    }

    static Transform FindSupplyPanel(Transform hostRoot)
    {
        Transform localPanel = ManagerUtility.FindDeepChild(hostRoot, PanelName);
        if (localPanel != null)
            return localPanel;

        if (hostRoot.parent == null)
            return null;

        Transform canvasPanel = hostRoot.parent.Find(PanelName);
        if (canvasPanel == null)
            return null;

        canvasPanel.SetParent(hostRoot, false);
        return canvasPanel;
    }

    static bool IsValidDrawer(Transform panel)
    {
        return panel != null
            && panel.Find(PanelBodyName) != null
            && panel.GetComponent<SupplyPanelDrawer>() != null;
    }

    static SupplyPanelRefs BindExisting(Transform drawerRoot)
    {
        Transform panelBody = drawerRoot.Find(PanelBodyName);
        SupplyPanelRefs refs = new SupplyPanelRefs
        {
            drawerRoot = drawerRoot as RectTransform,
            panelBody = panelBody as RectTransform,
            listContentRoot = panelBody != null
                ? panelBody.Find("OrderScroll/ScrollViewport/OrderListContent") as RectTransform
                : null,
            toggleButton = drawerRoot.Find(ToggleButtonName)?.GetComponent<Button>(),
            toggleLabel = drawerRoot.Find($"{ToggleButtonName}/Label")?.GetComponent<TextMeshProUGUI>(),
            coinText = panelBody?.Find("CoinText")?.GetComponent<TextMeshProUGUI>(),
            totalCostText = panelBody?.Find("TotalCostText")?.GetComponent<TextMeshProUGUI>(),
            statusText = panelBody?.Find("StatusText")?.GetComponent<TextMeshProUGUI>(),
            criticalWarningText = panelBody?.Find("CriticalWarningText")?.GetComponent<TextMeshProUGUI>(),
            confirmButton = panelBody?.Find("ConfirmButton")?.GetComponent<Button>(),
            drawer = drawerRoot.GetComponent<SupplyPanelDrawer>()
        };

        return refs;
    }

    static void ApplyPanelLayout(SupplyPanelRefs refs)
    {
        if (refs.drawerRoot == null || refs.panelBody == null)
            return;

        ApplyDrawerLayout(refs);
        ApplyHeaderLayout(refs);
        ApplyFooterLayout(refs);
        ApplyScrollLayout(refs.panelBody);
        ApplyExistingOrderRowsLayout(refs.listContentRoot);
        ConfigureWarningText(refs.criticalWarningText);
        ConfigureStatusText(refs.statusText);
    }

    static void ApplyExistingOrderRowsLayout(Transform listContentRoot)
    {
        if (listContentRoot == null)
            return;

        for (int i = 0; i < listContentRoot.childCount; i++)
        {
            Transform child = listContentRoot.GetChild(i);
            RectTransform rowRect = child as RectTransform;
            if (rowRect == null)
                continue;

            RectTransform nameRect = rowRect.Find("NameText") as RectTransform;
            RectTransform priceRect = rowRect.Find("PriceText") as RectTransform;
            RectTransform quantityRect = rowRect.Find("QuantityText") as RectTransform;
            RectTransform decreaseRect = rowRect.Find("DecreaseButton") as RectTransform;
            RectTransform increaseRect = rowRect.Find("IncreaseButton") as RectTransform;

            if (nameRect == null || priceRect == null || quantityRect == null || decreaseRect == null || increaseRect == null)
                continue;

            LayoutElement layoutElement = rowRect.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = OrderRowHeight;
                layoutElement.minHeight = OrderRowHeight;
            }

            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, OrderRowHeight);
            ConfigureOrderRowLayout(rowRect, nameRect, priceRect, quantityRect, decreaseRect, increaseRect);
        }
    }

    static void ApplyDrawerLayout(SupplyPanelRefs refs)
    {
        PinToRightEdge(refs.drawerRoot, 0f, 0f);

        PinToRightEdge(refs.panelBody, PanelWidth, PanelHeight, PanelRightMargin);
        refs.panelBody.localScale = Vector3.one;

        if (refs.toggleButton != null)
        {
            RectTransform toggleRect = refs.toggleButton.GetComponent<RectTransform>();
            PinToRightEdge(toggleRect, ToggleWidth, ToggleHeight, PanelRightMargin);
            toggleRect.SetAsLastSibling();
        }
    }

    static void PinToRightEdge(RectTransform rect, float width, float height, float insetFromRight = 0f)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-insetFromRight, 0f);
        rect.sizeDelta = new Vector2(width, height > 0f ? height : 0f);
        rect.localScale = Vector3.one;
    }

    static void InitializeDrawer(SupplyPanelRefs refs)
    {
        if (refs.drawerRoot == null || refs.panelBody == null || refs.toggleButton == null)
            return;

        if (refs.drawer == null)
            refs.drawer = ManagerUtility.GetOrAddComponent<SupplyPanelDrawer>(refs.drawerRoot.gameObject);

        refs.drawer.Initialize(
            refs.panelBody,
            refs.toggleButton,
            refs.toggleLabel,
            PanelWidth,
            -(PanelWidth + PanelRightMargin),
            PanelRightMargin,
            refs.drawer.IsExpanded);
    }

    static void ApplyHeaderLayout(SupplyPanelRefs refs)
    {
        float contentWidth = PanelWidth - HorizontalPadding * 2f;
        float y = -HeaderTopPadding;

        TextMeshProUGUI headerText = refs.panelBody.Find("HeaderText")?.GetComponent<TextMeshProUGUI>();
        if (headerText != null)
        {
            ApplyTopAnchoredLabel(headerText.rectTransform, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 34f));
            headerText.fontStyle = FontStyles.Bold;
            headerText.fontSize = 24f;
            y -= 38f;
        }

        if (refs.coinText != null)
        {
            ApplyTopAnchoredLabel(refs.coinText.rectTransform, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 28f));
            refs.coinText.fontSize = 20f;
            y -= 32f;
        }

        if (refs.totalCostText != null)
        {
            ApplyTopAnchoredLabel(refs.totalCostText.rectTransform, new Vector2(HorizontalPadding, y), new Vector2(contentWidth, 28f));
            refs.totalCostText.fontSize = 18f;
            y -= 32f;
        }

        if (refs.criticalWarningText != null)
        {
            float warningHeight = HeaderBlockHeight - (HeaderTopPadding + 38f + 32f + 32f);
            ApplyTopAnchoredLabel(
                refs.criticalWarningText.rectTransform,
                new Vector2(HorizontalPadding, y),
                new Vector2(contentWidth, warningHeight));
        }
    }

    static void ApplyFooterLayout(SupplyPanelRefs refs)
    {
        float contentWidth = PanelWidth - HorizontalPadding * 2f;

        if (refs.confirmButton != null)
        {
            RectTransform buttonRect = refs.confirmButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 14f);
            buttonRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, 48f);

            TextMeshProUGUI confirmLabel = refs.confirmButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (confirmLabel != null)
                confirmLabel.fontSize = 18f;
        }

        if (refs.statusText != null)
        {
            RectTransform statusRect = refs.statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 70f);
            statusRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, 36f);
            refs.statusText.fontSize = 15f;
        }
    }

    static void ApplyScrollLayout(RectTransform panelRect)
    {
        Transform scrollTransform = panelRect.Find("OrderScroll");
        if (scrollTransform == null)
            return;

        RectTransform scrollRectTransform = scrollTransform as RectTransform;
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.offsetMin = new Vector2(HorizontalPadding, FooterHeight);
        scrollRectTransform.offsetMax = new Vector2(-HorizontalPadding, -HeaderBlockHeight);
    }

    static SupplyPanelRefs CreatePanel(Transform canvasRoot)
    {
        RectTransform canvasRect = canvasRoot as RectTransform;
        Transform parent = canvasRect != null ? canvasRect : canvasRoot;

        GameObject drawerObject = CreateUIObject(PanelName, parent, typeof(SupplyPanelDrawer));
        RectTransform drawerRect = drawerObject.GetComponent<RectTransform>();
        drawerObject.transform.SetAsLastSibling();

        Button toggleButton = CreateToggleButton(drawerRect, ">");
        TextMeshProUGUI toggleLabel = toggleButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

        GameObject bodyObject = CreateUIObject(PanelBodyName, drawerRect, typeof(Image));
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        Image panelImage = bodyObject.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);

        TextMeshProUGUI headerText = CreateLabel(bodyRect, "HeaderText", "발주 (하루 1회)", Vector2.zero, Vector2.zero, 20);
        headerText.fontStyle = FontStyles.Bold;

        SupplyPanelRefs refs = new SupplyPanelRefs
        {
            drawerRoot = drawerRect,
            panelBody = bodyRect,
            toggleButton = toggleButton,
            toggleLabel = toggleLabel,
            drawer = drawerObject.GetComponent<SupplyPanelDrawer>(),
            coinText = CreateLabel(bodyRect, "CoinText", "보유 코인: 0", Vector2.zero, Vector2.zero, 17),
            totalCostText = CreateLabel(bodyRect, "TotalCostText", "발주 합계: 0 Coin", Vector2.zero, Vector2.zero, 16),
            criticalWarningText = CreateLabel(bodyRect, "CriticalWarningText", string.Empty, Vector2.zero, Vector2.zero, 14),
            listContentRoot = CreateScrollList(bodyRect),
            statusText = CreateLabel(bodyRect, "StatusText", string.Empty, Vector2.zero, Vector2.zero, 14),
            confirmButton = CreateButton(bodyRect, "ConfirmButton", "발주 확정", Vector2.zero, Vector2.zero)
        };

        return refs;
    }

    static Button CreateToggleButton(RectTransform drawerRect, string label)
    {
        Button toggleButton = CreateButton(drawerRect, ToggleButtonName, label, Vector2.zero, new Vector2(ToggleWidth, ToggleHeight));
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();

        Image toggleImage = toggleButton.GetComponent<Image>();
        toggleImage.color = new Color(0.18f, 0.2f, 0.24f, 0.95f);

        TextMeshProUGUI toggleLabel = toggleRect.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (toggleLabel != null)
        {
            toggleLabel.fontSize = 26f;
            toggleLabel.fontStyle = FontStyles.Bold;
            toggleLabel.alignment = TextAlignmentOptions.Center;
        }

        return toggleButton;
    }

    static RectTransform CreateScrollList(RectTransform panelRect)
    {
        GameObject scrollObject = CreateUIObject("OrderScroll", panelRect, typeof(ScrollRect), typeof(Image));
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();

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
        layout.spacing = 8f;
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
        rowRect.sizeDelta = new Vector2(0f, OrderRowHeight);

        Image rowImage = rowObject.GetComponent<Image>();
        rowImage.color = new Color(1f, 1f, 1f, 0.08f);

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = OrderRowHeight;
        layoutElement.minHeight = OrderRowHeight;

        TextMeshProUGUI nameText = CreateLabel(rowRect, "NameText", "-", Vector2.zero, Vector2.zero, 18);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI priceText = CreateLabel(rowRect, "PriceText", "0 Coin", Vector2.zero, Vector2.zero, 15);
        priceText.alignment = TextAlignmentOptions.MidlineLeft;
        priceText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        Button decreaseButton = CreateButton(rowRect, "DecreaseButton", "-", Vector2.zero, new Vector2(OrderRowButtonSize, OrderRowButtonSize));
        Button increaseButton = CreateButton(rowRect, "IncreaseButton", "+", Vector2.zero, new Vector2(OrderRowButtonSize, OrderRowButtonSize));
        TextMeshProUGUI quantityText = CreateLabel(rowRect, "QuantityText", "0", Vector2.zero, Vector2.zero, 20);
        quantityText.alignment = TextAlignmentOptions.Center;

        ConfigureOrderRowLayout(
            rowRect,
            nameText.rectTransform,
            priceText.rectTransform,
            quantityText.rectTransform,
            decreaseButton.GetComponent<RectTransform>(),
            increaseButton.GetComponent<RectTransform>());

        SetButtonLabelSize(decreaseButton, 22f);
        SetButtonLabelSize(increaseButton, 22f);

        SupplyOrderRowUI row = rowObject.GetComponent<SupplyOrderRowUI>();
        row.Bind(nameText, priceText, quantityText, decreaseButton, increaseButton);
        return row;
    }

    static void ConfigureOrderRowLayout(
        RectTransform rowRect,
        RectTransform nameRect,
        RectTransform priceRect,
        RectTransform quantityRect,
        RectTransform decreaseRect,
        RectTransform increaseRect)
    {
        float controlBlockWidth = GetOrderRowControlBlockWidth();

        ApplyInfoLabel(nameRect, OrderRowLeftPadding, -10f, controlBlockWidth, 28f);
        ApplyInfoLabel(priceRect, OrderRowLeftPadding, -38f, controlBlockWidth, 22f);

        float increaseInset = OrderRowRightPadding;
        float quantityInset = increaseInset + OrderRowButtonSize + OrderRowControlSpacing;
        float decreaseInset = quantityInset + OrderRowQuantityWidth + OrderRowControlSpacing;

        PinRightCenter(increaseRect, increaseInset, OrderRowButtonSize, OrderRowButtonSize);
        PinRightCenter(quantityRect, quantityInset, OrderRowQuantityWidth, OrderRowButtonSize);
        PinRightCenter(decreaseRect, decreaseInset, OrderRowButtonSize, OrderRowButtonSize);
    }

    static float GetOrderRowControlBlockWidth()
    {
        return OrderRowRightPadding
            + OrderRowButtonSize * 2f
            + OrderRowQuantityWidth
            + OrderRowControlSpacing * 2f
            + OrderRowLeftPadding;
    }

    static void ApplyInfoLabel(RectTransform labelRect, float leftPadding, float topOffset, float rightMargin, float height)
    {
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.offsetMin = new Vector2(leftPadding, topOffset - height);
        labelRect.offsetMax = new Vector2(-rightMargin, topOffset);
    }

    static void PinRightCenter(RectTransform rect, float insetFromRight, float width, float height)
    {
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-insetFromRight, 0f);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void SetButtonLabelSize(Button button, float fontSize)
    {
        if (button == null)
            return;

        TextMeshProUGUI label = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
            label.fontSize = fontSize;
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

    static void ApplyTopAnchoredLabel(RectTransform labelRect, Vector2 anchoredPosition, Vector2 size)
    {
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;
    }

    static void ConfigureWarningText(TextMeshProUGUI warningText)
    {
        if (warningText == null)
            return;

        warningText.color = new Color(1f, 0.75f, 0.35f, 1f);
        warningText.alignment = TextAlignmentOptions.TopLeft;
        warningText.enableWordWrapping = true;
        warningText.overflowMode = TextOverflowModes.Truncate;
        warningText.fontSize = 15f;
        warningText.lineSpacing = -4f;
    }

    static void ConfigureStatusText(TextMeshProUGUI statusText)
    {
        if (statusText == null)
            return;

        statusText.alignment = TextAlignmentOptions.BottomLeft;
        statusText.enableWordWrapping = true;
        statusText.overflowMode = TextOverflowModes.Ellipsis;
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