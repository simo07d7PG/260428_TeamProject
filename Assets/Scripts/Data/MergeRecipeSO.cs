using UnityEngine;

/// <summary>Merge 그리드에서 두 재료를 조합할 때 사용하는 레시피 데이터입니다.</summary>
[CreateAssetMenu(fileName = "NewMergeRecipe", menuName = "CafeSim/Merge Recipe")]
public class MergeRecipeSO : ScriptableObject
{
    [Header("입력")]
    [Tooltip("보통 2개의 동일 재료를 사용합니다.")]
    public IngredientSO[] inputIngredients;

    [Header("결과")]
    public IngredientSO outputIngredient;
    public int outputLevel;
    public string effectDescription;

    [Header("해금")]
    public int unlockDay;
}