using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3x3 Merge 그리드 UI를 구성하고 PreparationManager와 연결합니다.
/// 슬롯과 GridLayoutGroup을 자동 탐색·생성합니다.
/// </summary>
public class MergeGridUI : MonoBehaviour
{
    [SerializeField] GridLayoutGroup gridLayout;
    [SerializeField] MergeSlotUI[] slots = new MergeSlotUI[PreparationManager.GridSize];
    [SerializeField] bool buildRuntimeGridIfMissing = true;
    [SerializeField] Vector2 cellSize = new Vector2(96f, 96f);
    [SerializeField] Sprite slotBackgroundSprite;

    void Awake()
    {
        ResolveAndBuild();
    }

    void ResolveAndBuild()
    {
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (gridLayout == null)
            gridLayout = gameObject.AddComponent<GridLayoutGroup>();

        MergeSlotUI[] childSlots = GetComponentsInChildren<MergeSlotUI>(true);
        if (childSlots.Length == PreparationManager.GridSize)
            slots = childSlots.OrderBy(slot => slot.SlotIndex).ToArray();

        if (buildRuntimeGridIfMissing && !HasValidSlots())
            BuildRuntimeGrid();
        else
            BindExistingSlots();
    }

    bool HasValidSlots()
    {
        if (slots == null || slots.Length != PreparationManager.GridSize)
            return false;

        foreach (MergeSlotUI slot in slots)
        {
            if (slot == null)
                return false;
        }

        return true;
    }

    void BindExistingSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].Initialize(i, null, null);
        }
    }

    void BuildRuntimeGrid()
    {
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(8f, 8f);

        slots = new MergeSlotUI[PreparationManager.GridSize];

        for (int i = 0; i < PreparationManager.GridSize; i++)
            slots[i] = CreateSlot(i);
    }

    MergeSlotUI CreateSlot(int index)
    {
        GameObject slotObject = new GameObject(
            $"MergeSlot_{index}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(MergeSlotUI));
        slotObject.transform.SetParent(gridLayout.transform, false);

        Image background = slotObject.GetComponent<Image>();
        background.color = new Color(0.2f, 0.18f, 0.16f, 0.95f);
        if (slotBackgroundSprite != null)
            background.sprite = slotBackgroundSprite;

        GameObject highlightObject = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
        highlightObject.transform.SetParent(slotObject.transform, false);
        RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;
        Image highlightImage = highlightObject.GetComponent<Image>();
        highlightImage.color = new Color(1f, 0.92f, 0.4f, 0.8f);
        highlightImage.enabled = false;
        highlightImage.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.enabled = false;
        iconImage.raycastTarget = false;

        MergeSlotUI slot = slotObject.GetComponent<MergeSlotUI>();
        slot.Initialize(index, iconImage, highlightImage);
        return slot;
    }
}