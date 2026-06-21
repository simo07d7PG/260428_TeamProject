using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 바(일차, 코인, 영업 시작 버튼/타이머)를 표시하고 ServiceManager와 연동합니다.
/// 코인을 실시간으로 갱신하고, 준비↔영업 전환 진입점을 제공합니다.
/// </summary>
public class DayFlowUIController : MonoBehaviour
{
    RectTransform _bar;
    TextMeshProUGUI _dayText;
    TextMeshProUGUI _coinText;
    TextMeshProUGUI _timerText;
    TextMeshProUGUI _servedText;
    Button _startServiceButton;

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
        BuildBar();
        UIFontUtility.ApplyToHierarchy(transform);
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;

        RefreshMode();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        if (GameManager.Instance != null)
        {
            if (_coinText != null)
                _coinText.text = UIFontUtility.Sanitize($"코인: {GameManager.Instance.Coin}");
            if (_dayText != null)
                _dayText.text = UIFontUtility.Sanitize($"Day {GameManager.Instance.CurrentDay}");
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service)
            RefreshServiceInfo();
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && ServiceManager.Instance == null)
            ManagerUtility.GetOrAddComponent<ServiceManager>(PreparationManager.Instance.gameObject);
    }

    void BuildBar()
    {
        GameObject barObject = UIFactoryUtility.CreateUIObject("TopBar", transform, typeof(Image));
        _bar = barObject.GetComponent<RectTransform>();
        _bar.anchorMin = new Vector2(0f, 1f);
        _bar.anchorMax = new Vector2(1f, 1f);
        _bar.pivot = new Vector2(0.5f, 1f);
        _bar.anchoredPosition = Vector2.zero;
        _bar.sizeDelta = new Vector2(0f, 56f);
        barObject.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.96f);

        _dayText = UIFactoryUtility.CreateLabel(_bar, "DayText", "Day 1", 20f);
        _dayText.alignment = TextAlignmentOptions.MidlineLeft;
        AnchorLeft(_dayText.rectTransform, 20f, 160f);
        _dayText.fontStyle = FontStyles.Bold;

        _coinText = UIFactoryUtility.CreateLabel(_bar, "CoinText", "코인: 0", 20f);
        _coinText.alignment = TextAlignmentOptions.Center;
        AnchorCenter(_coinText.rectTransform, 260f);

        _timerText = UIFactoryUtility.CreateLabel(_bar, "TimerText", "4:00", 20f);
        _timerText.alignment = TextAlignmentOptions.MidlineRight;
        AnchorRight(_timerText.rectTransform, 20f, 120f);
        _timerText.fontStyle = FontStyles.Bold;

        _servedText = UIFactoryUtility.CreateLabel(_bar, "ServedText", string.Empty, 15f);
        _servedText.alignment = TextAlignmentOptions.MidlineRight;
        AnchorRight(_servedText.rectTransform, 150f, 160f);

        _startServiceButton = UIFactoryUtility.CreateButton(
            _bar, "StartServiceButton", "영업 시작", new Color(0.30f, 0.62f, 0.36f, 1f));
        RectTransform startRect = _startServiceButton.GetComponent<RectTransform>();
        AnchorRight(startRect, 20f, 150f);
        startRect.sizeDelta = new Vector2(150f, 42f);
        _startServiceButton.onClick.AddListener(OnStartServiceClicked);
    }

    void OnStartServiceClicked()
    {
        ServiceManager.Instance?.StartService();
        RefreshMode();
    }

    void HandleStateChanged(GameState state)
    {
        RefreshMode();
    }

    void RefreshMode()
    {
        bool prep = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Preparation;
        bool service = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service;

        if (_startServiceButton != null)
            _startServiceButton.gameObject.SetActive(prep);
        if (_timerText != null)
            _timerText.gameObject.SetActive(service);
        if (_servedText != null)
            _servedText.gameObject.SetActive(service);
    }

    void RefreshServiceInfo()
    {
        if (_timerText != null && ServiceManager.Instance != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, ServiceManager.Instance.TimeRemaining));
            _timerText.text = UIFontUtility.Sanitize($"{seconds / 60}:{seconds % 60:00}");
            _timerText.color = seconds <= 30 ? new Color(0.95f, 0.4f, 0.35f, 1f) : Color.white;
        }

        if (_servedText != null && CustomerManager.Instance != null)
        {
            CustomerManager manager = CustomerManager.Instance;
            _servedText.text = UIFontUtility.Sanitize(
                $"서빙 {manager.ServedCount}/{manager.CustomersPerDay}");
        }
    }

    static void AnchorLeft(RectTransform rect, float inset, float width)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(inset, 0f);
        rect.sizeDelta = new Vector2(width, 36f);
    }

    static void AnchorCenter(RectTransform rect, float width)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, 36f);
    }

    static void AnchorRight(RectTransform rect, float inset, float width)
    {
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-inset, 0f);
        rect.sizeDelta = new Vector2(width, 36f);
    }
}
