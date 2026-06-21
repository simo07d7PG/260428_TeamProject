using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GPGP식 음료 제작 패널을 런타임에 구성합니다. (SupplyUIPanelFactory 패턴)
/// 컵 캔버스 + 스테이션 도구 + 제어 버튼을 만들고 상호작용 컴포넌트를 연결합니다.
/// </summary>
public static class BeverageUIPanelFactory
{
    const string PanelName = "BeveragePanel";
    const float StationCell = 118f;
    const float StationCellHeight = 116f;

    public struct BeverageUIRefs
    {
        public RectTransform panelRoot;
        public TextMeshProUGUI orderNoteText;
        public TextMeshProUGUI menuEstimateText;
        public TextMeshProUGUI statusText;

        public CupCanvasUI cupCanvas;
        public ToppingPlaceInteraction toppingInteraction;

        public BeverageStationUI shotStation;
        public ShotPullInteraction shotInteraction;
        public BeverageStationUI milkStation;
        public PourMilkInteraction milkInteraction;
        public BeverageStationUI syrupStation;
        public SyrupTapInteraction syrupInteraction;

        public Button toppingButton;
        public Button iceButton;
        public Button lidButton;

        public Button newCupButton;
        public Button completeButton;
        public Button clearButton;
        public Button serveButton;
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
        panelObject.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f, 0.97f);

        BeverageUIRefs refs = new BeverageUIRefs { panelRoot = panelRect };

        // --- 상단: 주문 노트 / 예상 메뉴 ---
        refs.orderNoteText = CreateLabel(panelRect, "OrderNoteText", "주문 대기 중", 22);
        ConfigureTop(refs.orderNoteText.rectTransform, -108f, 56f);
        refs.orderNoteText.fontStyle = FontStyles.Bold;
        refs.orderNoteText.alignment = TextAlignmentOptions.Center;

        refs.menuEstimateText = CreateLabel(panelRect, "MenuEstimateText", "예상: -", 18);
        ConfigureTop(refs.menuEstimateText.rectTransform, -166f, 34f);
        refs.menuEstimateText.alignment = TextAlignmentOptions.Center;
        refs.menuEstimateText.color = new Color(0.75f, 0.86f, 1f, 1f);

        // --- 중앙: 컵 캔버스 ---
        refs.cupCanvas = CreateCup(panelRect, out refs.toppingInteraction);

        // --- 상태 텍스트 ---
        refs.statusText = CreateLabel(panelRect, "StatusText", string.Empty, 16);
        ConfigureBottomAnchored(refs.statusText.rectTransform, 318f, 700f, 40f);
        refs.statusText.alignment = TextAlignmentOptions.Center;
        refs.statusText.color = new Color(0.95f, 0.9f, 0.65f, 1f);

        // --- 스테이션 도구 행 ---
        RectTransform stationRow = CreateRow(panelRect, "StationRow", 150f, 780f, StationCellHeight + 6f);

        refs.shotStation = CreateStationTool(stationRow, StationType.EspressoShot, "에스프레소\n(홀드)", true);
        refs.shotInteraction = refs.shotStation.gameObject.AddComponent<ShotPullInteraction>();
        refs.shotInteraction.Bind(refs.shotStation);

        refs.milkStation = CreateStationTool(stationRow, StationType.SteamMilk, "밀크\n(홀드)", true);
        refs.milkInteraction = refs.milkStation.gameObject.AddComponent<PourMilkInteraction>();
        refs.milkInteraction.Bind(refs.milkStation);

        refs.syrupStation = CreateStationTool(stationRow, StationType.Syrup, "시럽\n(탭)", false);
        refs.syrupInteraction = refs.syrupStation.gameObject.AddComponent<SyrupTapInteraction>();

        refs.toppingButton = CreateStationButton(stationRow, "ToppingButton", "토핑\n(컵 탭)");
        refs.iceButton = CreateStationButton(stationRow, "IceButton", "얼음");
        refs.lidButton = CreateStationButton(stationRow, "LidButton", "리드");

        refs.toppingInteraction.Bind(refs.cupCanvas);

        // --- 제어 버튼 행 ---
        RectTransform controlRow = CreateRow(panelRect, "ControlRow", 36f, 620f, 56f);
        refs.newCupButton = CreateControlButton(controlRow, "NewCupButton", "새 컵", new Color(0.25f, 0.5f, 0.78f, 1f));
        refs.completeButton = CreateControlButton(controlRow, "CompleteButton", "완성", new Color(0.30f, 0.62f, 0.36f, 1f));
        refs.serveButton = CreateControlButton(controlRow, "ServeButton", "서빙", new Color(0.78f, 0.55f, 0.22f, 1f));
        refs.clearButton = CreateControlButton(controlRow, "ClearButton", "비우기", new Color(0.5f, 0.3f, 0.3f, 1f));

        return refs;
    }

    static CupCanvasUI CreateCup(RectTransform parent, out ToppingPlaceInteraction toppingInteraction)
    {
        GameObject cupObject = CreateUIObject("CupCanvas", parent, typeof(CupCanvasUI));
        RectTransform cupRect = cupObject.GetComponent<RectTransform>();
        cupRect.anchorMin = new Vector2(0.5f, 0.5f);
        cupRect.anchorMax = new Vector2(0.5f, 0.5f);
        cupRect.pivot = new Vector2(0.5f, 0.5f);
        cupRect.anchoredPosition = new Vector2(0f, 40f);
        cupRect.sizeDelta = new Vector2(240f, 330f);

        // 컵 몸체 (토핑 탭 레이캐스트 타겟)
        GameObject bodyObject = CreateUIObject("CupBody", cupRect, typeof(Image), typeof(ToppingPlaceInteraction));
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        StretchFull(bodyRect);
        Image bodyImage = bodyObject.GetComponent<Image>();
        bodyImage.color = new Color(0.93f, 0.93f, 0.95f, 1f);
        bodyImage.raycastTarget = true;
        toppingInteraction = bodyObject.GetComponent<ToppingPlaceInteraction>();

        // 액체 영역 (마스크)
        GameObject maskObject = CreateUIObject("LiquidArea", bodyRect, typeof(Image), typeof(RectMask2D));
        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.pivot = new Vector2(0.5f, 0.5f);
        maskRect.offsetMin = new Vector2(20f, 16f);
        maskRect.offsetMax = new Vector2(-20f, -44f);
        Image maskImage = maskObject.GetComponent<Image>();
        maskImage.color = new Color(0.86f, 0.86f, 0.9f, 1f);
        maskImage.raycastTarget = false;

        Image espressoFill = CreateFill(maskRect, "EspressoFill");
        Image milkFill = CreateFill(maskRect, "MilkFill");

        // 데코 영역 (방울/토핑) — 컵 전체 기준
        GameObject decorObject = CreateUIObject("DecorArea", bodyRect);
        RectTransform decorRect = decorObject.GetComponent<RectTransform>();
        StretchFull(decorRect);

        // 리드
        GameObject lidObject = CreateUIObject("Lid", cupRect, typeof(Image));
        RectTransform lidRect = lidObject.GetComponent<RectTransform>();
        lidRect.anchorMin = new Vector2(0f, 1f);
        lidRect.anchorMax = new Vector2(1f, 1f);
        lidRect.pivot = new Vector2(0.5f, 1f);
        lidRect.offsetMin = new Vector2(-8f, -28f);
        lidRect.offsetMax = new Vector2(8f, 6f);
        Image lidImage = lidObject.GetComponent<Image>();
        lidImage.color = new Color(0.2f, 0.2f, 0.22f, 1f);
        lidImage.raycastTarget = false;
        lidImage.enabled = false;

        TextMeshProUGUI hint = CreateLabel(cupRect, "EmptyHint", "컵을 채워보세요", 15);
        StretchFull(hint.rectTransform);
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.4f, 0.4f, 0.45f, 1f);
        hint.raycastTarget = false;

        CupCanvasUI cupCanvas = cupObject.GetComponent<CupCanvasUI>();
        cupCanvas.Bind(maskRect, decorRect, espressoFill, milkFill, lidImage, hint);
        return cupCanvas;
    }

    static Image CreateFill(RectTransform parent, string name)
    {
        GameObject fillObject = CreateUIObject(name, parent, typeof(Image));
        RectTransform rect = fillObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;

        Image image = fillObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    static BeverageStationUI CreateStationTool(RectTransform row, StationType station, string label, bool addGauge)
    {
        GameObject toolObject = CreateUIObject(
            $"Station_{station}", row, typeof(Image), typeof(LayoutElement), typeof(BeverageStationUI));
        RectTransform rect = toolObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(StationCell, StationCellHeight);

        Image background = toolObject.GetComponent<Image>();
        background.color = new Color(0.22f, 0.26f, 0.32f, 1f);
        background.raycastTarget = true;

        LayoutElement layout = toolObject.GetComponent<LayoutElement>();
        layout.preferredWidth = StationCell;
        layout.preferredHeight = StationCellHeight;
        layout.flexibleWidth = 0f;

        Image gauge = null;
        if (addGauge)
        {
            GameObject gaugeObject = CreateUIObject("Gauge", rect, typeof(Image));
            RectTransform gaugeRect = gaugeObject.GetComponent<RectTransform>();
            StretchFull(gaugeRect);
            gauge = gaugeObject.GetComponent<Image>();
            gauge.color = new Color(0.30f, 0.55f, 0.85f, 0.45f);
            gauge.type = Image.Type.Filled;
            gauge.fillMethod = Image.FillMethod.Vertical;
            gauge.fillOrigin = (int)Image.OriginVertical.Bottom;
            gauge.fillAmount = 0f;
            gauge.raycastTarget = false;
        }

        TextMeshProUGUI labelText = CreateLabel(rect, "Label", label, 15);
        StretchFull(labelText.rectTransform);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        BeverageStationUI stationUI = toolObject.GetComponent<BeverageStationUI>();
        stationUI.Bind(station, background, gauge, labelText);
        return stationUI;
    }

    static Button CreateStationButton(RectTransform row, string name, string label)
    {
        GameObject buttonObject = CreateUIObject(name, row, typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(StationCell, StationCellHeight);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.24f, 0.28f, 0.34f, 1f);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = StationCell;
        layout.preferredHeight = StationCellHeight;
        layout.flexibleWidth = 0f;

        TextMeshProUGUI labelText = CreateLabel(rect, "Label", label, 15);
        StretchFull(labelText.rectTransform);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    static Button CreateControlButton(RectTransform row, string name, string label, Color color)
    {
        GameObject buttonObject = CreateUIObject(name, row, typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140f, 52f);

        buttonObject.GetComponent<Image>().color = color;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 140f;
        layout.preferredHeight = 52f;
        layout.flexibleWidth = 0f;

        TextMeshProUGUI labelText = CreateLabel(rect, "Label", label, 18);
        StretchFull(labelText.rectTransform);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    static RectTransform CreateRow(RectTransform parent, string name, float bottomY, float width, float height)
    {
        GameObject rowObject = CreateUIObject(name, parent, typeof(HorizontalLayoutGroup));
        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomY);
        rect.sizeDelta = new Vector2(width, height);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rect;
    }

    static void ConfigureTop(RectTransform rect, float topY, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, topY);
        rect.sizeDelta = new Vector2(720f, height);
    }

    static void ConfigureBottomAnchored(RectTransform rect, float bottomY, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomY);
        rect.sizeDelta = new Vector2(width, height);
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

    static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize)
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

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
