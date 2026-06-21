using System;
using UnityEngine;

/// <summary>
/// 정산(Closing) 단계를 처리하는 싱글톤 매니저입니다.
/// 영업 종료 시 매출·폐기 비용·순이익을 계산하고, '다음 날 시작'으로 진행과 저장을 처리합니다.
/// </summary>
[DefaultExecutionOrder(-6)]
public class ClosingManager : MonoBehaviour
{
    public static ClosingManager Instance { get; private set; }

    int _settledDay = -1;

    public DailySettlement LastSettlement { get; private set; }

    public event Action<DailySettlement> OnSettlementReady;
    public event Action OnDayAdvanced;

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
        if (state == GameState.Closing)
            BuildSettlement();
    }

    void BuildSettlement()
    {
        if (GameManager.Instance == null)
            return;

        int day = GameManager.Instance.CurrentDay;
        if (_settledDay == day)
            return;

        _settledDay = day;

        // 타이머 만료로 남은 손님이 있으면 노쇼로 마감해 정산 카운트를 맞춘다.
        CustomerManager.Instance?.FinalizeNoShows();

        int revenue = ServiceManager.Instance != null ? ServiceManager.Instance.TotalRevenue : 0;
        int garbage = PreparationManager.Instance != null ? PreparationManager.Instance.GarbageDisposalCost : 0;
        int leftover = SupplyManager.Instance != null
            ? SupplyManager.Instance.CalculateRemainingIngredientDisposalCost()
            : 0;

        GameManager.Instance.Coin = Mathf.Max(0, GameManager.Instance.Coin - (garbage + leftover));

        DailySettlement settlement = new DailySettlement
        {
            Day = day,
            Revenue = revenue,
            GarbageCost = garbage,
            LeftoverCost = leftover,
            ServedCount = CustomerManager.Instance != null ? CustomerManager.Instance.ServedCount : 0,
            LeftCount = CustomerManager.Instance != null ? CustomerManager.Instance.LeftCount : 0,
            MergeCount = PreparationManager.Instance != null ? PreparationManager.Instance.DailyMergeCount : 0,
            CoinAfter = GameManager.Instance.Coin,
            UpcomingTrendName = TrendManager.Instance != null && TrendManager.Instance.UpcomingTrend != null
                ? TrendManager.Instance.UpcomingTrend.trendName
                : string.Empty
        };

        CollectUpcomingUnlocks(settlement, day + 1);

        LastSettlement = settlement;
        SaveLoadUtility.Save(GameManager.Instance,
            CustomerManager.Instance != null ? CustomerManager.Instance.ServedCount : 0);

        OnSettlementReady?.Invoke(settlement);
    }

    static void CollectUpcomingUnlocks(DailySettlement settlement, int nextDay)
    {
        if (DataManager.Instance == null)
            return;

        foreach (MergeRecipeSO recipe in DataManager.Instance.MergeRecipes)
        {
            if (recipe == null || recipe.unlockDay != nextDay)
                continue;

            string name = recipe.outputIngredient != null
                ? recipe.outputIngredient.ingredientName
                : "새 레시피";
            settlement.UpcomingUnlocks.Add(name);
        }
    }

    /// <summary>다음 날로 진행합니다. 일차 증가, 준비 초기화, 상태 전환, 저장.</summary>
    public void AdvanceToNextDay()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.CurrentDay++;
        PreparationManager.Instance?.ResetForNewDay();
        GameManager.Instance.SetState(GameState.Preparation);

        SaveLoadUtility.Save(GameManager.Instance,
            CustomerManager.Instance != null ? CustomerManager.Instance.ServedCount : 0);

        OnDayAdvanced?.Invoke();
    }
}
