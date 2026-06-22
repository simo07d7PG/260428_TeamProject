using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사진형 카페 카운터 제작 UI를 표시하고 BeverageBuildManager와 연동합니다.
/// 컵을 머신에 올려 버튼 홀드로 샷 추출, 컵을 내려 재료 도구로 제작, 손님 카드로 끌어 서빙.
/// 상단 큰 주문 배너에 손님 음료/구성/인내심을 표시하고, 각 칸 아래 재료 변경을 갱신합니다.
/// </summary>
public class BeverageServiceUIController : MonoBehaviour
{
    BeverageUIPanelFactory.BeverageUIRefs _refs;
    GameState _lastVisibleState = (GameState)(-1);
    bool _externalSubscribed;
    bool _wasActive;
    bool _cupTaken; // 손님이 컵통을 눌러 컵을 집었는지

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        EnsureManagers();
        _refs = BeverageUIPanelFactory.EnsurePanel(transform);
        UIFontUtility.ApplyToHierarchy(transform);
        BindButtons();
        WireCallbacks();
    }

    void WireCallbacks()
    {
        if (_refs.cupDrag != null) _refs.cupDrag.OnResult = SetStatus;
        if (_refs.machineButton != null) _refs.machineButton.OnResult = SetStatus;
        if (_refs.milkTool != null) _refs.milkTool.OnResult = SetStatus;
        if (_refs.iceTool != null) _refs.iceTool.OnResult = SetStatus;
        if (_refs.toppingTool != null) _refs.toppingTool.OnResult = SetStatus;
        if (_refs.syrupTool != null) _refs.syrupTool.OnResult = SetStatus;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += OnGameStateChanged;
        if (BeverageBuildManager.Instance != null) BeverageBuildManager.Instance.OnBuildStateChanged += RefreshAll;
        RefreshPanelVisibility();
        RefreshAll();
    }

    void Start()
    {
        SubscribeExternal();
        RefreshAll();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        if (BeverageBuildManager.Instance != null) BeverageBuildManager.Instance.OnBuildStateChanged -= RefreshAll;
        UnsubscribeExternal();
    }

    void SubscribeExternal()
    {
        if (_externalSubscribed) return;
        if (CustomerManager.Instance != null) CustomerManager.Instance.OnSelectionChanged += OnSelectionChanged;
        _externalSubscribed = CustomerManager.Instance != null;
    }

    void UnsubscribeExternal()
    {
        if (!_externalSubscribed) return;
        if (CustomerManager.Instance != null) CustomerManager.Instance.OnSelectionChanged -= OnSelectionChanged;
        _externalSubscribed = false;
    }

    void OnSelectionChanged(Customer customer) => RefreshAll();

    void Update()
    {
        RefreshPanelVisibility();
        RefreshPatience();
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && BeverageBuildManager.Instance == null)
            ManagerUtility.GetOrAddComponent<BeverageBuildManager>(PreparationManager.Instance.gameObject);
    }

    void BindButtons()
    {
        Bind(_refs.newCupButton, OnNewCupClicked);
        Bind(_refs.clearButton, OnClearClicked);
        Bind(_refs.cupStackButton, TakeCup);
    }

    void TakeCup()
    {
        if (BeverageBuildManager.Instance == null || !BeverageBuildManager.Instance.IsBuildActive)
        {
            SetStatus(ServiceMode ? "먼저 손님을 눌러 주문을 받으세요." : "‘새 컵’으로 시작하세요.");
            return;
        }

        if (_cupTaken)
            return;

        _cupTaken = true;
        _refs.cupDrag?.ResetToStack();
        if (_refs.cupRoot != null)
            _refs.cupRoot.gameObject.SetActive(true);
        AudioManager.PlaySfx("cup_take");
        SetStatus("컵을 집었습니다. 컵을 머신으로 끌어 올리세요.");
    }

    static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    bool ServiceMode => ServiceManager.Instance != null;

    void OnNewCupClicked()
    {
        BeverageBuildManager.Instance?.StartPreviewBuild();
        TakeCup(); // 미리보기는 즉시 컵을 집어 둠
    }

    void OnClearClicked()
    {
        BeverageBuildManager.Instance?.ClearBuild();
        SetStatus("컵을 비웠습니다.");
    }

    void OnGameStateChanged(GameState state)
    {
        RefreshPanelVisibility();
        RefreshAll();
    }

    void RefreshPanelVisibility()
    {
        if (GameManager.Instance == null || _refs.panelRoot == null) return;
        GameState state = GameManager.Instance.CurrentState;
        if (_lastVisibleState == state) return;
        _lastVisibleState = state;
        _refs.panelRoot.gameObject.SetActive(state == GameState.Service);
    }

    public void RefreshAll()
    {
        if (BeverageBuildManager.Instance == null) return;

        BeverageBuildManager manager = BeverageBuildManager.Instance;
        BeverageBuildSnapshot snapshot = manager.GetCurrentSnapshot();
        bool active = manager.IsBuildActive;

        if (active && !_wasActive)
        {
            _cupTaken = false; // 새 주문: 컵통을 눌러 직접 집어야 함
            SetStatus("컵통을 눌러 컵을 집으세요.");
        }
        if (!active)
            _cupTaken = false;
        _wasActive = active;

        // 컵은 '주문 활성 + 컵을 집었을 때'만 보입니다.
        if (_refs.cupRoot != null)
            _refs.cupRoot.gameObject.SetActive(active && _cupTaken);

        _refs.cupCanvas?.Render(snapshot);
        RefreshOrderBanner(snapshot, manager);
        RefreshMachineLabel(snapshot);
        RefreshSelectors();
        RefreshButtons(active);
    }

    void RefreshOrderBanner(BeverageBuildSnapshot snapshot, BeverageBuildManager manager)
    {
        bool active = manager.IsBuildActive;
        CustomerOrder order = manager.CurrentOrder;

        if (_refs.orderNameText != null)
        {
            if (!active)
                _refs.orderNameText.text = UIFontUtility.Sanitize(ServiceMode ? "손님 카드를 눌러 주문 받기" : "‘새 컵’으로 시작");
            else if (manager.IsPreview || order == null)
                _refs.orderNameText.text = UIFontUtility.Sanitize("미리보기");
            else
                _refs.orderNameText.text = UIFontUtility.Sanitize(order.MenuName);
        }

        if (_refs.orderCompText != null)
        {
            string comp = active && order?.menu != null
                ? $"\"{order.phrase}\"   {Composition(order.menu)}"
                : (active && manager.IsPreview ? "자유롭게 만들어 보세요" : string.Empty);
            _refs.orderCompText.text = UIFontUtility.Sanitize(comp);
        }

        if (_refs.orderIcon != null)
        {
            Sprite icon = active ? order?.menu?.icon : null;
            _refs.orderIcon.sprite = icon;
            _refs.orderIcon.enabled = icon != null;
        }

        if (_refs.menuEstimateText != null)
            _refs.menuEstimateText.text = UIFontUtility.Sanitize(active
                ? $"예상: {snapshot.EstimatedMenuName} ({snapshot.MatchConfidence * 100f:0}%)"
                : "예상: -");
    }

    void RefreshPatience()
    {
        if (_refs.orderPatienceFill == null) return;

        Customer customer = ServiceManager.Instance != null ? ServiceManager.Instance.ActiveCustomer : null;
        if (customer == null || !customer.IsActive)
        {
            _refs.orderPatienceFill.fillAmount = 0f;
            return;
        }

        float ratio = customer.PatienceRatio;
        _refs.orderPatienceFill.fillAmount = ratio;
        _refs.orderPatienceFill.color = ratio > 0.5f
            ? new Color(0.35f, 0.8f, 0.4f, 1f)
            : ratio > 0.25f
                ? new Color(0.95f, 0.8f, 0.3f, 1f)
                : new Color(0.9f, 0.35f, 0.3f, 1f);
    }

    void RefreshMachineLabel(BeverageBuildSnapshot snapshot)
    {
        if (_refs.machineLabel == null || BeverageBuildManager.Instance == null) return;
        int beans = BeverageBuildManager.Instance.CountAvailable(IngredientType.Base);
        _refs.machineLabel.text = UIFontUtility.Sanitize(snapshot.ShotCount > 0
            ? $"커피 머신\n샷 {snapshot.ShotCount} · 원두 {beans}"
            : $"커피 머신\n원두 {beans}");
    }

    void RefreshSelectors()
    {
        if (_refs.selectors == null) return;
        foreach (MaterialSelectorUI selector in _refs.selectors)
            selector?.Refresh();
    }

    void RefreshButtons(bool active)
    {
        bool service = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service;
        SetActive(_refs.newCupButton, !ServiceMode);
        SetInteractable(_refs.newCupButton, !ServiceMode && service);
        SetInteractable(_refs.clearButton, active);
    }

    static string Composition(MenuDefinition menu)
    {
        List<string> parts = new List<string>();
        if (menu.requiredShots > 0) parts.Add($"샷{menu.requiredShots}");
        if (menu.milkAmount > 0.05f) parts.Add(menu.milkAmount >= 0.6f ? "우유많이" : "우유");
        if (menu.syrupCount > 0) parts.Add($"시럽{menu.syrupCount}");
        if (menu.toppingCount > 0) parts.Add("토핑");
        if (menu.requiresIce) parts.Add("아이스");
        return parts.Count > 0 ? string.Join(" / ", parts) : "-";
    }

    static void SetActive(Button button, bool value)
    {
        if (button != null && button.gameObject.activeSelf != value)
            button.gameObject.SetActive(value);
    }

    static void SetInteractable(Button button, bool value)
    {
        if (button != null) button.interactable = value;
    }

    void SetStatus(string message)
    {
        if (_refs.statusText != null)
            _refs.statusText.text = UIFontUtility.Sanitize(message);
    }
}
