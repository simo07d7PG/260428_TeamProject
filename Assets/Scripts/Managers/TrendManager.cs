using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>일자별 트렌드를 관리하는 싱글톤 매니저입니다. 활성 트렌드의 multiplier를 메뉴 매출에 적용합니다.</summary>
[DefaultExecutionOrder(-7)]
public class TrendManager : MonoBehaviour
{
    public static TrendManager Instance { get; private set; }

    TrendSO _activeTrend;
    TrendSO _upcomingTrend;
    int _activeStartDay;
    int _lastEvaluatedDay = -1;

    public TrendSO ActiveTrend => _activeTrend;
    public TrendSO UpcomingTrend => _upcomingTrend;

    public event Action OnTrendChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        EvaluateDay(CurrentDay());
    }

    void Update()
    {
        int day = CurrentDay();
        if (day != _lastEvaluatedDay)
            EvaluateDay(day);
    }

    static int CurrentDay()
    {
        return GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
    }

    void EvaluateDay(int day)
    {
        _lastEvaluatedDay = day;

        List<TrendSO> trends = GetTrends();
        if (trends.Count == 0)
        {
            _activeTrend = null;
            _upcomingTrend = null;
            OnTrendChanged?.Invoke();
            return;
        }

        bool changed = false;

        if (_activeTrend == null)
        {
            _activeTrend = PickRandom(trends, null);
            _activeStartDay = day;
            _upcomingTrend = PickRandom(trends, _activeTrend);
            changed = true;
        }
        else if (day >= _activeStartDay + Mathf.Max(1, _activeTrend.durationDays))
        {
            _activeTrend = _upcomingTrend != null ? _upcomingTrend : PickRandom(trends, _activeTrend);
            _activeStartDay = day;
            _upcomingTrend = PickRandom(trends, _activeTrend);
            changed = true;
        }

        if (changed)
            OnTrendChanged?.Invoke();
    }

    static List<TrendSO> GetTrends()
    {
        List<TrendSO> result = new List<TrendSO>();
        if (DataManager.Instance == null)
            return result;

        foreach (TrendSO trend in DataManager.Instance.Trends)
        {
            if (trend != null)
                result.Add(trend);
        }

        return result;
    }

    static TrendSO PickRandom(List<TrendSO> trends, TrendSO exclude)
    {
        if (trends.Count == 0)
            return null;

        if (trends.Count == 1)
            return trends[0];

        TrendSO chosen;
        int guard = 0;
        do
        {
            chosen = trends[UnityEngine.Random.Range(0, trends.Count)];
            guard++;
        }
        while (chosen == exclude && guard < 8);

        return chosen;
    }

    public float GetOrderMultiplier(CustomerOrder order)
    {
        if (_activeTrend == null || order == null || order.menu == null)
            return 1f;

        if (_activeTrend.affectedIngredients == null)
            return 1f;

        foreach (IngredientSO ingredient in _activeTrend.affectedIngredients)
        {
            if (ingredient != null && MenuUsesType(order.menu, ingredient.type))
                return Mathf.Max(0.1f, _activeTrend.multiplier);
        }

        return 1f;
    }

    public float GetIngredientMultiplier(IngredientSO ingredient)
    {
        if (_activeTrend == null || ingredient == null || _activeTrend.affectedIngredients == null)
            return 1f;

        foreach (IngredientSO affected in _activeTrend.affectedIngredients)
        {
            if (affected == ingredient || (affected != null && affected.type == ingredient.type))
                return Mathf.Max(0.1f, _activeTrend.multiplier);
        }

        return 1f;
    }

    static bool MenuUsesType(MenuDefinition menu, IngredientType type)
    {
        return type switch
        {
            IngredientType.Base => menu.requiredShots > 0,
            IngredientType.Milk => menu.milkAmount > 0.05f,
            IngredientType.Syrup => menu.syrupCount > 0,
            IngredientType.Topping => menu.toppingCount > 0,
            _ => false
        };
    }
}
