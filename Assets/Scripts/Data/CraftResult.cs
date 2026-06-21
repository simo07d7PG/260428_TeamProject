/// <summary>
/// 3단계 제작 완료 후 계산된 결과입니다.
/// </summary>
public class CraftResult
{
    public float SuccessRate;
    public float PayoutMultiplier;
    public int BasePayout;
    public int FinalPayout;
    public IngredientSO[] UsedIngredients = System.Array.Empty<IngredientSO>();
    public int[] UsedLevels = System.Array.Empty<int>();
    public bool SkippedTopping;
    public string Message = string.Empty;

    public static CraftResult Invalid(string message)
    {
        return new CraftResult { Message = message };
    }
}