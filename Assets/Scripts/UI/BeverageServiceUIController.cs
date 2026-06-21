using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GPGP식 음료 제작 UI를 표시하고 BeverageBuildManager와 연동합니다.
/// 영업(Service) 상태에서만 보이며, 컵 렌더링·상태 텍스트·스테이션 조작을 갱신합니다.
/// Phase 5-R에서는 손님 없는 미리보기 흐름을 제공합니다.
/// </summary>
public class BeverageServiceUIController : MonoBehaviour
{
    BeverageUIPanelFactory.BeverageUIRefs _refs;
    GameState _lastVisibleState = (GameState)(-1);
    bool _toppingMode;
    bool _externalSubscribed;
    ServeDragInteraction _serveDrag;

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
        _refs = BeverageUIPanelFactory.EnsurePanel(transform);
        UIFontUtility.ApplyToHierarchy(transform);
        BindButtons();
        BindInteractionCallbacks();
        SetupServeDrag();
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        if (BeverageBuildManager.Instance != null)
            BeverageBuildManager.Instance.OnBuildStateChanged += RefreshAll;

        RefreshPanelVisibility();
        RefreshAll();
    }

    void Start()
    {
        // CustomerManager/ServiceManager는 이 호스트보다 늦게 생성될 수 있어 Start에서 구독합니다.
        SubscribeExternal();
        RefreshAll();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;

        if (BeverageBuildManager.Instance != null)
            BeverageBuildManager.Instance.OnBuildStateChanged -= RefreshAll;

        UnsubscribeExternal();
    }

    void SubscribeExternal()
    {
        if (_externalSubscribed)
            return;

        if (CustomerManager.Instance != null)
            CustomerManager.Instance.OnSelectionChanged += OnSelectionChanged;

        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnServed += OnServed;

        _externalSubscribed = CustomerManager.Instance != null || ServiceManager.Instance != null;
    }

    void UnsubscribeExternal()
    {
        if (!_externalSubscribed)
            return;

        if (CustomerManager.Instance != null)
            CustomerManager.Instance.OnSelectionChanged -= OnSelectionChanged;

        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnServed -= OnServed;

        _externalSubscribed = false;
    }

    void SetupServeDrag()
    {
        if (_refs.toppingInteraction == null)
            return;

        _serveDrag = ManagerUtility.GetOrAddComponent<ServeDragInteraction>(_refs.toppingInteraction.gameObject);
        _serveDrag.OnResult = SetStatus;
    }

    void OnSelectionChanged(Customer customer) => RefreshAll();

    void OnServed(Customer customer, ServingScoreBreakdown score, int payout) => RefreshAll();

    void Update()
    {
        RefreshPanelVisibility();
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && BeverageBuildManager.Instance == null)
            ManagerUtility.GetOrAddComponent<BeverageBuildManager>(PreparationManager.Instance.gameObject);
    }

    void BindButtons()
    {
        Bind(_refs.newCupButton, OnNewCupClicked);
        Bind(_refs.completeButton, OnCompleteClicked);
        Bind(_refs.clearButton, OnClearClicked);
        Bind(_refs.toppingButton, OnToppingToggleClicked);
        Bind(_refs.iceButton, OnIceClicked);
        Bind(_refs.lidButton, OnLidClicked);
        Bind(_refs.serveButton, OnServeClicked);
    }

    bool ServiceMode => ServiceManager.Instance != null;

    static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void BindInteractionCallbacks()
    {
        if (_refs.shotInteraction != null)
            _refs.shotInteraction.OnResult = SetStatus;
        if (_refs.milkInteraction != null)
            _refs.milkInteraction.OnResult = SetStatus;
        if (_refs.syrupInteraction != null)
            _refs.syrupInteraction.OnResult = SetStatus;
        if (_refs.toppingInteraction != null)
            _refs.toppingInteraction.OnResult = SetStatus;
    }

    // --- 버튼 핸들러 ---

    protected virtual void OnNewCupClicked()
    {
        BeverageBuildManager.Instance?.StartPreviewBuild();
        SetToppingMode(false);
        SetStatus("새 컵을 준비했습니다. 자유롭게 만들어 보세요.");
    }

    void OnCompleteClicked()
    {
        if (BeverageBuildManager.Instance == null)
            return;

        if (BeverageBuildManager.Instance.CompleteBuild(out string message))
            SetToppingMode(false);

        SetStatus(message);
    }

    void OnClearClicked()
    {
        BeverageBuildManager.Instance?.ClearBuild();
        SetToppingMode(false);
        SetStatus("컵을 비웠습니다.");
    }

    void OnToppingToggleClicked()
    {
        SetToppingMode(!_toppingMode);
        SetStatus(_toppingMode ? "토핑 모드: 컵을 탭해 올려주세요." : "토핑 모드를 껐습니다.");
    }

    void OnIceClicked()
    {
        if (BeverageBuildManager.Instance != null &&
            BeverageBuildManager.Instance.TryAddIce(out string message))
            SetStatus(message);
    }

    void OnLidClicked()
    {
        if (BeverageBuildManager.Instance != null &&
            BeverageBuildManager.Instance.TryApplyLid(out string message))
            SetStatus(message);
    }

    void OnServeClicked()
    {
        if (ServiceManager.Instance == null)
            return;

        if (ServiceManager.Instance.TryServeSelected(out string message))
            SetToppingMode(false);

        SetStatus(message);
    }

    void SetToppingMode(bool on)
    {
        _toppingMode = on;
        _refs.toppingInteraction?.SetToppingMode(on);

        if (_refs.toppingButton != null)
        {
            Image image = _refs.toppingButton.GetComponent<Image>();
            if (image != null)
                image.color = on ? new Color(0.30f, 0.55f, 0.82f, 1f) : new Color(0.24f, 0.28f, 0.34f, 1f);
        }
    }

    // --- 갱신 ---

    void OnGameStateChanged(GameState state)
    {
        RefreshPanelVisibility();
        RefreshAll();
    }

    void RefreshPanelVisibility()
    {
        if (GameManager.Instance == null || _refs.panelRoot == null)
            return;

        GameState state = GameManager.Instance.CurrentState;
        if (_lastVisibleState == state)
            return;

        _lastVisibleState = state;
        _refs.panelRoot.gameObject.SetActive(state == GameState.Service);
    }

    public void RefreshAll()
    {
        if (BeverageBuildManager.Instance == null)
            return;

        BeverageBuildSnapshot snapshot = BeverageBuildManager.Instance.GetCurrentSnapshot();
        _refs.cupCanvas?.Render(snapshot);
        RefreshTexts(snapshot);
        RefreshButtons();
        RefreshGauges(snapshot);
    }

    protected virtual void RefreshTexts(BeverageBuildSnapshot snapshot)
    {
        bool active = BeverageBuildManager.Instance.IsBuildActive;

        if (_refs.orderNoteText != null)
        {
            string note = !active
                ? (ServiceMode ? "손님 카드를 눌러 주문을 받으세요" : "‘새 컵’을 눌러 제작을 시작하세요")
                : BeverageBuildManager.Instance.IsPreview
                    ? "미리보기 모드 (손님 없음)"
                    : $"주문: {BeverageBuildManager.Instance.CurrentOrder?.phrase}";
            _refs.orderNoteText.text = UIFontUtility.Sanitize(note);
        }

        if (_refs.menuEstimateText != null)
        {
            string estimate = active
                ? $"예상: {snapshot.EstimatedMenuName} ({snapshot.MatchConfidence * 100f:0}%)"
                : "예상: -";
            _refs.menuEstimateText.text = UIFontUtility.Sanitize(estimate);
        }
    }

    protected void RefreshButtons()
    {
        bool service = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service;
        bool active = BeverageBuildManager.Instance.IsBuildActive;
        bool complete = BeverageBuildManager.Instance.IsBuildComplete;
        bool canOperate = BeverageBuildManager.Instance.CanOperate();
        bool empty = BeverageBuildManager.Instance.GetCurrentSnapshot().IsEmpty;
        bool serviceMode = ServiceMode;
        bool hasSelectedCustomer = CustomerManager.Instance != null && CustomerManager.Instance.Selected != null;

        SetActive(_refs.newCupButton, !serviceMode);
        SetActive(_refs.serveButton, serviceMode);

        SetInteractable(_refs.newCupButton, !serviceMode && service);
        SetInteractable(_refs.serveButton, serviceMode && complete && hasSelectedCustomer);
        SetInteractable(_refs.completeButton, active && !complete && !empty);
        SetInteractable(_refs.clearButton, active);
        SetInteractable(_refs.toppingButton, canOperate);
        SetInteractable(_refs.iceButton, canOperate);
        SetInteractable(_refs.lidButton, canOperate);
    }

    static void SetActive(Button button, bool value)
    {
        if (button != null && button.gameObject.activeSelf != value)
            button.gameObject.SetActive(value);
    }

    void RefreshGauges(BeverageBuildSnapshot snapshot)
    {
        _refs.milkStation?.SetGauge(snapshot.MilkAmount);
        _refs.shotStation?.SetGauge(0f);

        if (_refs.shotStation != null)
            _refs.shotStation.SetLabel(snapshot.ShotCount > 0
                ? $"에스프레소\n샷 {snapshot.ShotCount}"
                : "에스프레소\n(홀드)");
    }

    static void SetInteractable(Button button, bool value)
    {
        if (button != null)
            button.interactable = value;
    }

    protected void SetStatus(string message)
    {
        if (_refs.statusText != null)
            _refs.statusText.text = UIFontUtility.Sanitize(message);
    }

    protected BeverageUIPanelFactory.BeverageUIRefs Refs => _refs;
}
