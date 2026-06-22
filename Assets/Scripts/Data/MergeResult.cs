/// <summary>Merge 시도 결과 정보입니다.</summary>
public struct MergeResult
{
    public MergeResultType resultType;
    public IngredientSO outputIngredient;
    public int outputLevel;
    public int disposalCostAdded;
    public string message;

    public static MergeResult Invalid(string message)
    {
        return new MergeResult
        {
            resultType = MergeResultType.InvalidSelection,
            message = message
        };
    }

    public static MergeResult Success(IngredientSO output, int level, string message)
    {
        return new MergeResult
        {
            resultType = MergeResultType.Success,
            outputIngredient = output,
            outputLevel = level,
            message = message
        };
    }

    public static MergeResult Failure(int disposalCost, string message)
    {
        return new MergeResult
        {
            resultType = MergeResultType.Failure,
            disposalCostAdded = disposalCost,
            message = message
        };
    }
}