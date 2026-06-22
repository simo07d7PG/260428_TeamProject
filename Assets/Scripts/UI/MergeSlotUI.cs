using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Merge 그리드의 개별 슬롯 UI입니다.
/// Icon, Highlight, Button을 자동 탐색·장착합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class MergeSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] int slotIndex;
    [SerializeField] Image iconImage;
    [SerializeField] Image highlightImage;
    [SerializeField] Color selectedColor = new Color(1f, 0.92f, 0.4f, 0.8f);
    [SerializeField] Color garbageColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] Color hintColor = new Color(0.4f, 0.9f, 0.5f, 0.7f); // 병합 가능 힌트
    static readonly Color PremiumTint = new Color(1f, 0.92f, 0.55f, 1f);   // Lv2 프리미엄 강조

    public int SlotIndex => slotIndex;

    void Awake()
    {
        ResolveReferences();
    }

    void ResolveReferences()
    {
        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();

        if (highlightImage == null)
            highlightImage = transform.Find("Highlight")?.GetComponent<Image>();

        ManagerUtility.GetOrAddComponent<Button>(gameObject);
    }

    void OnEnable()
    {
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnGridChanged += Refresh;
            PreparationManager.Instance.OnSlotSelected += HandleSlotSelected;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnGridChanged -= Refresh;
            PreparationManager.Instance.OnSlotSelected -= HandleSlotSelected;
        }
    }

    public void Initialize(int index, Image icon, Image highlight)
    {
        slotIndex = index;

        if (icon != null)
            iconImage = icon;

        if (highlight != null)
            highlightImage = highlight;

        ResolveReferences();
        Refresh();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null)
            return;

        MergeGridItem item = PreparationManager.Instance.GetSlot(slotIndex);
        if (item.IsEmpty || item.isGarbage)
            return;

        MergeDragContext.Begin(item.ingredient, item.level, slotIndex);

        Sprite dragSprite = iconImage != null ? iconImage.sprite : null;
        Vector2 dragSize = iconImage != null
            ? ((RectTransform)iconImage.transform).sizeDelta
            : ((RectTransform)transform).sizeDelta;
        CanvasGroup canvasGroup = ManagerUtility.GetOrAddComponent<CanvasGroup>(gameObject);
        MergeDragVisualizer.Begin(dragSprite, eventData, dragSize, canvasGroup);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MergeDragVisualizer.UpdatePosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MergeDragContext.EndIfDragging();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null || !MergeDragContext.IsDragging)
            return;

        if (MergeDragContext.SourceSlotIndex >= 0)
        {
            int from = MergeDragContext.SourceSlotIndex;
            if (from != slotIndex)
            {
                // 끌어다 겹친 칸이 병합 가능하면 합치고(드래그-투-머지), 아니면 이동/스왑.
                MergeGridItem dragged = PreparationManager.Instance.GetSlot(from);
                MergeGridItem target = PreparationManager.Instance.GetSlot(slotIndex);
                if (dragged.CanMergeWith(target))
                    PreparationManager.Instance.MergeSlotsAndNotify(from, slotIndex);
                else
                    PreparationManager.Instance.TryMoveSlot(from, slotIndex);
            }
            MergeDragContext.End();
            return;
        }

        PreparationManager.Instance.TryPlaceFromInventory(
            slotIndex,
            MergeDragContext.Ingredient,
            MergeDragContext.Level);
        MergeDragContext.End();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            MergeGridItem item = PreparationManager.Instance.GetSlot(slotIndex);
            if (item.isGarbage)
                PreparationManager.Instance.TryDisposeGarbage(slotIndex);
            else
                PreparationManager.Instance.TryRemoveToInventory(slotIndex);

            return;
        }

        PreparationManager.Instance.SelectSlot(slotIndex);
    }

    void HandleSlotSelected(int selectedIndex)
    {
        if (highlightImage == null)
            return;

        if (selectedIndex == slotIndex)
        {
            highlightImage.enabled = true;
            highlightImage.color = selectedColor;
            return;
        }

        // 선택한 칸과 합칠 수 있는 칸을 살짝 강조(병합 가능 힌트).
        if (selectedIndex >= 0 && PreparationManager.Instance != null)
        {
            MergeGridItem selected = PreparationManager.Instance.GetSlot(selectedIndex);
            MergeGridItem mine = PreparationManager.Instance.GetSlot(slotIndex);
            if (mine.CanMergeWith(selected))
            {
                highlightImage.enabled = true;
                highlightImage.color = hintColor;
                return;
            }
        }

        highlightImage.enabled = false;
    }

    void Refresh()
    {
        if (PreparationManager.Instance == null)
            return;

        MergeGridItem item = PreparationManager.Instance.GetSlot(slotIndex);

        if (iconImage == null)
            return;

        if (item.IsEmpty)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            return;
        }

        iconImage.enabled = true;

        if (item.isGarbage)
        {
            iconImage.sprite = null;
            iconImage.color = garbageColor;
            return;
        }

        iconImage.color = item.level >= 2 ? PremiumTint : Color.white; // Lv2 프리미엄 강조
        iconImage.sprite = item.ingredient != null ? item.ingredient.icon : null;
    }
}