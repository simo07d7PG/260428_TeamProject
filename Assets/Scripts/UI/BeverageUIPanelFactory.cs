using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>사진 기준 카페 카운터 레이아웃을 런타임에 구성합니다.</summary>
public static class BeverageUIPanelFactory
{
    const string PanelName = "BeveragePanel";

    [System.Serializable]
    public struct BeverageUIRefs
    {
        public RectTransform panelRoot;

        public Image orderIcon;
        public TextMeshProUGUI orderNameText;
        public TextMeshProUGUI orderCompText;
        public Image orderPatienceFill;
        public TextMeshProUGUI menuEstimateText;
        public TextMeshProUGUI statusText;

        public RectTransform cupRoot;
        public CupCanvasUI cupCanvas;
        public CupDragHandler cupDrag;
        public Button cupStackButton;
        public RectTransform serveZone;
        public Vector2 cupStackHome;
        public Vector2 cupHeldHome;
        public Vector2 cupMachinePos;

        public RectTransform machineSlot;
        public RectTransform machineButtonRect;
        public RectTransform machineNeedle;
        public MachineButtonInteraction machineButton;
        public TextMeshProUGUI machineLabel;

        public BeverageTool milkTool;
        public BeverageTool iceTool;
        public BeverageTool toppingTool;
        public BeverageTool syrupTool;

        public List<MaterialSelectorUI> selectors;

        public Button newCupButton;
        public Button clearButton;
    }

    public static BeverageUIRefs EnsurePanel(Transform hostRoot)
    {
        Transform existing = ManagerUtility.FindDeepChild(hostRoot, PanelName);
        if (existing != null)
            return BindExisting(existing);

        return CreatePanel(hostRoot);
    }

    /// <summary>씬에 구워진 BeveragePanel을 재탐색해 런타임 전용 배선을 다시 연결합니다.</summary>
    public static BeverageUIRefs BindExisting(Transform panelRoot)
    {
        BeverageUIRefs refs = new BeverageUIRefs
        {
            panelRoot = panelRoot as RectTransform,
            selectors = new List<MaterialSelectorUI>(),
            cupStackHome = SlotPos(Anchors?.cupStack, CafeLayoutConfig.CupStackHome),
            cupHeldHome = SlotPos(Anchors?.cupHeld, CafeLayoutConfig.CupHeldHome),
            cupMachinePos = SlotPos(Anchors?.cupMachine, CafeLayoutConfig.CupMachinePos)
        };

        refs.orderIcon = FindImage(panelRoot, "OrderIcon");
        refs.orderNameText = FindText(panelRoot, "OrderName");
        refs.orderCompText = FindText(panelRoot, "OrderComp");
        refs.menuEstimateText = FindText(panelRoot, "Estimate");
        Transform patienceBar = ManagerUtility.FindDeepChild(panelRoot, "PatienceBar");
        refs.orderPatienceFill = patienceBar != null ? patienceBar.Find("Fill")?.GetComponent<Image>() : null;
        refs.statusText = FindText(panelRoot, "StatusText");

        refs.serveZone = ManagerUtility.FindDeepChild(panelRoot, "ServeZone") as RectTransform;
        refs.cupStackButton = FindButton(panelRoot, "CupStack");

        refs.cupCanvas = panelRoot.GetComponentInChildren<CupCanvasUI>(true);
        refs.cupDrag = panelRoot.GetComponentInChildren<CupDragHandler>(true);
        if (refs.cupCanvas != null)
        {
            Transform cup = refs.cupCanvas.transform;
            refs.cupRoot = cup as RectTransform;
            refs.cupCanvas.Bind(
                ManagerUtility.FindDeepChild(cup, "LiquidArea") as RectTransform,
                ManagerUtility.FindDeepChild(cup, "DecorArea") as RectTransform,
                FindImage(cup, "EspressoFill"),
                FindImage(cup, "MilkFill"),
                FindImage(cup, "Lid"),
                FindText(cup, "EmptyHint"));
        }

        refs.machineButton = panelRoot.GetComponentInChildren<MachineButtonInteraction>(true);
        if (refs.machineButton != null)
        {
            refs.machineButtonRect = refs.machineButton.transform as RectTransform;
            refs.machineNeedle = ManagerUtility.FindDeepChild(refs.machineButton.transform, "Needle") as RectTransform;
        }
        refs.machineSlot = ManagerUtility.FindDeepChild(panelRoot, "CupSlot") as RectTransform;
        refs.machineLabel = FindText(panelRoot, "MachineLabel");

        foreach (BeverageTool tool in panelRoot.GetComponentsInChildren<BeverageTool>(true))
        {
            BeverageToolKind kind = ToolKind(tool.gameObject.name);
            tool.Bind(kind, refs.cupCanvas);
            switch (kind)
            {
                case BeverageToolKind.Milk: refs.milkTool = tool; break;
                case BeverageToolKind.Ice: refs.iceTool = tool; break;
                case BeverageToolKind.Topping: refs.toppingTool = tool; break;
                case BeverageToolKind.Syrup: refs.syrupTool = tool; break;
            }
        }

        foreach (MaterialSelectorUI selector in panelRoot.GetComponentsInChildren<MaterialSelectorUI>(true))
        {
            Transform t = selector.transform;
            Button left = t.Find("Left")?.GetComponent<Button>();
            Button right = t.Find("Right")?.GetComponent<Button>();
            left?.onClick.RemoveAllListeners();
            right?.onClick.RemoveAllListeners();
            selector.Bind(
                SelectorType(selector.gameObject.name),
                t.Find("Label")?.GetComponent<TextMeshProUGUI>(),
                left,
                right);
            refs.selectors.Add(selector);
        }

        // 컵 도킹 3위치(stack/held/machine)를 씬에 배치된 실제 오브젝트에서 직접 유도해
        // 사용자가 옮긴 레이아웃에 정확히 맞춘다(config 폴백은 런타임 빌드 경로에서만 사용).
        if (refs.cupRoot != null)
            refs.cupStackHome = refs.cupRoot.anchoredPosition;
        if (refs.machineSlot != null)
            refs.cupMachinePos = refs.machineSlot.anchoredPosition;
        refs.cupHeldHome = Vector2.Lerp(refs.cupStackHome, refs.cupMachinePos, 0.5f);

        if (refs.cupDrag != null)
            refs.cupDrag.Bind(refs.machineSlot, refs.machineButtonRect, refs.serveZone, refs.cupMachinePos, refs.cupHeldHome, refs.cupStackHome);
        if (refs.machineButton != null)
            refs.machineButton.Bind(refs.machineNeedle, refs.cupDrag);

        return refs;
    }

    static Image FindImage(Transform root, string name)
    {
        Transform t = ManagerUtility.FindDeepChild(root, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    static TextMeshProUGUI FindText(Transform root, string name)
    {
        Transform t = ManagerUtility.FindDeepChild(root, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    static Button FindButton(Transform root, string name)
    {
        Transform t = ManagerUtility.FindDeepChild(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static BeverageToolKind ToolKind(string objectName)
    {
        if (objectName.EndsWith("Ice")) return BeverageToolKind.Ice;
        if (objectName.EndsWith("Topping")) return BeverageToolKind.Topping;
        if (objectName.EndsWith("Syrup")) return BeverageToolKind.Syrup;
        return BeverageToolKind.Milk;
    }

    static IngredientType SelectorType(string objectName)
    {
        if (objectName.EndsWith("Milk")) return IngredientType.Milk;
        if (objectName.EndsWith("Topping")) return IngredientType.Topping;
        if (objectName.EndsWith("Syrup")) return IngredientType.Syrup;
        return IngredientType.Base;
    }

    static CafeLayoutAnchors Anchors => CafeLayoutAnchors.Instance;

    static Vector2 SlotPos(RectTransform marker, Vector2 fallback) => marker != null ? marker.anchoredPosition : fallback;

    static Vector2 SlotSize(RectTransform marker, Vector2 fallback) => marker != null ? marker.sizeDelta : fallback;

    static void PlaceWithMarker(RectTransform target, RectTransform marker, Vector2 fallbackPos, Vector2 fallbackSize)
    {
        if (marker != null)
        {
            target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = marker.anchoredPosition;
            target.sizeDelta = marker.sizeDelta;
        }
        else
        {
            target.anchoredPosition = fallbackPos;
            target.sizeDelta = fallbackSize;
        }
    }

    public static BeverageUIRefs CreatePanel(Transform hostRoot)
    {
        GameObject panelObject = CreateUIObject(PanelName, hostRoot, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        StretchFull(panelRect);
        Image panelImage = panelObject.GetComponent<Image>();
        Sprite background = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.ServiceBackground : null;
        if (background != null)
        {
            panelImage.sprite = background;
            panelImage.color = Color.white;
            panelImage.type = Image.Type.Simple;
        }
        else
        {
            CafeSpriteUtility.ApplyStation(panelImage, "Counter", new Color(0.12f, 0.10f, 0.09f, 0.97f));
        }

        BeverageUIRefs refs = new BeverageUIRefs
        {
            panelRoot = panelRect,
            selectors = new List<MaterialSelectorUI>(),
            cupStackHome = SlotPos(Anchors?.cupStack, CafeLayoutConfig.CupStackHome),
            cupHeldHome = SlotPos(Anchors?.cupHeld, CafeLayoutConfig.CupHeldHome),
            cupMachinePos = SlotPos(Anchors?.cupMachine, CafeLayoutConfig.CupMachinePos)
        };

        BuildOrderBanner(panelRect, ref refs);

        refs.serveZone = CreateServeZone(panelRect);

        refs.cupStackButton = CreateStackDecor(panelRect, refs.cupStackHome);
        refs.cupCanvas = CreateCup(panelRect, refs.cupStackHome, out refs.cupRoot, out refs.cupDrag);

        Vector2 machinePos = SlotPos(Anchors?.machine, CafeLayoutConfig.MachinePos);
        BuildMachine(panelRect, ref refs, machinePos);

        refs.milkTool = CreateTool(panelRect, refs, BeverageToolKind.Milk, "우유", new Color(0.85f, 0.88f, 0.92f, 1f),
            SlotPos(Anchors?.milkTool, CafeLayoutConfig.MilkToolPos), SlotSize(Anchors?.milkTool, CafeLayoutConfig.ToolSize));
        refs.iceTool = CreateTool(panelRect, refs, BeverageToolKind.Ice, "얼음", new Color(0.62f, 0.80f, 0.92f, 1f),
            SlotPos(Anchors?.iceTool, CafeLayoutConfig.IceToolPos), SlotSize(Anchors?.iceTool, CafeLayoutConfig.ToolSize));
        refs.toppingTool = CreateTool(panelRect, refs, BeverageToolKind.Topping, "토핑", new Color(0.98f, 0.78f, 0.83f, 1f),
            SlotPos(Anchors?.toppingTool, CafeLayoutConfig.ToppingToolPos), SlotSize(Anchors?.toppingTool, CafeLayoutConfig.ToolSize));
        refs.syrupTool = CreateTool(panelRect, refs, BeverageToolKind.Syrup, "시럽", new Color(0.85f, 0.62f, 0.20f, 1f),
            SlotPos(Anchors?.syrupTool, CafeLayoutConfig.SyrupToolPos), SlotSize(Anchors?.syrupTool, CafeLayoutConfig.ToolSize));

        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Base,
            SlotPos(Anchors?.baseSelector, CafeLayoutConfig.BaseSelectorPos), SlotSize(Anchors?.baseSelector, CafeLayoutConfig.SelectorSize)));
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Milk,
            SlotPos(Anchors?.milkSelector, CafeLayoutConfig.MilkSelectorPos), SlotSize(Anchors?.milkSelector, CafeLayoutConfig.SelectorSize)));
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Topping,
            SlotPos(Anchors?.toppingSelector, CafeLayoutConfig.ToppingSelectorPos), SlotSize(Anchors?.toppingSelector, CafeLayoutConfig.SelectorSize)));
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Syrup,
            SlotPos(Anchors?.syrupSelector, CafeLayoutConfig.SyrupSelectorPos), SlotSize(Anchors?.syrupSelector, CafeLayoutConfig.SelectorSize)));

        refs.cupRoot.SetAsLastSibling();

        CreateMachineSpout(panelRect, machinePos);

        refs.cupDrag.Bind(refs.machineSlot, refs.machineButtonRect, refs.serveZone, refs.cupMachinePos, refs.cupHeldHome, refs.cupStackHome);
        refs.machineButton.Bind(refs.machineNeedle, refs.cupDrag);

        refs.statusText = CreateLabel(panelRect, "StatusText", string.Empty, 16);
        ConfigureBottomAnchored(refs.statusText.rectTransform, 96f, 820f, 34f);
        refs.statusText.color = new Color(0.95f, 0.9f, 0.65f, 1f);

        refs.newCupButton = CreateControlButton(panelRect, "NewCupButton", "새 컵", new Color(0.25f, 0.5f, 0.78f, 1f), new Vector2(-90f, 34f));
        refs.clearButton = CreateControlButton(panelRect, "ClearButton", "비우기", new Color(0.5f, 0.3f, 0.3f, 1f), new Vector2(90f, 34f));

        return refs;
    }

    static void BuildOrderBanner(RectTransform parent, ref BeverageUIRefs refs)
    {
        GameObject bannerObj = CreateUIObject("OrderBanner", parent, typeof(Image));
        RectTransform banner = bannerObj.GetComponent<RectTransform>();
        banner.anchorMin = new Vector2(0.5f, 1f); banner.anchorMax = new Vector2(0.5f, 1f); banner.pivot = new Vector2(0.5f, 1f);
        PlaceWithMarker(banner, Anchors?.banner, CafeLayoutConfig.BannerPos, CafeLayoutConfig.BannerSize);
        bannerObj.GetComponent<Image>().color = new Color(0.16f, 0.14f, 0.13f, 0.96f);

        GameObject iconObj = CreateUIObject("OrderIcon", banner, typeof(Image));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f); iconRect.anchorMax = new Vector2(0f, 0.5f); iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 8f); iconRect.sizeDelta = new Vector2(96f, 96f);
        refs.orderIcon = iconObj.GetComponent<Image>();
        refs.orderIcon.preserveAspect = true; refs.orderIcon.enabled = false;

        refs.orderNameText = CreateLabel(banner, "OrderName", "주문 대기 중", 30);
        refs.orderNameText.fontStyle = FontStyles.Bold; refs.orderNameText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform nameRect = refs.orderNameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f); nameRect.anchorMax = new Vector2(1f, 1f); nameRect.pivot = new Vector2(0f, 1f);
        nameRect.offsetMin = new Vector2(126f, -52f); nameRect.offsetMax = new Vector2(-16f, -10f);

        refs.orderCompText = CreateLabel(banner, "OrderComp", string.Empty, 18);
        refs.orderCompText.alignment = TextAlignmentOptions.MidlineLeft; refs.orderCompText.color = new Color(0.8f, 0.86f, 1f, 1f);
        RectTransform compRect = refs.orderCompText.rectTransform;
        compRect.anchorMin = new Vector2(0f, 1f); compRect.anchorMax = new Vector2(1f, 1f); compRect.pivot = new Vector2(0f, 1f);
        compRect.offsetMin = new Vector2(126f, -84f); compRect.offsetMax = new Vector2(-16f, -54f);

        refs.menuEstimateText = CreateLabel(banner, "Estimate", "예상: -", 16);
        refs.menuEstimateText.alignment = TextAlignmentOptions.MidlineRight; refs.menuEstimateText.color = new Color(0.75f, 0.86f, 1f, 1f);
        RectTransform estRect = refs.menuEstimateText.rectTransform;
        estRect.anchorMin = new Vector2(0f, 1f); estRect.anchorMax = new Vector2(1f, 1f); estRect.pivot = new Vector2(1f, 1f);
        estRect.offsetMin = new Vector2(126f, -52f); estRect.offsetMax = new Vector2(-16f, -12f);

        Image patienceFill = UIFactoryUtility.CreateFilledBar(banner, "PatienceBar", new Color(0f, 0f, 0f, 0.4f), new Color(0.35f, 0.8f, 0.4f, 1f));
        RectTransform barRect = patienceFill.rectTransform.parent as RectTransform;
        barRect.anchorMin = new Vector2(0f, 0f); barRect.anchorMax = new Vector2(1f, 0f); barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = new Vector2(16f, 14f); barRect.offsetMax = new Vector2(-16f, 32f);
        refs.orderPatienceFill = patienceFill;
    }

    static void BuildMachine(RectTransform parent, ref BeverageUIRefs refs, Vector2 pos)
    {
        GameObject body = CreateUIObject("Machine", parent, typeof(Image));
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f); bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = pos; bodyRect.sizeDelta = SlotSize(Anchors?.machine, CafeLayoutConfig.MachineSize);
        Image bodyImg = body.GetComponent<Image>();
        bodyImg.raycastTarget = false;
        CafeSpriteUtility.ApplyStation(bodyImg, "EspressoShot", new Color(0.30f, 0.20f, 0.16f, 1f));

        refs.machineLabel = CreateLabel(bodyRect, "MachineLabel", "커피 머신", 15);
        RectTransform mlRect = refs.machineLabel.rectTransform;
        mlRect.anchorMin = new Vector2(0f, 1f); mlRect.anchorMax = new Vector2(1f, 1f); mlRect.pivot = new Vector2(0.5f, 1f);
        mlRect.offsetMin = new Vector2(4f, -40f); mlRect.offsetMax = new Vector2(-4f, -6f);
        refs.machineLabel.raycastTarget = false;

        GameObject slot = CreateUIObject("CupSlot", parent);
        refs.machineSlot = slot.GetComponent<RectTransform>();
        refs.machineSlot.anchorMin = refs.machineSlot.anchorMax = new Vector2(0.5f, 0.5f); refs.machineSlot.pivot = new Vector2(0.5f, 0.5f);
        refs.machineSlot.anchoredPosition = new Vector2(pos.x, pos.y - 170f);
        refs.machineSlot.sizeDelta = new Vector2(190f, 200f);

        Vector2 gaugeOffset = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GaugeOffset : new Vector2(0f, 30f);
        GameObject button = CreateUIObject("MachineButton", parent, typeof(Image), typeof(MachineButtonInteraction));
        refs.machineButtonRect = button.GetComponent<RectTransform>();
        refs.machineButtonRect.anchorMin = refs.machineButtonRect.anchorMax = new Vector2(0.5f, 0.5f); refs.machineButtonRect.pivot = new Vector2(0.5f, 0.5f);
        refs.machineButtonRect.anchoredPosition = new Vector2(pos.x + gaugeOffset.x, pos.y + gaugeOffset.y);
        refs.machineButtonRect.sizeDelta = new Vector2(96f, 96f);
        Image dial = button.GetComponent<Image>();
        dial.sprite = UIShapeUtility.Disc();
        dial.type = Image.Type.Simple;
        dial.color = new Color(0.16f, 0.13f, 0.11f, 1f);
        dial.raycastTarget = true;

        float gMax = MachineButtonInteraction.MaxFill;
        float gMin = MachineButtonInteraction.SweetMin;
        float gPerfect = MachineButtonInteraction.SweetMax;
        CreateArc(refs.machineButtonRect, "LowZone", new Color(0.95f, 0.66f, 0.26f, 0.95f),
            gMin / gMax, 0f);
        CreateArc(refs.machineButtonRect, "GreenZone", new Color(0.3f, 0.82f, 0.42f, 0.98f),
            (gPerfect - gMin) / gMax, gMin / gMax);
        CreateArc(refs.machineButtonRect, "RedZone", new Color(0.9f, 0.32f, 0.28f, 0.98f),
            (gMax - gPerfect) / gMax, gPerfect / gMax);

        GameObject ringObj = CreateUIObject("DialRing", refs.machineButtonRect, typeof(Image));
        StretchFull(ringObj.GetComponent<RectTransform>());
        Image ring = ringObj.GetComponent<Image>();
        ring.sprite = UIShapeUtility.Ring();
        ring.type = Image.Type.Simple;
        ring.color = new Color(0.86f, 0.86f, 0.9f, 0.9f);
        ring.raycastTarget = false;

        GameObject needleObj = CreateUIObject("Needle", refs.machineButtonRect, typeof(Image));
        refs.machineNeedle = needleObj.GetComponent<RectTransform>();
        refs.machineNeedle.anchorMin = refs.machineNeedle.anchorMax = new Vector2(0.5f, 0.5f);
        refs.machineNeedle.pivot = new Vector2(0.5f, 0f);
        refs.machineNeedle.anchoredPosition = Vector2.zero;
        refs.machineNeedle.sizeDelta = new Vector2(5f, 42f);
        Image needleImg = needleObj.GetComponent<Image>();
        needleImg.color = new Color(0.92f, 0.2f, 0.18f, 1f);
        needleImg.raycastTarget = false;

        GameObject cap = CreateUIObject("Cap", refs.machineButtonRect, typeof(Image));
        RectTransform capRect = cap.GetComponent<RectTransform>();
        capRect.anchorMin = capRect.anchorMax = new Vector2(0.5f, 0.5f); capRect.pivot = new Vector2(0.5f, 0.5f);
        capRect.anchoredPosition = Vector2.zero; capRect.sizeDelta = new Vector2(14f, 14f);
        Image capImg = cap.GetComponent<Image>();
        capImg.sprite = UIShapeUtility.Disc();
        capImg.type = Image.Type.Simple;
        capImg.color = new Color(0.72f, 0.72f, 0.74f, 1f);
        capImg.raycastTarget = false;

        TextMeshProUGUI hint = CreateLabel(refs.machineButtonRect, "Hint", "홀드", 13);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f); hintRect.anchorMax = new Vector2(1f, 0f); hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -2f); hintRect.sizeDelta = new Vector2(90f, 22f);
        hint.raycastTarget = false;

        refs.machineButton = button.GetComponent<MachineButtonInteraction>();
    }

    /// <summary>다이얼 위 부채꼴 구역을 만들어 바늘과 같은 시계방향 기준으로 정렬합니다.</summary>
    static Image CreateArc(RectTransform parent, string name, Color color, float widthFraction, float startFraction)
    {
        GameObject obj = CreateUIObject(name, parent, typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        StretchFull(rect);
        rect.localEulerAngles = new Vector3(0f, 0f, -startFraction * 360f);
        Image img = obj.GetComponent<Image>();
        img.sprite = UIShapeUtility.Disc();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.fillAmount = Mathf.Clamp01(widthFraction);
        img.raycastTarget = false;
        return img;
    }

    static BeverageTool CreateTool(RectTransform parent, BeverageUIRefs refs, BeverageToolKind kind, string label, Color color, Vector2 pos, Vector2 size)
    {
        GameObject obj = CreateUIObject($"Tool_{kind}", parent, typeof(Image), typeof(BeverageTool));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        Image bg = obj.GetComponent<Image>();
        bg.raycastTarget = true;
        CafeSpriteUtility.ApplyStation(bg, kind.ToString(), color);

        TextMeshProUGUI labelText = CreateLabel(rect, "Label", label, 16);
        StretchFull(labelText.rectTransform);
        labelText.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        labelText.raycastTarget = false;

        BeverageTool tool = obj.GetComponent<BeverageTool>();
        tool.Bind(kind, refs.cupCanvas);
        return tool;
    }

    static MaterialSelectorUI CreateSelector(RectTransform parent, IngredientType type, Vector2 pos, Vector2 size)
    {
        GameObject obj = CreateUIObject($"Selector_{type}", parent, typeof(MaterialSelectorUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        TextMeshProUGUI label = CreateLabel(rect, "Label", "<  >", 15);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(36f, 0f); labelRect.offsetMax = new Vector2(-36f, 0f);

        Button left = CreateMiniButton(rect, "Left", "<", new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        Button right = CreateMiniButton(rect, "Right", ">", new Vector2(1f, 0.5f), new Vector2(-2f, 0f));

        MaterialSelectorUI selector = obj.GetComponent<MaterialSelectorUI>();
        selector.Bind(type, label, left, right);
        return selector;
    }

    static Button CreateMiniButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 offset)
    {
        GameObject obj = CreateUIObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
        rect.anchoredPosition = offset; rect.sizeDelta = new Vector2(34f, 40f);
        obj.GetComponent<Image>().color = new Color(0.28f, 0.32f, 0.38f, 1f);
        TextMeshProUGUI labelText = CreateLabel(rect, "Label", label, 18);
        StretchFull(labelText.rectTransform);
        labelText.raycastTarget = false;
        return obj.GetComponent<Button>();
    }

    static Button CreateStackDecor(RectTransform parent, Vector2 pos)
    {
        GameObject obj = CreateUIObject("CupStack", parent, typeof(Image), typeof(Button));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(120f, 200f);
        Image bg = obj.GetComponent<Image>();
        bg.raycastTarget = true;
        CafeSpriteUtility.ApplyStation(bg, "Cup", new Color(0.30f, 0.34f, 0.40f, 1f));

        TextMeshProUGUI label = CreateLabel(rect, "Label", "컵 통\n(눌러서 컵 집기)", 14);
        StretchFull(label.rectTransform);
        label.raycastTarget = false;

        return obj.GetComponent<Button>();
    }

    static RectTransform CreateServeZone(RectTransform parent)
    {
        GameObject obj = CreateUIObject("ServeZone", parent, typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f); rect.anchorMax = new Vector2(1f, 0.5f); rect.pivot = new Vector2(1f, 0.5f);
        PlaceWithMarker(rect, Anchors?.serveZone, CafeLayoutConfig.ServeZonePos, CafeLayoutConfig.ServeZoneSize);
        Image bg = obj.GetComponent<Image>();
        bg.color = new Color(0.22f, 0.38f, 0.30f, 0.5f);
        bg.raycastTarget = false;

        TextMeshProUGUI label = CreateLabel(rect, "Label", "여기로 끌어\n손님에게\n서빙", 18);
        StretchFull(label.rectTransform);
        label.color = new Color(0.88f, 0.96f, 0.9f, 1f);
        label.raycastTarget = false;
        return rect;
    }

    static void CreateMachineSpout(RectTransform parent, Vector2 machinePos)
    {
        GameObject obj = CreateUIObject("MachineSpout", parent, typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(machinePos.x, machinePos.y - 95f);
        rect.sizeDelta = new Vector2(150f, 95f);
        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.22f, 0.16f, 0.13f, 1f);
        img.raycastTarget = false;

        TextMeshProUGUI label = CreateLabel(rect, "Label", "▼", 18);
        StretchFull(label.rectTransform);
        label.color = new Color(0.6f, 0.5f, 0.45f, 1f);
        label.raycastTarget = false;
    }

    static CupCanvasUI CreateCup(RectTransform parent, Vector2 home, out RectTransform cupRoot, out CupDragHandler cupDrag)
    {
        GameObject cupObject = CreateUIObject("CupCanvas", parent, typeof(CupCanvasUI), typeof(CupDragHandler));
        cupRoot = cupObject.GetComponent<RectTransform>();
        cupRoot.anchorMin = cupRoot.anchorMax = new Vector2(0.5f, 0.5f); cupRoot.pivot = new Vector2(0.5f, 0.5f);
        cupRoot.anchoredPosition = home; cupRoot.sizeDelta = SlotSize(Anchors?.cupSize, CafeLayoutConfig.CupSize);

        GameObject bodyObject = CreateUIObject("CupBody", cupRoot, typeof(Image));
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        StretchFull(bodyRect);
        Image bodyImage = bodyObject.GetComponent<Image>();
        bodyImage.raycastTarget = true;
        CafeSpriteUtility.ApplyStation(bodyImage, "Cup", new Color(0.93f, 0.93f, 0.95f, 1f));

        GameObject maskObject = CreateUIObject("LiquidArea", bodyRect, typeof(Image), typeof(RectMask2D));
        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero; maskRect.anchorMax = Vector2.one; maskRect.pivot = new Vector2(0.5f, 0.5f);
        maskRect.offsetMin = new Vector2(18f, 14f); maskRect.offsetMax = new Vector2(-18f, -38f);
        Image maskImage = maskObject.GetComponent<Image>();
        maskImage.color = new Color(0.86f, 0.86f, 0.9f, 1f);
        maskImage.raycastTarget = false;

        Image espressoFill = CreateFill(maskRect, "EspressoFill");
        Image milkFill = CreateFill(maskRect, "MilkFill");

        GameObject decorObject = CreateUIObject("DecorArea", bodyRect);
        StretchFull(decorObject.GetComponent<RectTransform>());

        GameObject lidObject = CreateUIObject("Lid", cupRoot, typeof(Image));
        RectTransform lidRect = lidObject.GetComponent<RectTransform>();
        lidRect.anchorMin = new Vector2(0f, 1f); lidRect.anchorMax = new Vector2(1f, 1f); lidRect.pivot = new Vector2(0.5f, 1f);
        lidRect.offsetMin = new Vector2(-6f, -24f); lidRect.offsetMax = new Vector2(6f, 6f);
        Image lidImage = lidObject.GetComponent<Image>();
        lidImage.color = new Color(0.2f, 0.2f, 0.22f, 1f); lidImage.raycastTarget = false; lidImage.enabled = false;

        TextMeshProUGUI hint = CreateLabel(cupRoot, "EmptyHint", "컵을 머신으로\n끌어 올리세요", 14);
        StretchFull(hint.rectTransform);
        hint.color = new Color(0.4f, 0.4f, 0.45f, 1f); hint.raycastTarget = false;

        CupCanvasUI cupCanvas = cupObject.GetComponent<CupCanvasUI>();
        cupCanvas.Bind(maskRect, decorObject.GetComponent<RectTransform>(), espressoFill, milkFill, lidImage, hint);
        cupDrag = cupObject.GetComponent<CupDragHandler>();
        return cupCanvas;
    }

    static Image CreateFill(RectTransform parent, string name)
    {
        GameObject fillObject = CreateUIObject(name, parent, typeof(Image));
        RectTransform rect = fillObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 0f); rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        Image image = fillObject.GetComponent<Image>();
        image.raycastTarget = false; image.enabled = false;
        return image;
    }

    static Button CreateControlButton(RectTransform parent, string name, string label, Color color, Vector2 pos)
    {
        GameObject buttonObject = CreateUIObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(160f, 52f);
        buttonObject.GetComponent<Image>().color = color;
        TextMeshProUGUI labelText = CreateLabel(rect, "Label", label, 18);
        StretchFull(labelText.rectTransform); labelText.raycastTarget = false;
        return buttonObject.GetComponent<Button>();
    }

    static void ConfigureBottomAnchored(RectTransform rect, float bottomY, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomY); rect.sizeDelta = new Vector2(width, height);
    }

    static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        foreach (System.Type componentType in components)
            if (componentType != typeof(RectTransform) && gameObject.GetComponent(componentType) == null)
                gameObject.AddComponent(componentType);
        return gameObject;
    }

    static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize)
    {
        GameObject labelObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = UIFontUtility.Sanitize(text);
        UIFontUtility.Apply(label);
        label.fontSize = fontSize; label.color = Color.white; label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false; label.enableWordWrapping = true; label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
