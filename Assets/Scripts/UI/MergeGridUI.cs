using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>3x3 Merge 그리드 UI입니다.</summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class MergeGridUI : MonoBehaviour
{
    [SerializeField] GridLayoutGroup gridLayout;
    [SerializeField] MergeSlotUI[] slots = new MergeSlotUI[PreparationManager.GridSize];

    void Awake()
    {
        ValidateSetup();
    }

    void ValidateSetup()
    {
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (slots == null || slots.Any(slot => slot == null))
            slots = GetComponentsInChildren<MergeSlotUI>(true)
                .OrderBy(slot => slot.SlotIndex)
                .ToArray();

        if (slots.Length != PreparationManager.GridSize)
        {
            Debug.LogError($"[MergeGridUI] MergeSlotUI 9개가 필요합니다. 현재: {slots.Length}");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
            slots[i].Initialize(i, null, null);
    }
}