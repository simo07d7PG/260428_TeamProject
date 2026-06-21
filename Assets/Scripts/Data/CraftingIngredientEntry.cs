/// <summary>
/// 제작 UI에 표시할 인벤토리 재료 항목입니다.
/// </summary>
public readonly struct CraftingIngredientEntry
{
    public IngredientSO Ingredient { get; }
    public int Level { get; }
    public int Count { get; }

    public CraftingIngredientEntry(IngredientSO ingredient, int level, int count)
    {
        Ingredient = ingredient;
        Level = level;
        Count = count;
    }
}