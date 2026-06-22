using System;
using UnityEngine;

/// <summary>
/// 영업 루프를 총괄하는 싱글톤 매니저입니다.
/// 준비→영업 전환, 손님 선택 시 제작 시작, 서빙 평가·지급, 4분 타이머, 영업 종료를 처리합니다.
/// </summary>
[DefaultExecutionOrder(-8)]
public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }

    [SerializeField] float sessionSeconds = 240f;

    float _timer;
    bool _running;
    bool _customerSubscribed;
    Customer _activeCustomer;

    public float TimeRemaining => _timer;
    public float SessionSeconds => sessionSeconds;
    public bool IsRunning => _running;
    public int TotalRevenue { get; private set; }
    public Customer ActiveCustomer => _activeCustomer;

    public event Action OnServiceStarted;
    public event Action OnServiceEnded;
    public event Action<Customer, ServingScoreBreakdown, int> OnServed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;

        SubscribeToCustomerManager();
    }

    void Start()
    {
        SubscribeToCustomerManager();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service)
            BeginTimer();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;

        UnsubscribeFromCustomerManager();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void SubscribeToCustomerManager()
    {
        if (_customerSubscribed || CustomerManager.Instance == null)
            return;

        CustomerManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        CustomerManager.Instance.OnDayComplete += HandleDayComplete;
        _customerSubscribed = true;
    }

    void UnsubscribeFromCustomerManager()
    {
        if (!_customerSubscribed || CustomerManager.Instance == null)
            return;

        CustomerManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        CustomerManager.Instance.OnDayComplete -= HandleDayComplete;
        _customerSubscribed = false;
    }

    /// <summary>준비 단계에서 영업을 시작합니다. (영업 시작 버튼)</summary>
    public bool StartService()
    {
        if (GameManager.Instance == null)
            return false;

        if (GameManager.Instance.CurrentState != GameState.Preparation)
            return false;

        GameManager.Instance.StartService();
        return true;
    }

    public void EndService()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service)
            GameManager.Instance.EndService();
    }

    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Service)
            BeginTimer();
        else
            _running = false;
    }

    void BeginTimer()
    {
        _timer = sessionSeconds;
        _running = true;
        _activeCustomer = null;
        TotalRevenue = 0;
        OnServiceStarted?.Invoke();
    }

    void Update()
    {
        if (!_running || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Service)
            return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = 0f;
            _running = false;
            OnServiceEnded?.Invoke();
            EndService();
        }
    }

    void HandleSelectionChanged(Customer customer)
    {
        if (customer == null)
        {
            if (BeverageBuildManager.Instance != null &&
                BeverageBuildManager.Instance.IsBuildActive &&
                !BeverageBuildManager.Instance.IsBuildComplete)
                BeverageBuildManager.Instance.ClearBuild();

            _activeCustomer = null;
            return;
        }

        if (customer == _activeCustomer)
            return;

        _activeCustomer = customer;
        BeverageBuildManager.Instance?.StartBuild(customer.Order);
    }

    void HandleDayComplete()
    {
        EndService();
    }

    public bool TryServeSelected(out string message)
    {
        return TryServe(CustomerManager.Instance?.Selected, out message);
    }

    /// <summary>완성된 음료를 지정 손님에게 전달하고 평가·지급합니다.</summary>
    public bool TryServe(Customer customer, out string message)
    {
        message = string.Empty;

        if (BeverageBuildManager.Instance == null || !BeverageBuildManager.Instance.IsBuildComplete)
        {
            message = "음료를 먼저 완성해 주세요.";
            return false;
        }

        if (customer == null || !customer.IsActive)
        {
            message = "전달할 손님을 선택해 주세요.";
            return false;
        }

        BeverageBuildSnapshot snapshot = BeverageBuildManager.Instance.GetCurrentSnapshot();
        ServingScoreBreakdown score = ServingEvaluator.Evaluate(customer.Order, snapshot, customer.PatienceRatio);
        int premium = snapshot != null ? Mathf.Min(snapshot.PremiumIngredientCount, 3) : 0;
        int payout = CalculatePayout(customer, score, premium);

        if (payout > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.Coin += payout;
            TotalRevenue += payout;
        }

        string premiumNote = premium > 0 && payout > 0 ? $" · 프리미엄x{premium} 보너스" : string.Empty;
        message = payout > 0
            ? $"{score.Summary} (+{payout} Coin{premiumNote})"
            : $"{score.Summary} (환불)";

        OnServed?.Invoke(customer, score, payout);

        CustomerManager.Instance?.MarkServed(customer);
        _activeCustomer = null;
        BeverageBuildManager.Instance.ClearBuild();
        return true;
    }

    int CalculatePayout(Customer customer, ServingScoreBreakdown score, int premiumIngredients)
    {
        if (score == null || !score.IsCorrectMenu || score.PayoutMultiplier <= 0f)
            return 0;

        float patienceModifier = 1f + (customer.PatienceRatio - 0.5f) * 0.7f; // 0.65 ~ 1.35
        float menuMultiplier = GetMenuMultiplier(customer.Order);
        // 머지로 만든 Lv2 프리미엄 재료를 쓰면 재료당 +12%(최대 3개 = +36%) 매출 보너스.
        float premiumMultiplier = 1f + 0.12f * Mathf.Clamp(premiumIngredients, 0, 3);
        float raw = customer.Order.BasePrice * score.PayoutMultiplier * patienceModifier * menuMultiplier * premiumMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    /// <summary>트렌드 배수를 적용합니다. (Phase 8)</summary>
    float GetMenuMultiplier(CustomerOrder order)
    {
        return TrendManager.Instance != null ? TrendManager.Instance.GetOrderMultiplier(order) : 1f;
    }
}
