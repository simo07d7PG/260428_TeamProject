using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject를 로드하고 조회하는 싱글톤 매니저입니다.
/// GameManager와 같은 오브젝트에 자동 장착됩니다.
/// </summary>
[DefaultExecutionOrder(-90)]
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // 에디터 실제 경로 (Assets/ScriptableObject/...)
    static readonly string[] IngredientEditorPaths =
    {
        "Assets/ScriptableObject/Ingredient/LV1",
        "Assets/ScriptableObject/Ingredient/LV2"
    };

    static readonly string[] MergeRecipeEditorPaths =
    {
        "Assets/ScriptableObject/Recipe"
    };

    static readonly string[] TrendEditorPaths =
    {
        "Assets/ScriptableObject/Trends"
    };

    // 빌드용 Resources 상대 경로 (Assets/Resources/... 와 동일 구조 필요)
    static readonly string[] IngredientResourcePaths =
    {
        "ScriptableObject/Ingredient/LV1",
        "ScriptableObject/Ingredient/LV2"
    };

    static readonly string[] MergeRecipeResourcePaths =
    {
        "ScriptableObject/Recipe"
    };

    static readonly string[] TrendResourcePaths =
    {
        "ScriptableObject/Trends"
    };

    readonly List<IngredientSO> _ingredients = new();
    readonly List<MergeRecipeSO> _mergeRecipes = new();
    readonly List<TrendSO> _trends = new();

    public IReadOnlyList<IngredientSO> Ingredients => _ingredients;
    public IReadOnlyList<MergeRecipeSO> MergeRecipes => _mergeRecipes;
    public IReadOnlyList<TrendSO> Trends => _trends;

    public bool IsLoaded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        LoadAllData();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LoadAllData()
    {
        _ingredients.Clear();
        _mergeRecipes.Clear();
        _trends.Clear();

        GameAssetLoader.LoadAll(IngredientEditorPaths, IngredientResourcePaths, _ingredients);
        GameAssetLoader.LoadAll(MergeRecipeEditorPaths, MergeRecipeResourcePaths, _mergeRecipes);
        GameAssetLoader.LoadAll(TrendEditorPaths, TrendResourcePaths, _trends);

        IsLoaded = true;

        if (_ingredients.Count == 0)
            Debug.LogWarning("[DataManager] IngredientSO를 찾지 못했습니다. 경로: Assets/ScriptableObject/Ingredient/LV1, LV2");

        if (_mergeRecipes.Count == 0)
            Debug.LogWarning("[DataManager] MergeRecipeSO를 찾지 못했습니다. 경로: Assets/ScriptableObject/Recipe");

        if (_trends.Count == 0)
            Debug.LogWarning("[DataManager] TrendSO를 찾지 못했습니다. 경로: Assets/ScriptableObject/Trends");
    }

    public IngredientSO GetIngredientByName(string ingredientName)
    {
        return _ingredients.FirstOrDefault(ingredient =>
            ingredient != null && ingredient.ingredientName == ingredientName);
    }

    public IEnumerable<MergeRecipeSO> GetUnlockedRecipes(int currentDay)
    {
        return _mergeRecipes.Where(recipe => recipe != null && recipe.unlockDay <= currentDay);
    }

    public IEnumerable<IngredientSO> GetCriticalIngredients()
    {
        return _ingredients.Where(ingredient => ingredient != null && ingredient.isCritical);
    }

    public IEnumerable<IngredientSO> GetIngredientsByType(IngredientType type)
    {
        return _ingredients.Where(ingredient => ingredient != null && ingredient.type == type);
    }

    public IngredientSO[] GetStarterIngredients()
    {
        IngredientSO[] critical = GetCriticalIngredients().ToArray();
        if (critical.Length > 0)
            return critical;

        return _ingredients
            .Where(ingredient => ingredient != null && ingredient.buyPrice > 0)
            .ToArray();
    }
}