using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정산(Closing) 화면을 표시하고 ClosingManager와 연동합니다.
/// 매출/폐기/순이익, 해금 예고, 다음 날 시작 버튼을 제공합니다.
/// 숫자 카운트업·등급 스탬프·순차 페이드 등 '하루 결산' 연출을 코루틴으로 처리합니다.
/// </summary>
public class ClosingUIController : MonoBehaviour
{
    // 연출 타이밍 상수.
    const float CountUpDuration = 0.6f;   // 카운트업 지속 시간(초).
    const float RowFadeDuration = 0.28f;  // 행 1개 페이드 인 시간(초).
    const float RowStagger = 0.10f;       // 행 간 등장 딜레이(초).
    const float StampPunchDuration = 0.35f;

    /// <summary>카운트업으로 증가시킬 수치 행 1개를 표현합니다.</summary>
    class SettlementRow
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public TextMeshProUGUI ValueLabel;
        public string Prefix = string.Empty; // 부호/접두(예: "-").
        public string Suffix = " Coin";
        public int TargetValue;
    }

    RectTransform _overlay;
    RectTransform _panel;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _gradeStamp;
    RectTransform _gradeStampRect;

    // 정산 수치 행(카운트업 대상).
    SettlementRow _revenueRow;
    SettlementRow _garbageRow;
    SettlementRow _leftoverRow;
    SettlementRow _netProfitRow;
    SettlementRow _coinRow;

    TextMeshProUGUI _servedText;
    Image _servedBarFill;
    TextMeshProUGUI _starsText;
    TextMeshProUGUI _mergeText;
    TextMeshProUGUI _footerText;

    readonly List<CanvasGroup> _sequentialGroups = new();

    Coroutine _presentRoutine;
    GameState _lastVisibleState = (GameState)(-1);

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

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

        StopPresentRoutine();
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
        _panel = panelObject.GetComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.anchoredPosition = Vector2.zero;
        _panel.sizeDelta = new Vector2(540f, 640f);
        panelObject.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 1f);

        _titleText = UIFactoryUtility.CreateLabel(_panel, "Title", "정산", 26f);
        _titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = _titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -56f);
        titleRect.offsetMax = new Vector2(-24f, -14f);

        // 등급 스탬프: 패널 우상단에 '쾅' 찍히듯 등장.
        _gradeStamp = UIFactoryUtility.CreateLabel(_panel, "GradeStamp", string.Empty, 30f);
        _gradeStamp.fontStyle = FontStyles.Bold;
        _gradeStamp.color = new Color(1f, 0.84f, 0.32f, 1f);
        _gradeStampRect = _gradeStamp.rectTransform;
        _gradeStampRect.anchorMin = new Vector2(0.5f, 1f);
        _gradeStampRect.anchorMax = new Vector2(0.5f, 1f);
        _gradeStampRect.pivot = new Vector2(0.5f, 1f);
        _gradeStampRect.anchoredPosition = new Vector2(0f, -64f);
        _gradeStampRect.sizeDelta = new Vector2(480f, 48f);

        // 수치 행들을 위에서 아래로 쌓는다.
        float y = -128f;
        const float rowStep = 40f;

        _revenueRow = BuildRow("Revenue", "총 매출", y, new Color(0.92f, 0.94f, 0.98f));
        y -= rowStep;
        _garbageRow = BuildRow("Garbage", "쓰레기 폐기", y, new Color(0.90f, 0.55f, 0.50f));
        _garbageRow.Prefix = "-";
        y -= rowStep;
        _leftoverRow = BuildRow("Leftover", "재료 폐기", y, new Color(0.90f, 0.55f, 0.50f));
        _leftoverRow.Prefix = "-";
        y -= rowStep + 8f;

        BuildDivider(y + 22f);

        _netProfitRow = BuildRow("NetProfit", "순이익", y, new Color(0.55f, 0.92f, 0.62f));
        _netProfitRow.ValueLabel.fontStyle = FontStyles.Bold;
        _netProfitRow.ValueLabel.fontSize = 22f;
        y -= rowStep;
        _coinRow = BuildRow("Coin", "보유 코인", y, new Color(1f, 0.86f, 0.36f));
        y -= rowStep + 14f;

        // 서빙 성공: 텍스트 + 막대 + 별점.
        _servedText = BuildLeftLabel("ServedText", string.Empty, y, 17f, new Color(0.88f, 0.9f, 0.95f));
        RegisterSequential(_servedText);
        y -= 28f;

        Image servedBackground = UIFactoryUtility.CreateFilledBar(
            _panel, "ServedBar",
            new Color(0.20f, 0.22f, 0.27f, 1f),
            new Color(0.42f, 0.74f, 0.96f, 1f));
        _servedBarFill = servedBackground;
        RectTransform barBackRect = servedBackground.rectTransform.parent as RectTransform;
        barBackRect.anchorMin = new Vector2(0f, 1f);
        barBackRect.anchorMax = new Vector2(1f, 1f);
        barBackRect.pivot = new Vector2(0.5f, 1f);
        barBackRect.offsetMin = new Vector2(28f, 0f);
        barBackRect.offsetMax = new Vector2(-28f, 0f);
        barBackRect.anchoredPosition = new Vector2(0f, y);
        barBackRect.sizeDelta = new Vector2(barBackRect.sizeDelta.x, 14f);
        CanvasGroup barGroup = barBackRect.gameObject.AddComponent<CanvasGroup>();
        _sequentialGroups.Add(barGroup);
        y -= 30f;

        _starsText = BuildLeftLabel("Stars", string.Empty, y, 22f, new Color(1f, 0.84f, 0.32f));
        RegisterSequential(_starsText);
        y -= 32f;

        _mergeText = BuildLeftLabel("Merge", string.Empty, y, 16f, new Color(0.85f, 0.88f, 0.94f));
        RegisterSequential(_mergeText);
        y -= 30f;

        _footerText = BuildLeftLabel("Footer", string.Empty, y, 15f, new Color(0.78f, 0.82f, 0.9f));
        _footerText.enableWordWrapping = true;
        RectTransform footerRect = _footerText.rectTransform;
        footerRect.sizeDelta = new Vector2(footerRect.sizeDelta.x, 64f);
        RegisterSequential(_footerText);

        Button nextButton = UIFactoryUtility.CreateButton(
            _panel, "NextDayButton", "다음 날 시작", new Color(0.30f, 0.62f, 0.36f, 1f));
        RectTransform buttonRect = nextButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 20f);
        buttonRect.sizeDelta = new Vector2(260f, 52f);
        nextButton.onClick.AddListener(OnNextDayClicked);
    }

    /// <summary>라벨(좌)·값(우)로 구성된 수치 행을 생성합니다. CanvasGroup으로 페이드 제어.</summary>
    SettlementRow BuildRow(string name, string caption, float y, Color valueColor)
    {
        GameObject rowObject = UIFactoryUtility.CreateUIObject($"Row_{name}", _panel, typeof(CanvasGroup));
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.offsetMin = new Vector2(28f, 0f);
        rowRect.offsetMax = new Vector2(-28f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, 34f);

        CanvasGroup group = rowObject.GetComponent<CanvasGroup>();

        TextMeshProUGUI captionLabel = UIFactoryUtility.CreateLabel(rowRect, "Caption", caption, 18f);
        captionLabel.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform captionRect = captionLabel.rectTransform;
        captionRect.anchorMin = new Vector2(0f, 0f);
        captionRect.anchorMax = new Vector2(0.55f, 1f);
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;

        TextMeshProUGUI valueLabel = UIFactoryUtility.CreateLabel(rowRect, "Value", "0", 20f);
        valueLabel.alignment = TextAlignmentOptions.MidlineRight;
        valueLabel.color = valueColor;
        RectTransform valueRect = valueLabel.rectTransform;
        valueRect.anchorMin = new Vector2(0.45f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;

        _sequentialGroups.Add(group);

        return new SettlementRow
        {
            Root = rowRect,
            Group = group,
            ValueLabel = valueLabel
        };
    }

    /// <summary>왼쪽 정렬 단일 라벨을 생성합니다(서빙/별/머지/푸터용). CanvasGroup 부착.</summary>
    TextMeshProUGUI BuildLeftLabel(string name, string text, float y, float fontSize, Color color)
    {
        GameObject host = UIFactoryUtility.CreateUIObject($"Line_{name}", _panel, typeof(CanvasGroup));
        RectTransform hostRect = host.GetComponent<RectTransform>();
        hostRect.anchorMin = new Vector2(0f, 1f);
        hostRect.anchorMax = new Vector2(1f, 1f);
        hostRect.pivot = new Vector2(0.5f, 1f);
        hostRect.offsetMin = new Vector2(28f, 0f);
        hostRect.offsetMax = new Vector2(-28f, 0f);
        hostRect.anchoredPosition = new Vector2(0f, y);
        hostRect.sizeDelta = new Vector2(hostRect.sizeDelta.x, 26f);

        TextMeshProUGUI label = UIFactoryUtility.CreateLabel(hostRect, name, text, fontSize);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = color;
        UIFactoryUtility.StretchFull(label.rectTransform);
        return label;
    }

    void RegisterSequential(TextMeshProUGUI label)
    {
        CanvasGroup group = label.rectTransform.parent.GetComponent<CanvasGroup>();
        if (group != null)
            _sequentialGroups.Add(group);
    }

    void BuildDivider(float y)
    {
        Image divider = UIFactoryUtility.CreateImage(_panel, "Divider", new Color(0.4f, 0.43f, 0.5f, 0.6f));
        RectTransform rect = divider.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(28f, 0f);
        rect.offsetMax = new Vector2(-28f, 0f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 2f);
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
        if (settlement == null || _panel == null)
            return;

        if (_titleText != null)
            _titleText.text = UIFontUtility.Sanitize($"Day {settlement.Day} 정산");

        // 카운트업 목표값 설정.
        _revenueRow.TargetValue = settlement.Revenue;
        _garbageRow.TargetValue = settlement.GarbageCost;
        _leftoverRow.TargetValue = settlement.LeftoverCost;
        _netProfitRow.TargetValue = settlement.NetProfit;
        _coinRow.TargetValue = settlement.CoinAfter;

        // 서빙 통계.
        int target = CustomerManager.Instance != null ? CustomerManager.Instance.CustomersPerDay : 0;
        int totalCustomers = target > 0 ? target : settlement.ServedCount + settlement.LeftCount;
        float servedRatio = totalCustomers > 0
            ? Mathf.Clamp01(settlement.ServedCount / (float)totalCustomers)
            : 0f;

        if (_servedText != null)
            _servedText.text = UIFontUtility.Sanitize(
                $"서빙 완료 {settlement.ServedCount}명 / 떠난 손님 {settlement.LeftCount}명");

        if (_mergeText != null)
            _mergeText.text = UIFontUtility.Sanitize(
                $"머지 성공 {settlement.MergeCount}회 (고급 재료 확보)");

        if (_starsText != null)
            _starsText.text = BuildStars(servedRatio);

        if (_footerText != null)
            _footerText.text = UIFontUtility.Sanitize(BuildFooter(settlement));

        // 재진입 안전: 진행 중 연출 정리 후 다시 시작.
        StopPresentRoutine();
        _presentRoutine = StartCoroutine(PresentRoutine(settlement, servedRatio));
    }

    static string BuildStars(float ratio)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt(ratio * 5f), 0, 5);
        StringBuilder builder = new StringBuilder(5);
        for (int i = 0; i < 5; i++)
            builder.Append(i < filled ? '★' : '☆');
        return builder.ToString();
    }

    static string BuildFooter(DailySettlement settlement)
    {
        StringBuilder builder = new StringBuilder();

        if (settlement.UpcomingUnlocks.Count > 0)
            builder.AppendLine($"내일 해금: {string.Join(", ", settlement.UpcomingUnlocks)}");

        if (!string.IsNullOrEmpty(settlement.UpcomingTrendName))
            builder.Append($"내일 트렌드 예고: {settlement.UpcomingTrendName}");

        return builder.ToString().TrimEnd();
    }

    IEnumerator PresentRoutine(DailySettlement settlement, float servedRatio)
    {
        // 초기화: 모든 순차 그룹을 숨기고, 값/스탬프 리셋.
        foreach (CanvasGroup group in _sequentialGroups)
        {
            if (group != null)
                group.alpha = 0f;
        }

        SetRowValue(_revenueRow, 0);
        SetRowValue(_garbageRow, 0);
        SetRowValue(_leftoverRow, 0);
        SetRowValue(_netProfitRow, 0);
        SetRowValue(_coinRow, 0);

        if (_servedBarFill != null)
            _servedBarFill.fillAmount = 0f;

        if (_gradeStampRect != null)
        {
            _gradeStamp.text = string.Empty;
            _gradeStampRect.localScale = Vector3.zero;
        }

        // 순차 페이드 인 + 행 카운트업.
        var rowByGroup = new Dictionary<CanvasGroup, SettlementRow>();
        if (_revenueRow.Group != null) rowByGroup[_revenueRow.Group] = _revenueRow;
        if (_garbageRow.Group != null) rowByGroup[_garbageRow.Group] = _garbageRow;
        if (_leftoverRow.Group != null) rowByGroup[_leftoverRow.Group] = _leftoverRow;
        if (_netProfitRow.Group != null) rowByGroup[_netProfitRow.Group] = _netProfitRow;
        if (_coinRow.Group != null) rowByGroup[_coinRow.Group] = _coinRow;

        foreach (CanvasGroup group in _sequentialGroups)
        {
            if (group == null)
                continue;

            yield return FadeInGroup(group);

            if (rowByGroup.TryGetValue(group, out SettlementRow row))
                yield return CountUpRow(row);

            yield return WaitUnscaled(RowStagger);
        }

        // 서빙 막대 채우기.
        if (_servedBarFill != null)
            yield return FillBar(_servedBarFill, servedRatio);

        // 등급 스탬프 '쾅'.
        if (_gradeStampRect != null && !string.IsNullOrEmpty(settlement.Grade))
            yield return StampGrade(settlement.Grade);

        _presentRoutine = null;
    }

    IEnumerator FadeInGroup(CanvasGroup group)
    {
        float t = 0f;
        while (t < RowFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / RowFadeDuration);
            yield return null;
        }
        group.alpha = 1f;
    }

    IEnumerator CountUpRow(SettlementRow row)
    {
        if (row == null || row.ValueLabel == null)
            yield break;

        int targetValue = row.TargetValue;
        if (targetValue == 0)
        {
            SetRowValue(row, 0);
            yield break;
        }

        float t = 0f;
        while (t < CountUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / CountUpDuration);
            // EaseOut 느낌으로 가속 후 감속.
            float eased = 1f - (1f - k) * (1f - k);
            SetRowValue(row, Mathf.RoundToInt(Mathf.Lerp(0f, targetValue, eased)));
            yield return null;
        }

        SetRowValue(row, targetValue);
    }

    void SetRowValue(SettlementRow row, int value)
    {
        if (row == null || row.ValueLabel == null)
            return;

        row.ValueLabel.text = UIFontUtility.Sanitize($"{row.Prefix}{value.ToString("N0")}{row.Suffix}");
    }

    IEnumerator FillBar(Image fill, float ratio)
    {
        const float duration = 0.45f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            fill.fillAmount = Mathf.Lerp(0f, ratio, k);
            yield return null;
        }
        fill.fillAmount = ratio;
    }

    IEnumerator StampGrade(string grade)
    {
        _gradeStamp.text = UIFontUtility.Sanitize(grade);

        float t = 0f;
        while (t < StampPunchDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / StampPunchDuration);
            // 1.6 -> 1.0 EaseOut 펀치.
            float eased = 1f - (1f - k) * (1f - k);
            float scale = Mathf.Lerp(1.6f, 1f, eased);
            _gradeStampRect.localScale = Vector3.one * scale;
            yield return null;
        }

        _gradeStampRect.localScale = Vector3.one;
    }

    static IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void StopPresentRoutine()
    {
        if (_presentRoutine != null)
        {
            StopCoroutine(_presentRoutine);
            _presentRoutine = null;
        }
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
