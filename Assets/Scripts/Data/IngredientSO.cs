using UnityEngine;

/// <summary>카페에서 사용하는 재료의 기본 데이터입니다.</summary>
[CreateAssetMenu(fileName = "NewIngredient", menuName = "CafeSim/Ingredient")]
public class IngredientSO : ScriptableObject
{
    [Header("기본 정보")]
    public string ingredientName;
    public Sprite icon;
    public int buyPrice;

    [Header("속성")]
    [Tooltip("에스프레소 원두, 우유 등 필수 재료 여부")]
    public bool isCritical;
    public IngredientType type;
}