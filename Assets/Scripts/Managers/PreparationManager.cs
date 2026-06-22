using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>준비 단계의 Merge 그리드, 인벤토리, 폐기 비용을 관리하는 싱글톤 매니저입니다.</summary>
[DefaultExecutionOrder(-10)]
public class PreparationManager : MonoBehaviour
{
    public const int GridSize = 9;

    public static PreparationManager Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] MergeGridUI mergeGridUI;
    [SerializeField] MergeUIController mergeUIController;

    [Header("초기 재료 (비어 있으면 DataManager에서 자동 로드)")]
    [SerializeField] IngredientSO[] starterIngredients;
    [SerializeField] int starterCountPerIngredient = 6;

    readonly MergeGridItem[] _grid = new MergeGridItem[GridSize];
    readonly Dictionary<IngredientSO, int> _basicInventory = new();
    readonly List<MergeGridItem> _advancedInventory = new();

    int _selectedSlotIndex = -1;
    int _garbageDisposalCost;
    int _dailyMergeCount;
    int _comboCount;
    float _comboExpireTime;
    bool _goalRewarded;

    const float ComboWindowSeconds = 4f;
    const int BaseDailyMergeGoal = 4;

    public int GarbageDisposalCost => _garbageDisposalCost;
    public int DailyMergeCount => _dailyMergeCount;
    public int SelectedSlotIndex => _selectedSlotIndex;
    public MergeGridUI MergeGridUI => mergeGridUI;
    public MergeUIController MergeUIController => mergeUIController;
    public IReadOnlyDictionary<IngredientSO, int> BasicInventory => _basicInventory;
    public IReadOnlyList<MergeGridItem> AdvancedInventory => _advancedInventory;

    public int CurrentCombo => Time.time <= _comboExpireTime ? _comboCount : 0;
    public int DailyMergeGoal => BaseDailyMergeGoal + Mathf.Max(0, (GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1) - 1);
    public bool IsDailyGoalComplete => _dailyMergeCount >= DailyMergeGoal;

    public event Action OnGridChanged;
    public event Action OnInventoryChanged;
    public event Action<int> OnDisposalCostChanged;
    public event Action<MergeResult> OnMergeCompleted;
    public event Action<int> OnSlotSelected;
    public event Action OnMergeStatsChanged;
    public event Action<int> OnDailyGoalReward;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSupplySystem();
        EnsureServiceSystem();
        BindSceneReferences();
        AutoLoadConfiguration();
        ClearGrid();
        InitializeStarterInventory();
    }

    void Start()
    {
        RefreshInventoryUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureSupplySystem()
    {
        ManagerUtility.GetOrAddComponent<SupplyManager>(gameObject);

        if (FindAnyObjectByType<SupplyUIController>() != null)
            return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject supplyUiHost = new GameObject("SupplyUI", typeof(RectTransform), typeof(SupplyUIController));
        supplyUiHost.transform.SetParent(canvas.transform, false);
        SupplyUIController.ConfigureHostTransform(supplyUiHost.transform as RectTransform);
    }

    void EnsureServiceSystem()
    {
        ManagerUtility.GetOrAddComponent<BeverageBuildManager>(gameObject);
        ManagerUtility.GetOrAddComponent<AudioManager>(gameObject);

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        EnsureUIHost<BeverageServiceUIController>(canvas, "BeverageUI", BeverageServiceUIController.ConfigureHostTransform);
        EnsureUIHost<CustomerQueueUIController>(canvas, "CustomerQueueUI", CustomerQueueUIController.ConfigureHostTransform);
        EnsureUIHost<DayFlowUIController>(canvas, "DayFlowUI", DayFlowUIController.ConfigureHostTransform);
        EnsureUIHost<TrendBannerUIController>(canvas, "TrendBannerUI", TrendBannerUIController.ConfigureHostTransform);
        EnsureUIHost<ClosingUIController>(canvas, "ClosingUI", ClosingUIController.ConfigureHostTransform);
        EnsureUIHost<FeedbackUIController>(canvas, "FeedbackUI", FeedbackUIController.ConfigureHostTransform);
        EnsureUIHost<HelpUIController>(canvas, "HelpUI", HelpUIController.ConfigureHostTransform);
        EnsureUIHost<RecipeBookUIController>(canvas, "RecipeBookUI", RecipeBookUIController.ConfigureHostTransform);
    }

    static void EnsureUIHost<T>(Canvas canvas, string hostName, Action<RectTransform> configure)
        where T : MonoBehaviour
    {
        if (FindAnyObjectByType<T>() != null)
            return;

        GameObject host = new GameObject(hostName, typeof(RectTransform), typeof(T));
        host.transform.SetParent(canvas.transform, false);
        configure?.Invoke(host.transform as RectTransform);
    }

    void BindSceneReferences()
    {
        if (mergeGridUI == null)
            mergeGridUI = FindAnyObjectByType<MergeGridUI>();

        if (mergeUIController == null)
            mergeUIController = FindAnyObjectByType<MergeUIController>();

        if (mergeGridUI == null)
            Debug.LogError("[PreparationManager] MergeGridUI가 연결되지 않았습니다. MergeGridPanel에 배치해 주세요.");

        if (mergeUIController == null)
            Debug.LogError("[PreparationManager] MergeUIController가 연결되지 않았습니다. MergeInfoPanel에 배치해 주세요.");
    }

    void AutoLoadConfiguration()
    {
        if (HasValidStarterIngredients(starterIngredients))
            return;

        if (DataManager.Instance == null || !DataManager.Instance.IsLoaded)
            return;

        starterIngredients = DataManager.Instance.GetStarterIngredients();
    }

    static bool HasValidStarterIngredients(IngredientSO[] ingredients)
    {
        if (ingredients == null || ingredients.Length == 0)
            return false;

        foreach (IngredientSO ingredient in ingredients)
        {
            if (ingredient != null)
                return true;
        }

        return false;
    }

    void ClearGrid()
    {
        for (int i = 0; i < GridSize; i++)
            _grid[i] = MergeGridItem.Empty;
    }

    void InitializeStarterInventory()
    {
        int qty = Mathf.Max(10, starterCountPerIngredient);

        if (DataManager.Instance != null && DataManager.Instance.IsLoaded)
        {
            bool stockedAny = false;
            foreach (IngredientSO ingredient in DataManager.Instance.GetOrderableIngredients())
            {
                if (ingredient == null)
                    continue;

                AddToInventory(ingredient, 1, qty);
                stockedAny = true;
            }

            if (stockedAny)
                return;
        }

        if (!HasValidStarterIngredients(starterIngredients))
            AutoLoadConfiguration();

        if (!HasValidStarterIngredients(starterIngredients))
        {
            Debug.LogWarning("[PreparationManager] starterIngredients가 비어 있습니다. PrepSystem Inspector 또는 DataManager SO를 확인해 주세요.");
            return;
        }

        foreach (IngredientSO ingredient in starterIngredients)
        {
            if (ingredient == null)
                continue;

            AddToInventory(ingredient, 1, qty);
        }
    }

    void RefreshInventoryUI()
    {
        if (mergeUIController == null)
            mergeUIController = FindAnyObjectByType<MergeUIController>();

        mergeUIController?.RefreshAll();
    }

    public MergeGridItem GetSlot(int index)
    {
        if (!IsValidSlot(index))
            return MergeGridItem.Empty;

        return _grid[index]?.Clone() ?? MergeGridItem.Empty;
    }

    public bool TryPlaceFromInventory(int slotIndex, IngredientSO ingredient, int level)
    {
        if (!IsValidSlot(slotIndex) || ingredient == null || !_grid[slotIndex].IsEmpty)
            return false;

        if (!TryConsumeFromInventory(ingredient, level))
            return false;

        _grid[slotIndex] = MergeGridItem.Create(ingredient, level);
        ClearSelection();
        OnGridChanged?.Invoke();
        return true;
    }

    public bool TryMoveSlot(int fromIndex, int toIndex)
    {
        if (!IsValidSlot(fromIndex) || !IsValidSlot(toIndex) || fromIndex == toIndex)
            return false;

        if (_grid[fromIndex].IsEmpty)
            return false;

        if (_grid[toIndex].IsEmpty)
        {
            _grid[toIndex] = _grid[fromIndex];
            _grid[fromIndex] = MergeGridItem.Empty;
        }
        else
        {
            MergeGridItem temp = _grid[toIndex];
            _grid[toIndex] = _grid[fromIndex];
            _grid[fromIndex] = temp;
        }

        ClearSelection();
        OnGridChanged?.Invoke();
        return true;
    }

    public bool TryRemoveToInventory(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || _grid[slotIndex].IsEmpty)
            return false;

        MergeGridItem item = _grid[slotIndex];
        if (item.isGarbage)
            return false;

        AddToInventory(item.ingredient, item.level, 1);
        _grid[slotIndex] = MergeGridItem.Empty;
        ClearSelection();
        OnGridChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryDisposeGarbage(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || !_grid[slotIndex].isGarbage)
            return false;

        _grid[slotIndex] = MergeGridItem.Empty;
        ClearSelection();
        OnGridChanged?.Invoke();
        return true;
    }

    public void SelectSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (_selectedSlotIndex == slotIndex)
        {
            ClearSelection();
            return;
        }

        if (_selectedSlotIndex < 0)
        {
            if (_grid[slotIndex].IsEmpty)
                return;

            _selectedSlotIndex = slotIndex;
            OnSlotSelected?.Invoke(_selectedSlotIndex);
            return;
        }

        int firstIndex = _selectedSlotIndex;
        MergeResult result = TryMergeSlots(firstIndex, slotIndex);
        ClearSelection();
        OnMergeCompleted?.Invoke(result);
    }

    public MergeResult TryMergeSlots(int firstIndex, int secondIndex)
    {
        if (!IsValidSlot(firstIndex) || !IsValidSlot(secondIndex))
            return MergeResult.Invalid("유효하지 않은 슬롯입니다.");

        if (firstIndex == secondIndex)
            return MergeResult.Invalid("같은 슬롯은 병합할 수 없습니다.");

        MergeGridItem first = _grid[firstIndex];
        MergeGridItem second = _grid[secondIndex];

        if (!first.CanMergeWith(second))
            return MergeResult.Invalid("같은 재료와 같은 레벨만 병합할 수 있습니다.");

        _grid[firstIndex] = MergeGridItem.Empty;
        _grid[secondIndex] = MergeGridItem.Empty;

        MergeRecipeSO recipe = FindMatchingRecipe(first.ingredient, first.level);
        if (recipe != null)
        {
            _dailyMergeCount++;
            AddToInventory(recipe.outputIngredient, recipe.outputLevel, 1);

            _comboCount = Time.time <= _comboExpireTime ? _comboCount + 1 : 1;
            _comboExpireTime = Time.time + ComboWindowSeconds;

            int comboBonus = 0;
            if (_comboCount >= 2 && GameManager.Instance != null)
            {
                comboBonus = 2 * (_comboCount - 1);
                GameManager.Instance.Coin += comboBonus;
            }

            int goalReward = TryAwardDailyGoal();

            OnGridChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            OnMergeStatsChanged?.Invoke();

            string combo = _comboCount >= 2 ? $" 콤보x{_comboCount}!" : string.Empty;
            string bonus = comboBonus > 0 ? $" (+{comboBonus})" : string.Empty;
            string goal = goalReward > 0 ? $"  ★목표 달성 +{goalReward} Coin" : string.Empty;

            return MergeResult.Success(
                recipe.outputIngredient,
                recipe.outputLevel,
                $"{recipe.outputIngredient.ingredientName} 제작 성공!{combo}{bonus}{goal}");
        }

        _comboCount = 0;
        int disposalCost = MergeGridItem.GarbageDisposalCost;
        _garbageDisposalCost += disposalCost;
        PlaceGarbageOnGrid();
        OnGridChanged?.Invoke();
        OnDisposalCostChanged?.Invoke(_garbageDisposalCost);
        OnMergeStatsChanged?.Invoke();

        return MergeResult.Failure(disposalCost, "알 수 없는 조합입니다. 쓰레기가 생성되었습니다.");
    }

    int TryAwardDailyGoal()
    {
        if (_goalRewarded || _dailyMergeCount < DailyMergeGoal || GameManager.Instance == null)
            return 0;

        _goalRewarded = true;
        int reward = 40 + 10 * Mathf.Max(0, GameManager.Instance.CurrentDay - 1);
        GameManager.Instance.Coin += reward;
        OnDailyGoalReward?.Invoke(reward);
        return reward;
    }

    public MergeResult MergeSlotsAndNotify(int firstIndex, int secondIndex)
    {
        MergeResult result = TryMergeSlots(firstIndex, secondIndex);
        ClearSelection();
        OnMergeCompleted?.Invoke(result);
        return result;
    }

    public int MergeAllPairs()
    {
        int merges = 0;
        int safety = 0;
        bool merged = true;

        while (merged && safety++ < GridSize * GridSize)
        {
            merged = false;
            for (int i = 0; i < GridSize && !merged; i++)
            {
                if (_grid[i].IsEmpty || _grid[i].isGarbage)
                    continue;

                for (int j = i + 1; j < GridSize; j++)
                {
                    if (!_grid[i].CanMergeWith(_grid[j]))
                        continue;
                    if (FindMatchingRecipe(_grid[i].ingredient, _grid[i].level) == null)
                        continue;

                    if (TryMergeSlots(i, j).resultType == MergeResultType.Success)
                        merges++;
                    merged = true;
                    break;
                }
            }
        }

        MergeResult summary = merges > 0
            ? MergeResult.Success(null, 0, $"자동 합성 {merges}회 완료!" + (CurrentCombo >= 2 ? $" 콤보x{CurrentCombo}!" : string.Empty))
            : MergeResult.Invalid("합칠 수 있는 짝이 없습니다.");
        OnMergeCompleted?.Invoke(summary);
        return merges;
    }

    public void AddToInventory(IngredientSO ingredient, int level, int count)
    {
        if (ingredient == null || count <= 0)
            return;

        if (level >= 2)
        {
            for (int i = 0; i < count; i++)
                _advancedInventory.Add(MergeGridItem.Create(ingredient, level));
        }
        else
        {
            if (_basicInventory.ContainsKey(ingredient))
                _basicInventory[ingredient] += count;
            else
                _basicInventory[ingredient] = count;
        }

        OnInventoryChanged?.Invoke();
    }

    public bool TryConsumeFromInventory(IngredientSO ingredient, int level)
    {
        if (ingredient == null)
            return false;

        if (level >= 2)
        {
            int index = _advancedInventory.FindIndex(item =>
                item.ingredient == ingredient && item.level == level);

            if (index < 0)
                return false;

            _advancedInventory.RemoveAt(index);
            OnInventoryChanged?.Invoke();
            return true;
        }

        if (!_basicInventory.TryGetValue(ingredient, out int count) || count <= 0)
            return false;

        count--;
        if (count <= 0)
            _basicInventory.Remove(ingredient);
        else
            _basicInventory[ingredient] = count;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetInventoryCount(IngredientSO ingredient, int level)
    {
        if (ingredient == null)
            return 0;

        if (level >= 2)
            return _advancedInventory.Count(item => item.ingredient == ingredient && item.level == level);

        return _basicInventory.TryGetValue(ingredient, out int count) ? count : 0;
    }

    public void ResetForNewDay()
    {
        ClearGrid();
        ClearSelection();
        _basicInventory.Clear();
        _advancedInventory.Clear();
        _garbageDisposalCost = 0;
        _dailyMergeCount = 0;
        _comboCount = 0;
        _comboExpireTime = 0f;
        _goalRewarded = false;
        InitializeStarterInventory();
        SupplyManager.Instance?.ResetDailyOrder();
        BeverageBuildManager.Instance?.ResetSession();
        OnGridChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        OnDisposalCostChanged?.Invoke(_garbageDisposalCost);
        OnMergeStatsChanged?.Invoke();
    }

    MergeRecipeSO FindMatchingRecipe(IngredientSO ingredient, int level)
    {
        if (DataManager.Instance == null || GameManager.Instance == null)
            return null;

        foreach (MergeRecipeSO recipe in DataManager.Instance.GetUnlockedRecipes(GameManager.Instance.CurrentDay))
        {
            if (recipe != null && IsRecipeMatch(recipe, ingredient, level))
                return recipe;
        }

        return null;
    }

    static bool IsRecipeMatch(MergeRecipeSO recipe, IngredientSO ingredient, int level)
    {
        if (recipe.inputIngredients == null || recipe.inputIngredients.Length < 2)
            return false;

        foreach (IngredientSO input in recipe.inputIngredients)
        {
            if (input != ingredient)
                return false;
        }

        return recipe.outputLevel == level + 1;
    }

    void PlaceGarbageOnGrid()
    {
        for (int i = 0; i < GridSize; i++)
        {
            if (!_grid[i].IsEmpty)
                continue;

            _grid[i] = MergeGridItem.CreateGarbage();
            return;
        }

        Debug.LogWarning("[PreparationManager] 그리드가 가득 차 쓰레기를 배치하지 못했습니다. 폐기 비용만 누적됩니다.");
    }

    void ClearSelection()
    {
        _selectedSlotIndex = -1;
        OnSlotSelected?.Invoke(-1);
    }

    static bool IsValidSlot(int index) => index >= 0 && index < GridSize;
}