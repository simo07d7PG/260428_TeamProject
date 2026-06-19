using System;

/// <summary>
/// Merge 그리드 슬롯 또는 인벤토리에 배치되는 재료 인스턴스입니다.
/// </summary>
[Serializable]
public class MergeGridItem
{
    public const int GarbageDisposalCost = 25;

    public IngredientSO ingredient;
    public int level;
    public bool isGarbage;

    public bool IsEmpty => !isGarbage && ingredient == null;

    public static MergeGridItem Empty => new MergeGridItem();

    public static MergeGridItem Create(IngredientSO ingredient, int level)
    {
        return new MergeGridItem
        {
            ingredient = ingredient,
            level = level,
            isGarbage = false
        };
    }

    public static MergeGridItem CreateGarbage()
    {
        return new MergeGridItem
        {
            ingredient = null,
            level = 0,
            isGarbage = true
        };
    }

    public bool CanMergeWith(MergeGridItem other)
    {
        if (other == null || IsEmpty || other.IsEmpty || isGarbage || other.isGarbage)
            return false;

        return ingredient == other.ingredient && level == other.level;
    }

    public MergeGridItem Clone()
    {
        return new MergeGridItem
        {
            ingredient = ingredient,
            level = level,
            isGarbage = isGarbage
        };
    }
}