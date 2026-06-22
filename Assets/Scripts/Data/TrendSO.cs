using UnityEngine;

/// <summary>특정 재료나 메뉴에 배수를 적용하는 트렌드 데이터입니다.</summary>
[CreateAssetMenu(fileName = "NewTrend", menuName = "CafeSim/Trend")]
public class TrendSO : ScriptableObject
{
    [Header("기본 정보")]
    public string trendName;
    public int durationDays = 1;
    public float multiplier = 1.5f;

    [Header("영향 대상")]
    public IngredientSO[] affectedIngredients;
}