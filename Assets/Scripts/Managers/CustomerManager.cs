using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영업 중 손님 도착, 대기열, 인내심을 관리하는 싱글톤 매니저입니다.
/// Service 상태에 진입하면 자동으로 영업을 시작하고, 일정 간격으로 손님을 스폰합니다.
/// </summary>
[DefaultExecutionOrder(-6)]
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("도착")]
    [SerializeField] float arrivalMin = 13f;
    [SerializeField] float arrivalMax = 27f;
    [SerializeField] float firstArrivalDelay = 2f;
    [SerializeField] int maxQueue = 4;
    [SerializeField] int customersPerDay = 8;

    [Header("인내심(초)")]
    [SerializeField] float patienceMin = 42f;
    [SerializeField] float patienceMax = 60f;

    readonly List<Customer> _queue = new();
    Customer _selected;
    int _spawnedCount;
    int _completedCount;
    int _nextId = 1;
    float _spawnTimer;
    bool _serviceActive;
    bool _dayCompleteFired;

    public IReadOnlyList<Customer> Queue => _queue;
    public Customer Selected => _selected;
    public int ServedCount { get; private set; }
    public int LeftCount { get; private set; }
    public int CompletedCount => _completedCount;
    public int CustomersPerDay => customersPerDay;
    public int RemainingToday => Mathf.Max(0, customersPerDay - _completedCount);
    public bool IsServiceActive => _serviceActive;
    public bool IsDayComplete => _completedCount >= customersPerDay;

    public event Action OnQueueChanged;
    public event Action<Customer> OnCustomerArrived;
    public event Action<Customer> OnCustomerLeft;
    public event Action<Customer> OnCustomerServed;
    public event Action<Customer> OnSelectionChanged;
    public event Action OnDayComplete;

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
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service)
            BeginService();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Service)
            BeginService();
        else
            EndService();
    }

    public void BeginService()
    {
        if (_serviceActive)
            return;

        _queue.Clear();
        _selected = null;
        _spawnedCount = 0;
        _completedCount = 0;
        ServedCount = 0;
        LeftCount = 0;
        _nextId = 1;
        _spawnTimer = firstArrivalDelay;
        _serviceActive = true;
        _dayCompleteFired = false;

        OnQueueChanged?.Invoke();
        OnSelectionChanged?.Invoke(null);
    }

    public void EndService()
    {
        if (!_serviceActive)
            return;

        _serviceActive = false;
        _selected = null;
        OnSelectionChanged?.Invoke(null);
        OnQueueChanged?.Invoke();
    }

    /// <summary>
    /// 영업 종료(타이머 만료 등) 시 대기열에 남은 손님을 노쇼(Left)로 마감합니다.
    /// 정산 카운트 정확도를 위해 정산 직전 호출합니다. (호출 순서에 무관하도록 idempotent)
    /// </summary>
    public void FinalizeNoShows()
    {
        bool any = false;
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            Customer customer = _queue[i];
            if (customer != null && customer.IsActive)
            {
                MarkLeft(customer, false);
                any = true;
            }
        }

        if (any)
            OnQueueChanged?.Invoke();
    }

    void Update()
    {
        if (!_serviceActive)
            return;

        float dt = Time.deltaTime;
        TickPatience(dt);
        TickSpawning(dt);
        CheckDayComplete();
    }

    void TickSpawning(float dt)
    {
        if (_spawnedCount >= customersPerDay || _queue.Count >= maxQueue)
            return;

        _spawnTimer -= dt;
        if (_spawnTimer > 0f)
            return;

        SpawnCustomer();
        _spawnTimer = UnityEngine.Random.Range(arrivalMin, arrivalMax);
    }

    void SpawnCustomer()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        List<MenuDefinition> menus = MenuCatalog.BuildCatalog(day);

        CustomerOrder order = menus.Count > 0
            ? CustomerOrder.FromMenu(menus[UnityEngine.Random.Range(0, menus.Count)])
            : CustomerOrder.FromMenu(null);

        float patience = UnityEngine.Random.Range(patienceMin, patienceMax);
        Customer customer = new Customer(_nextId++, order, patience);

        _queue.Add(customer);
        _spawnedCount++;

        OnCustomerArrived?.Invoke(customer);
        OnQueueChanged?.Invoke();
    }

    void TickPatience(float dt)
    {
        bool removedAny = false;

        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            Customer customer = _queue[i];
            if (customer == null)
            {
                _queue.RemoveAt(i);
                removedAny = true;
                continue;
            }

            if (customer.Tick(dt))
            {
                MarkLeft(customer, false);
                removedAny = true;
            }
        }

        if (removedAny)
            OnQueueChanged?.Invoke();
    }

    void MarkLeft(Customer customer, bool notifyQueue = true)
    {
        if (customer == null)
            return;

        customer.State = CustomerState.Left;
        _queue.Remove(customer);
        LeftCount++;
        _completedCount++;

        if (_selected == customer)
        {
            _selected = null;
            OnSelectionChanged?.Invoke(null);
        }

        OnCustomerLeft?.Invoke(customer);

        if (notifyQueue)
            OnQueueChanged?.Invoke();
    }

    /// <summary>손님에게 음료를 전달했을 때 호출합니다. (ServingManager에서 사용)</summary>
    public void MarkServed(Customer customer)
    {
        if (customer == null || customer.State == CustomerState.Served)
            return;

        customer.State = CustomerState.Served;
        _queue.Remove(customer);
        ServedCount++;
        _completedCount++;

        if (_selected == customer)
        {
            _selected = null;
            OnSelectionChanged?.Invoke(null);
        }

        OnCustomerServed?.Invoke(customer);
        OnQueueChanged?.Invoke();
    }

    public void Select(Customer customer)
    {
        if (!_serviceActive)
            return;

        if (customer != null && !_queue.Contains(customer))
            return;

        if (_selected != null && _selected != customer && _selected.State == CustomerState.Building)
            _selected.State = CustomerState.Waiting;

        _selected = customer;
        if (customer != null && customer.State == CustomerState.Waiting)
            customer.State = CustomerState.Building;

        OnSelectionChanged?.Invoke(_selected);
    }

    public void ClearSelection()
    {
        if (_selected == null)
            return;

        if (_selected.State == CustomerState.Building)
            _selected.State = CustomerState.Waiting;

        _selected = null;
        OnSelectionChanged?.Invoke(null);
    }

    void CheckDayComplete()
    {
        if (_dayCompleteFired || _completedCount < customersPerDay)
            return;

        _dayCompleteFired = true;
        OnDayComplete?.Invoke();
    }
}
