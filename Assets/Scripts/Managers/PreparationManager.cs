using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 준비 단계의 Merge 그리드, 인벤토리, 폐기 비용을 관리하는 싱글톤 매니저입니다.
/// UI는 씬에 직접 배치하고 Inspector에서 연결합니다.
/// </summary>
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

    public int GarbageDisposalCost => _garbageDisposalCost;
    public int SelectedSlotIndex => _selectedSlotIndex;
    public MergeGridUI MergeGridUI => mergeGridUI;
    public MergeUIController MergeUIController => mergeUIController;
    public IReadOnlyDictionary<IngredientSO, int> BasicInventory => _basicInventory;
    public IReadOnlyList<MergeGridItem> AdvancedInventory => _advancedInventory;

    public event Action OnGridChanged;
    public event Action OnInventoryChanged;
    public event Action<int> OnDisposalCostChanged;
    public event Action<MergeResult> OnMergeCompleted;
    public event Action<int> OnSlotSelected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BindSceneReferences();
        AutoLoadConfiguration();
        ClearGrid();
        InitializeStarterInventory();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        if (starterIngredients != null && starterIngredients.Length > 0)
            return;

        if (DataManager.Instance == null || !DataManager.Instance.IsLoaded)
            return;

        starterIngredients = DataManager.Instance.GetStarterIngredients();
    }

    void ClearGrid()
    {
        for (int i = 0; i < GridSize; i++)
            _grid[i] = MergeGridItem.Empty;
    }

    void InitializeStarterInventory()
    {
        if (starterIngredients == null || starterIngredients.Length == 0)
            return;

        foreach (IngredientSO ingredient in starterIngredients)
        {
            if (ingredient == null)
                continue;

            AddToInventory(ingredient, 1, starterCountPerIngredient);
        }
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
            AddToInventory(recipe.outputIngredient, recipe.outputLevel, 1);
            OnGridChanged?.Invoke();
            OnInventoryChanged?.Invoke();

            return MergeResult.Success(
                recipe.outputIngredient,
                recipe.outputLevel,
                $"{recipe.outputIngredient.ingredientName} 제작 성공!");
        }

        int disposalCost = MergeGridItem.GarbageDisposalCost;
        _garbageDisposalCost += disposalCost;
        PlaceGarbageOnGrid();
        OnGridChanged?.Invoke();
        OnDisposalCostChanged?.Invoke(_garbageDisposalCost);

        return MergeResult.Failure(disposalCost, "알 수 없는 조합입니다. 쓰레기가 생성되었습니다.");
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
        InitializeStarterInventory();
        OnGridChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        OnDisposalCostChanged?.Invoke(_garbageDisposalCost);
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