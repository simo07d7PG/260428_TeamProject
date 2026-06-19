using UnityEngine;

/// <summary>
/// Merge UI 드래그 중 공유되는 데이터입니다.
/// </summary>
public static class MergeDragContext
{
    public static IngredientSO Ingredient { get; private set; }
    public static int Level { get; private set; } = 1;
    public static int SourceSlotIndex { get; private set; } = -1;
    public static bool IsDragging { get; private set; }

    public static void Begin(IngredientSO ingredient, int level, int sourceSlotIndex = -1)
    {
        Ingredient = ingredient;
        Level = level;
        SourceSlotIndex = sourceSlotIndex;
        IsDragging = true;
    }

    public static void End()
    {
        MergeDragVisualizer.End();

        Ingredient = null;
        Level = 1;
        SourceSlotIndex = -1;
        IsDragging = false;
    }

    /// <summary>
    /// OnEndDrag에서 호출합니다. 유효한 드롭 대상이 없을 때 고스트/UI 상태를 정리합니다.
    /// OnDrop이 먼저 성공하면 IsDragging이 false이므로 중복 정리되지 않습니다.
    /// </summary>
    public static void EndIfDragging()
    {
        if (!IsDragging)
            return;

        End();
    }
}