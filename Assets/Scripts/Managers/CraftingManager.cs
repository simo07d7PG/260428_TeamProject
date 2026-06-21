using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영업 단계의 3단계 음식 제작(베이스 → 액체/시럽 → 토핑)을 관리하는 싱글톤 매니저입니다.
/// </summary>
[DefaultExecutionOrder(-4)]
public class CraftingManager : MonoBehaviour
{
    public const float HighSuccessThreshold = 85f;
    public const float LowSuccessThreshold = 50f;
    public const float HighPayoutMultiplier = 1.25f;
    public const float LowPayoutMultiplier = 0.85f;
    public const float BaseMenuPayout = 50f;
    public const float SkipToppingPenalty = 8f;

    public static CraftingManager Instance { get; private set; }

    [SerializeField] float baseStageDuration = 2f;
    [SerializeField] float liquidStageDuration = 1.5f;
    [SerializeField] float toppingStageDuration = 1f;
    [SerializeField] float advancedSpeedMultiplier = 0.75f;
    [SerializeField] float baseSuccessRate = 35f;
    [SerializeField] float level1SuccessBonus = 15f;
    [SerializeField] float level2SuccessBonus = 25f;
    [SerializeField] float level2PayoutBonus = 1.5f;

    readonly List<IngredientSO> _usedIngredients = new();
    readonly List<int> _usedLevels = new();

    CraftStage _currentStage = CraftStage.Base;
    CraftResult _lastResult;
    Coroutine _stageRoutine;
    float _currentSuccessRate;
    float _stageProgress;
    bool _isCrafting;
    bool _skippedTopping;
    bool _sessionActive;

    public CraftStage CurrentStage => _currentStage;
    public float CurrentSuccessRate => _currentSuccessRate;
    public float StageProgress => _stageProgress;
    public bool IsCrafting => _isCrafting;
    public bool IsSessionActive => _sessionActive;
    public CraftResult LastResult => _lastResult;

    public event Action OnCraftingStateChanged;
    public event Action<CraftResult> OnCraftingCompleted;
    public event Action<CraftResult> OnReadyForDelivery;

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

    public bool CanCraft()
    {
        if (GameManager.Instance == null)
            return false;

        return GameManager.Instance.CurrentState == GameState.Service
            && !_isCrafting
            && _currentStage != CraftStage.Complete;
    }

    public bool TryStartSession(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (GameManager.Instance == null)
        {
            errorMessage = "GameManager를 찾을 수 없습니다.";
            return false;
        }

        if (GameManager.Instance.CurrentState != GameState.Service)
        {
            errorMessage = "영업 단계에서만 제작할 수 있습니다.";
            return false;
        }

        if (_isCrafting)
        {
            errorMessage = "제작이 진행 중입니다.";
            return false;
        }

        ResetSession();
        _sessionActive = true;
        NotifyStateChanged();
        return true;
    }

    public void ResetSession()
    {
        if (_stageRoutine != null)
        {
            StopCoroutine(_stageRoutine);
            _stageRoutine = null;
        }

        _currentStage = CraftStage.Base;
        _currentSuccessRate = baseSuccessRate;
        _stageProgress = 0f;
        _isCrafting = false;
        _skippedTopping = false;
        _sessionActive = false;
        _lastResult = null;
        _usedIngredients.Clear();
        _usedLevels.Clear();
        NotifyStateChanged();
    }

    public List<CraftingIngredientEntry> GetAvailableIngredients(CraftStage stage)
    {
        List<CraftingIngredientEntry> entries = new List<CraftingIngredientEntry>();

        if (PreparationManager.Instance == null || DataManager.Instance == null)
            return entries;

        Dictionary<string, CraftingIngredientEntry> grouped = new Dictionary<string, CraftingIngredientEntry>();

        foreach (KeyValuePair<IngredientSO, int> pair in PreparationManager.Instance.BasicInventory)
        {
            IngredientSO ingredient = pair.Key;
            if (ingredient == null || pair.Value <= 0)
                continue;

            if (!IsIngredientValidForStage(ingredient, stage))
                continue;

            if (!CanUseIngredient(ingredient))
                continue;

            string key = $"{ingredient.name}_1";
            grouped[key] = new CraftingIngredientEntry(ingredient, 1, pair.Value);
        }

        foreach (MergeGridItem item in PreparationManager.Instance.AdvancedInventory)
        {
            if (item == null || item.ingredient == null)
                continue;

            if (!IsIngredientValidForStage(item.ingredient, stage))
                continue;

            if (!CanUseIngredient(item.ingredient))
                continue;

            string key = $"{item.ingredient.name}_{item.level}";
            if (grouped.TryGetValue(key, out CraftingIngredientEntry existing))
                grouped[key] = new CraftingIngredientEntry(item.ingredient, item.level, existing.Count + 1);
            else
                grouped[key] = new CraftingIngredientEntry(item.ingredient, item.level, 1);
        }

        entries.AddRange(grouped.Values);
        entries.Sort((a, b) =>
        {
            int levelCompare = b.Level.CompareTo(a.Level);
            if (levelCompare != 0)
                return levelCompare;

            string nameA = a.Ingredient != null ? a.Ingredient.ingredientName : string.Empty;
            string nameB = b.Ingredient != null ? b.Ingredient.ingredientName : string.Empty;
            return string.Compare(nameA, nameB, StringComparison.Ordinal);
        });

        return entries;
    }

    public bool TrySelectIngredient(IngredientSO ingredient, int level, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!_sessionActive)
        {
            errorMessage = "제작을 먼저 시작해 주세요.";
            return false;
        }

        if (!CanCraft())
        {
            errorMessage = "현재 재료를 선택할 수 없습니다.";
            return false;
        }

        if (ingredient == null)
        {
            errorMessage = "유효하지 않은 재료입니다.";
            return false;
        }

        if (!IsIngredientValidForStage(ingredient, _currentStage))
        {
            errorMessage = $"{GetStageLabel(_currentStage)} 단계에 사용할 수 없는 재료입니다.";
            return false;
        }

        if (!CanUseIngredient(ingredient))
        {
            errorMessage = $"{ingredient.ingredientName} 재료가 부족합니다.";
            return false;
        }

        if (PreparationManager.Instance == null
            || !PreparationManager.Instance.TryConsumeFromInventory(ingredient, level))
        {
            errorMessage = "인벤토리에서 재료를 사용할 수 없습니다.";
            return false;
        }

        _usedIngredients.Add(ingredient);
        _usedLevels.Add(level);
        ApplyIngredientSuccessBonus(level);

        _stageRoutine = StartCoroutine(ProcessStageRoutine(ingredient, level));
        return true;
    }

    public bool TrySkipTopping(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!_sessionActive || _currentStage != CraftStage.Topping || _isCrafting)
        {
            errorMessage = "토핑 단계에서만 건너뛸 수 있습니다.";
            return false;
        }

        _skippedTopping = true;
        _currentSuccessRate = Mathf.Max(0f, _currentSuccessRate - SkipToppingPenalty);
        CompleteCrafting();
        return true;
    }

    public bool TryDeliverToCustomer(out string message)
    {
        message = string.Empty;

        if (_lastResult == null || _currentStage != CraftStage.Complete)
        {
            message = "완성된 음식이 없습니다.";
            return false;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Coin += _lastResult.FinalPayout;

        OnReadyForDelivery?.Invoke(_lastResult);
        message = $"손님에게 전달 완료 (+{_lastResult.FinalPayout} Coin)";
        ResetSession();
        return true;
    }

    IEnumerator ProcessStageRoutine(IngredientSO ingredient, int level)
    {
        _isCrafting = true;
        _stageProgress = 0f;
        NotifyStateChanged();

        float duration = GetStageDuration(_currentStage, level);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _stageProgress = Mathf.Clamp01(elapsed / duration);
            NotifyStateChanged();
            yield return null;
        }

        _stageProgress = 1f;
        _isCrafting = false;
        AdvanceStage();
    }

    void AdvanceStage()
    {
        if (_currentStage == CraftStage.Base)
            _currentStage = CraftStage.Liquid;
        else if (_currentStage == CraftStage.Liquid)
            _currentStage = CraftStage.Topping;
        else if (_currentStage == CraftStage.Topping)
        {
            CompleteCrafting();
            return;
        }

        _stageProgress = 0f;
        NotifyStateChanged();
    }

    void CompleteCrafting()
    {
        _currentStage = CraftStage.Complete;
        _stageProgress = 1f;
        _lastResult = BuildResult();
        OnCraftingCompleted?.Invoke(_lastResult);
        NotifyStateChanged();
    }

    CraftResult BuildResult()
    {
        float payoutMultiplier = GetPayoutMultiplier(_currentSuccessRate);
        int basePayout = CalculateBasePayout();
        int finalPayout = Mathf.RoundToInt(basePayout * payoutMultiplier);

        string qualityLabel = payoutMultiplier >= HighPayoutMultiplier
            ? "우수"
            : payoutMultiplier <= LowPayoutMultiplier
                ? "미흡"
                : "보통";

        return new CraftResult
        {
            SuccessRate = _currentSuccessRate,
            PayoutMultiplier = payoutMultiplier,
            BasePayout = basePayout,
            FinalPayout = finalPayout,
            UsedIngredients = _usedIngredients.ToArray(),
            UsedLevels = _usedLevels.ToArray(),
            SkippedTopping = _skippedTopping,
            Message = UIFontUtility.Sanitize($"제작 완료 ({qualityLabel}) - 성공도 {_currentSuccessRate:0}%")
        };
    }

    void ApplyIngredientSuccessBonus(int level)
    {
        float bonus = level >= 2 ? level2SuccessBonus : level1SuccessBonus;
        _currentSuccessRate = Mathf.Clamp(_currentSuccessRate + bonus, 0f, 100f);
    }

    float GetStageDuration(CraftStage stage, int level)
    {
        float duration = stage switch
        {
            CraftStage.Base => baseStageDuration,
            CraftStage.Liquid => liquidStageDuration,
            CraftStage.Topping => toppingStageDuration,
            _ => 1f
        };

        if (level >= 2)
            duration *= advancedSpeedMultiplier;

        return Mathf.Max(0.3f, duration);
    }

    static float GetPayoutMultiplier(float successRate)
    {
        if (successRate >= HighSuccessThreshold)
            return HighPayoutMultiplier;

        if (successRate < LowSuccessThreshold)
            return LowPayoutMultiplier;

        return 1f;
    }

    int CalculateBasePayout()
    {
        float ingredientValue = 0f;

        for (int i = 0; i < _usedIngredients.Count; i++)
        {
            IngredientSO ingredient = _usedIngredients[i];
            if (ingredient == null)
                continue;

            int level = i < _usedLevels.Count ? _usedLevels[i] : 1;
            float price = Mathf.Max(ingredient.buyPrice, 8);
            float levelMultiplier = level >= 2 ? level2PayoutBonus : 1f;
            ingredientValue += price * levelMultiplier;
        }

        return Mathf.RoundToInt(BaseMenuPayout + ingredientValue);
    }

    static bool CanUseIngredient(IngredientSO ingredient)
    {
        if (ingredient == null || PreparationManager.Instance == null)
            return false;

        if (SupplyManager.Instance != null && !SupplyManager.Instance.CanCraftRequiring(ingredient))
            return false;

        return SupplyManager.Instance == null
            || SupplyManager.Instance.GetTotalStock(ingredient) > 0;
    }

    public static bool IsIngredientValidForStage(IngredientSO ingredient, CraftStage stage)
    {
        if (ingredient == null)
            return false;

        return stage switch
        {
            CraftStage.Base => ingredient.type is IngredientType.Base or IngredientType.DessertBase,
            CraftStage.Liquid => ingredient.type is IngredientType.Milk or IngredientType.Syrup,
            CraftStage.Topping => ingredient.type == IngredientType.Topping,
            _ => false
        };
    }

    public static string GetStageLabel(CraftStage stage)
    {
        return stage switch
        {
            CraftStage.Base => "베이스",
            CraftStage.Liquid => "액체/시럽",
            CraftStage.Topping => "토핑",
            CraftStage.Complete => "완료",
            _ => string.Empty
        };
    }

    void NotifyStateChanged()
    {
        OnCraftingStateChanged?.Invoke();
    }
}