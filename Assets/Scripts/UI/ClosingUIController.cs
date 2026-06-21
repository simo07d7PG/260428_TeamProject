using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정산(Closing) 화면을 표시하고 ClosingManager와 연동합니다.
/// 매출/폐기/순이익, 해금 예고, 다음 날 시작 버튼을 제공합니다.
/// </summary>
public class ClosingUIController : MonoBehaviour
{
    RectTransform _overlay;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _bodyText;
    GameState _lastVisibleState = (GameState)(-1);

    public static void ConfigureHostTransform(RectTransform host)
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

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        EnsureManagers();
        BuildPanel();
        UIFontUtility.ApplyToHierarchy(transform);
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;

        if (ClosingManager.Instance != null)
            ClosingManager.Instance.OnSettlementReady += ShowSettlement;

        RefreshVisibility();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;

        if (ClosingManager.Instance != null)
            ClosingManager.Instance.OnSettlementReady -= ShowSettlement;
    }

    void Update()
    {
        RefreshVisibility();
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && ClosingManager.Instance == null)
            ManagerUtility.GetOrAddComponent<ClosingManager>(PreparationManager.Instance.gameObject);
    }

    void BuildPanel()
    {
        GameObject overlayObject = UIFactoryUtility.CreateUIObject("ClosingOverlay", transform, typeof(Image));
        _overlay = overlayObject.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_overlay);
        overlayObject.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.92f);

        GameObject panelObject = UIFactoryUtility.CreateUIObject("SettlementPanel", _overlay, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(540f, 600f);
        panelObject.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 1f);

        _titleText = UIFactoryUtility.CreateLabel(panelRect, "Title", "정산", 26f);
        _titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = _titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -64f);
        titleRect.offsetMax = new Vector2(-24f, -16f);

        _bodyText = UIFactoryUtility.CreateLabel(panelRect, "Body", string.Empty, 18f);
        _bodyText.alignment = TextAlignmentOptions.TopLeft;
        _bodyText.enableWordWrapping = true;
        RectTransform bodyRect = _bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(28f, 84f);
        bodyRect.offsetMax = new Vector2(-28f, -72f);

        Button nextButton = UIFactoryUtility.CreateButton(
            panelRect, "NextDayButton", "다음 날 시작", new Color(0.30f, 0.62f, 0.36f, 1f));
        RectTransform buttonRect = nextButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 20f);
        buttonRect.sizeDelta = new Vector2(260f, 52f);
        nextButton.onClick.AddListener(OnNextDayClicked);
    }

    void OnNextDayClicked()
    {
        ClosingManager.Instance?.AdvanceToNextDay();
    }

    void HandleStateChanged(GameState state)
    {
        RefreshVisibility();

        if (state == GameState.Closing)
            ShowSettlement(ClosingManager.Instance?.LastSettlement);
    }

    void ShowSettlement(DailySettlement settlement)
    {
        if (settlement == null || _bodyText == null)
            return;

        if (_titleText != null)
        {
            string grade = string.IsNullOrEmpty(settlement.Grade) ? string.Empty : $" - {settlement.Grade}";
            _titleText.text = UIFontUtility.Sanitize($"Day {settlement.Day} 정산{grade}");
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"총 매출:        {settlement.Revenue} Coin");
        builder.AppendLine($"쓰레기 폐기:    -{settlement.GarbageCost} Coin");
        builder.AppendLine($"재료 폐기:      -{settlement.LeftoverCost} Coin");
        builder.AppendLine($"───────────────");
        builder.AppendLine($"순이익:         {settlement.NetProfit} Coin");
        builder.AppendLine($"보유 코인:      {settlement.CoinAfter} Coin");
        builder.AppendLine();
        builder.AppendLine($"서빙 완료: {settlement.ServedCount}명 / 떠난 손님: {settlement.LeftCount}명");
        builder.AppendLine($"머지 성공: {settlement.MergeCount}회 (고급 재료 확보)");
        builder.AppendLine();

        if (settlement.UpcomingUnlocks.Count > 0)
            builder.AppendLine($"내일 해금: {string.Join(", ", settlement.UpcomingUnlocks)}");

        if (!string.IsNullOrEmpty(settlement.UpcomingTrendName))
            builder.AppendLine($"내일 트렌드 예고: {settlement.UpcomingTrendName}");

        _bodyText.text = UIFontUtility.Sanitize(builder.ToString());
    }

    void RefreshVisibility()
    {
        if (GameManager.Instance == null || _overlay == null)
            return;

        GameState state = GameManager.Instance.CurrentState;
        if (_lastVisibleState == state)
            return;

        _lastVisibleState = state;
        _overlay.gameObject.SetActive(state == GameState.Closing);
    }
}
