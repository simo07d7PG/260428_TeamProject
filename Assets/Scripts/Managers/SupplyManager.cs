using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>하루 1회 발주와 영업 종료 시 재료 폐기 비용을 관리하는 싱글톤 매니저입니다.</summary>
[DefaultExecutionOrder(-5)]
public class SupplyManager : MonoBehaviour
{
    public const int EndOfDayDisposalBaseCost = 150;
    public const float EndOfDayDisposalRate = 0.3f;

    public static SupplyManager Instance { get; private set; }

    [SerializeField] int minimumCriticalStock = 1;

    public bool HasOrderedToday { get; private set; }

    public event Action OnSupplyStateChanged;

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

    public void ResetDailyOrder()
    {
        HasOrderedToday = false;
        OnSupplyStateChanged?.Invoke();
    }

    public bool CanOrderToday()
    {
        if (HasOrderedToday)
            return false;

        if (GameManager.Instance == null)
            return false;

        return GameManager.Instance.CurrentState == GameState.Preparation;
    }

    public int CalculateOrderCost(IReadOnlyDictionary<IngredientSO, int> order)
    {
        if (order == null)
            return 0;

        int total = 0;
        foreach (KeyValuePair<IngredientSO, int> pair in order)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            total += pair.Key.buyPrice * pair.Value;
        }

        return total;
    }

    public bool TrySubmitOrder(IReadOnlyDictionary<IngredientSO, int> order, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!CanOrderToday())
        {
            errorMessage = HasOrderedToday
                ? "오늘은 이미 발주했습니다."
                : "준비 단계에서만 발주할 수 있습니다.";
            return false;
        }

        if (order == null || order.Count == 0)
        {
            errorMessage = "발주할 재료를 선택해 주세요.";
            return false;
        }

        int totalCost = CalculateOrderCost(order);
        if (totalCost <= 0)
        {
            errorMessage = "발주 수량을 1개 이상 선택해 주세요.";
            return false;
        }

        if (GameManager.Instance == null)
        {
            errorMessage = "GameManager를 찾을 수 없습니다.";
            return false;
        }

        if (GameManager.Instance.Coin < totalCost)
        {
            errorMessage = $"코인이 부족합니다. (필요: {totalCost}, 보유: {GameManager.Instance.Coin})";
            return false;
        }

        if (PreparationManager.Instance == null)
        {
            errorMessage = "PreparationManager를 찾을 수 없습니다.";
            return false;
        }

        GameManager.Instance.Coin -= totalCost;

        foreach (KeyValuePair<IngredientSO, int> pair in order)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            PreparationManager.Instance.AddToInventory(pair.Key, 1, pair.Value);
        }

        HasOrderedToday = true;
        OnSupplyStateChanged?.Invoke();
        return true;
    }

    public int GetTotalStock(IngredientSO ingredient)
    {
        if (ingredient == null || PreparationManager.Instance == null)
            return 0;

        int stock = PreparationManager.Instance.GetInventoryCount(ingredient, 1);
        stock += PreparationManager.Instance.GetInventoryCount(ingredient, 2);

        for (int i = 0; i < PreparationManager.GridSize; i++)
        {
            MergeGridItem slot = PreparationManager.Instance.GetSlot(i);
            if (slot.isGarbage || slot.IsEmpty || slot.ingredient != ingredient)
                continue;

            stock++;
        }

        return stock;
    }

    public bool IsCriticalIngredientShort(IngredientSO ingredient)
    {
        if (ingredient == null || !ingredient.isCritical)
            return false;

        return GetTotalStock(ingredient) < minimumCriticalStock;
    }

    public bool CanCraftRequiring(IngredientSO ingredient, int requiredCount = 1)
    {
        if (ingredient == null || requiredCount <= 0)
            return false;

        if (ingredient.isCritical && GetTotalStock(ingredient) < requiredCount)
            return false;

        return GetTotalStock(ingredient) >= requiredCount;
    }

    public List<IngredientSO> GetShortCriticalIngredients()
    {
        List<IngredientSO> result = new List<IngredientSO>();

        if (DataManager.Instance == null)
            return result;

        foreach (IngredientSO ingredient in DataManager.Instance.GetCriticalIngredients())
        {
            if (IsCriticalIngredientShort(ingredient))
                result.Add(ingredient);
        }

        return result;
    }

    public int CalculateRemainingIngredientDisposalCost()
    {
        if (PreparationManager.Instance == null)
            return EndOfDayDisposalBaseCost;

        float ingredientValue = 0f;

        foreach (KeyValuePair<IngredientSO, int> pair in PreparationManager.Instance.BasicInventory)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            ingredientValue += pair.Key.buyPrice * pair.Value * EndOfDayDisposalRate;
        }

        foreach (MergeGridItem item in PreparationManager.Instance.AdvancedInventory)
        {
            if (item == null || item.ingredient == null)
                continue;

            ingredientValue += item.ingredient.buyPrice * EndOfDayDisposalRate;
        }

        for (int i = 0; i < PreparationManager.GridSize; i++)
        {
            MergeGridItem slot = PreparationManager.Instance.GetSlot(i);
            if (slot.isGarbage || slot.IsEmpty || slot.ingredient == null)
                continue;

            ingredientValue += slot.ingredient.buyPrice * EndOfDayDisposalRate;
        }

        return EndOfDayDisposalBaseCost + Mathf.CeilToInt(ingredientValue);
    }
}