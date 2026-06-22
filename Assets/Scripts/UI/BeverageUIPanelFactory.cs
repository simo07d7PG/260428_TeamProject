using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사진 기준 카페 카운터 레이아웃을 런타임에 구성합니다.
/// 컵 통 / 커피 머신(슬롯+버튼+원형 게이지) / 우유·얼음·토핑·시럽 도구 / 각 칸 아래 재료 변경 / 큰 주문 배너.
/// 스테이션·컵 배경은 Resources/Sprites/...에서 로드하며 없으면 색으로 표시됩니다.
/// </summary>
public static class BeverageUIPanelFactory
{
    const string PanelName = "BeveragePanel";

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
            Object.Destroy(existing.gameObject);

        return CreatePanel(hostRoot);
    }

    static BeverageUIRefs CreatePanel(Transform hostRoot)
    {
        GameObject panelObject = CreateUIObject(PanelName, hostRoot, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        StretchFull(panelRect);
        Image panelImage = panelObject.GetComponent<Image>();
        // 인스펙터로 지정한 영업 배경이 있으면 전체화면 배경으로, 없으면 카운터 스프라이트/색 폴백.
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
            cupStackHome = new Vector2(-600f, 150f),
            cupHeldHome = new Vector2(0f, -210f),
            cupMachinePos = new Vector2(-400f, -74f)
        };

        BuildOrderBanner(panelRect, ref refs);

        // 서빙 존 (그림 기준 오른쪽 끝 빈 공간) — 완성한 컵을 여기로 끌어 손님에게 전달
        refs.serveZone = CreateServeZone(panelRect);

        // 컵 통(누르면 컵을 집음) + 컵
        refs.cupStackButton = CreateStackDecor(panelRect, refs.cupStackHome);
        refs.cupCanvas = CreateCup(panelRect, refs.cupStackHome, out refs.cupRoot, out refs.cupDrag);

        // 커피 머신 (슬롯 + 버튼/다이얼 게이지). 다른 스테이션보다 크고 높게.
        Vector2 machinePos = new Vector2(-400f, 88f);
        BuildMachine(panelRect, ref refs, machinePos);

        // 재료 도구 (드래그해서 컵에) — 머신보다 살짝 낮은 줄
        refs.milkTool = CreateTool(panelRect, refs, BeverageToolKind.Milk, "우유", new Color(0.85f, 0.88f, 0.92f, 1f), new Vector2(-180f, 66f));
        refs.iceTool = CreateTool(panelRect, refs, BeverageToolKind.Ice, "얼음", new Color(0.62f, 0.80f, 0.92f, 1f), new Vector2(40f, 66f));
        refs.toppingTool = CreateTool(panelRect, refs, BeverageToolKind.Topping, "토핑", new Color(0.98f, 0.78f, 0.83f, 1f), new Vector2(260f, 66f));
        refs.syrupTool = CreateTool(panelRect, refs, BeverageToolKind.Syrup, "시럽", new Color(0.85f, 0.62f, 0.20f, 1f), new Vector2(480f, 66f));

        // 재료 변경 (각 칸 아래). 원두 변경은 머신 아래(컵 도킹 위치)와 겹치지 않게 컵통 열 아래로.
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Base, new Vector2(-600f, -48f)));
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Milk, new Vector2(-180f, -48f)));
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Topping, new Vector2(260f, -48f)));
        refs.selectors.Add(CreateSelector(panelRect, IngredientType.Syrup, new Vector2(480f, -48f)));

        // 컵을 도구 위로 올려 가리지 않게
        refs.cupRoot.SetAsLastSibling();

        // 머신 스파웃(전면 토출구) — 컵보다 앞에 그려서 컵이 '머신 아래'로 들어가 보이게
        CreateMachineSpout(panelRect, machinePos);

        // 와이어링
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
        // 상단바(56) + 손님 대기열(가로) 아래에 배치(겹침 방지)
        banner.anchoredPosition = new Vector2(0f, -212f);
        banner.sizeDelta = new Vector2(700f, 118f);
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
        bodyRect.anchoredPosition = pos; bodyRect.sizeDelta = new Vector2(200f, 210f);
        Image bodyImg = body.GetComponent<Image>();
        bodyImg.raycastTarget = false;
        CafeSpriteUtility.ApplyStation(bodyImg, "EspressoShot", new Color(0.30f, 0.20f, 0.16f, 1f));

        refs.machineLabel = CreateLabel(bodyRect, "MachineLabel", "커피 머신", 15);
        RectTransform mlRect = refs.machineLabel.rectTransform;
        mlRect.anchorMin = new Vector2(0f, 1f); mlRect.anchorMax = new Vector2(1f, 1f); mlRect.pivot = new Vector2(0.5f, 1f);
        mlRect.offsetMin = new Vector2(4f, -40f); mlRect.offsetMax = new Vector2(-4f, -6f);
        refs.machineLabel.raycastTarget = false;

        // 컵 슬롯(히트테스트용, 머신 하단 — 컵을 여기로 끌어 도킹)
        GameObject slot = CreateUIObject("CupSlot", parent);
        refs.machineSlot = slot.GetComponent<RectTransform>();
        refs.machineSlot.anchorMin = refs.machineSlot.anchorMax = new Vector2(0.5f, 0.5f); refs.machineSlot.pivot = new Vector2(0.5f, 0.5f);
        refs.machineSlot.anchoredPosition = new Vector2(pos.x, pos.y - 170f);
        refs.machineSlot.sizeDelta = new Vector2(190f, 200f);

        // 버튼(다이얼) — 머신 면 상단. 홀드하면 바늘이 시계방향으로 돕니다.
        // 위치는 CafeAssetConfig.GaugeOffset(인스펙터)으로 직접 조절할 수 있습니다.
        Vector2 gaugeOffset = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GaugeOffset : new Vector2(0f, 30f);
        GameObject button = CreateUIObject("MachineButton", parent, typeof(Image), typeof(MachineButtonInteraction));
        refs.machineButtonRect = button.GetComponent<RectTransform>();
        refs.machineButtonRect.anchorMin = refs.machineButtonRect.anchorMax = new Vector2(0.5f, 0.5f); refs.machineButtonRect.pivot = new Vector2(0.5f, 0.5f);
        refs.machineButtonRect.anchoredPosition = new Vector2(pos.x + gaugeOffset.x, pos.y + gaugeOffset.y);
        refs.machineButtonRect.sizeDelta = new Vector2(96f, 96f);
        Image dial = button.GetComponent<Image>();
        dial.sprite = UIShapeUtility.Disc(); // 진짜 둥근 다이얼
        dial.type = Image.Type.Simple;
        dial.color = new Color(0.16f, 0.13f, 0.11f, 1f);
        dial.raycastTarget = true;

        // 3색 부채꼴이 다이얼 '전체'를 빈틈없이 채우도록 채움 범위(0~MaxFill)를 한 바퀴(360도)에 매핑.
        // 바늘도 동일하게 fill/MaxFill로 회전하므로 구역과 정확히 일치합니다. 색은 주황→초록→빨강 3가지뿐.
        float gMax = MachineButtonInteraction.MaxFill;
        float gMin = MachineButtonInteraction.SweetMin;
        float gPerfect = MachineButtonInteraction.SweetMax;
        CreateArc(refs.machineButtonRect, "LowZone", new Color(0.95f, 0.66f, 0.26f, 0.95f),
            gMin / gMax, 0f);                                   // 부족(주황) [0~SweetMin]
        CreateArc(refs.machineButtonRect, "GreenZone", new Color(0.3f, 0.82f, 0.42f, 0.98f),
            (gPerfect - gMin) / gMax, gMin / gMax);             // 안전(초록) [SweetMin~SweetMax]
        CreateArc(refs.machineButtonRect, "RedZone", new Color(0.9f, 0.32f, 0.28f, 0.98f),
            (gMax - gPerfect) / gMax, gPerfect / gMax);         // 과추출(빨강) [SweetMax~MaxFill]

        // 다이얼 외곽 링
        GameObject ringObj = CreateUIObject("DialRing", refs.machineButtonRect, typeof(Image));
        StretchFull(ringObj.GetComponent<RectTransform>());
        Image ring = ringObj.GetComponent<Image>();
        ring.sprite = UIShapeUtility.Ring();
        ring.type = Image.Type.Simple;
        ring.color = new Color(0.86f, 0.86f, 0.9f, 0.9f);
        ring.raycastTarget = false;

        // 바늘 (중심에서 위로, 회전)
        GameObject needleObj = CreateUIObject("Needle", refs.machineButtonRect, typeof(Image));
        refs.machineNeedle = needleObj.GetComponent<RectTransform>();
        refs.machineNeedle.anchorMin = refs.machineNeedle.anchorMax = new Vector2(0.5f, 0.5f);
        refs.machineNeedle.pivot = new Vector2(0.5f, 0f);
        refs.machineNeedle.anchoredPosition = Vector2.zero;
        refs.machineNeedle.sizeDelta = new Vector2(5f, 42f);
        Image needleImg = needleObj.GetComponent<Image>();
        needleImg.color = new Color(0.92f, 0.2f, 0.18f, 1f);
        needleImg.raycastTarget = false;

        // 중심 캡
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

    /// <summary>
    /// 다이얼 위 부채꼴 구역. widthFraction = 구역 폭(바퀴 비율), startFraction = 시작 위치(바퀴 비율).
    /// 시작 위치는 바늘과 같은 시계방향 기준으로 회전해 정렬합니다.
    /// </summary>
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

    static BeverageTool CreateTool(RectTransform parent, BeverageUIRefs refs, BeverageToolKind kind, string label, Color color, Vector2 pos)
    {
        GameObject obj = CreateUIObject($"Tool_{kind}", parent, typeof(Image), typeof(BeverageTool));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(116f, 130f);

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

    static MaterialSelectorUI CreateSelector(RectTransform parent, IngredientType type, Vector2 pos)
    {
        GameObject obj = CreateUIObject($"Selector_{type}", parent, typeof(MaterialSelectorUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(190f, 64f);

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
        bg.raycastTarget = true; // 눌러서 컵을 집을 수 있게
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
        rect.anchoredPosition = new Vector2(-36f, -40f);
        rect.sizeDelta = new Vector2(180f, 320f);
        Image bg = obj.GetComponent<Image>();
        bg.color = new Color(0.22f, 0.38f, 0.30f, 0.5f);
        bg.raycastTarget = false; // 사각 영역 히트테스트는 CupDragHandler가 처리

        TextMeshProUGUI label = CreateLabel(rect, "Label", "여기로 끌어\n손님에게\n서빙", 18);
        StretchFull(label.rectTransform);
        label.color = new Color(0.88f, 0.96f, 0.9f, 1f);
        label.raycastTarget = false;
        return rect;
    }

    // 머신 전면 토출구. 컵보다 앞(나중 형제)에 그려져 도킹한 컵의 윗부분을 가려 '머신 아래' 느낌을 줍니다.
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
        cupRoot.anchoredPosition = home; cupRoot.sizeDelta = new Vector2(150f, 205f);

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
