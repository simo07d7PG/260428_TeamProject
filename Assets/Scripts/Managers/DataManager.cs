using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Resources 폴더의 ScriptableObject를 로드하고 조회하는 싱글톤 매니저입니다.
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    const string IngredientResourcePath = "Ingredients";
    const string MergeRecipeResourcePath = "MergeRecipes";
    const string TrendResourcePath = "Trends";

    readonly List<IngredientSO> _ingredients = new();
    readonly List<MergeRecipeSO> _mergeRecipes = new();
    readonly List<TrendSO> _trends = new();

    public IReadOnlyList<IngredientSO> Ingredients => _ingredients;
    public IReadOnlyList<MergeRecipeSO> MergeRecipes => _mergeRecipes;
    public IReadOnlyList<TrendSO> Trends => _trends;

    public bool IsLoaded { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<DataManager>() != null)
            return;

        var managerObject = new GameObject(nameof(DataManager));
        managerObject.AddComponent<DataManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        _ingredients.AddRange(Resources.LoadAll<IngredientSO>(IngredientResourcePath));
        _mergeRecipes.AddRange(Resources.LoadAll<MergeRecipeSO>(MergeRecipeResourcePath));
        _trends.AddRange(Resources.LoadAll<TrendSO>(TrendResourcePath));

        IsLoaded = true;

        if (_ingredients.Count == 0)
            Debug.LogWarning($"[DataManager] '{IngredientResourcePath}' 경로에 IngredientSO가 없습니다. Resources 폴더에 에셋을 생성해 주세요.");
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
}