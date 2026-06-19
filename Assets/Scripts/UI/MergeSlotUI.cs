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
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!MergeDragContext.IsDragging)
            return;

        if (eventData.pointerEnter == null)
            MergeDragContext.End();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null || !MergeDragContext.IsDragging)
            return;

        if (MergeDragContext.SourceSlotIndex >= 0)
        {
            PreparationManager.Instance.TryMoveSlot(MergeDragContext.SourceSlotIndex, slotIndex);
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
            PreparationManager.Instance.TryRemoveToInventory(slotIndex);
            return;
        }

        PreparationManager.Instance.SelectSlot(slotIndex);
    }

    void HandleSlotSelected(int selectedIndex)
    {
        if (highlightImage == null)
            return;

        highlightImage.enabled = selectedIndex == slotIndex;
        highlightImage.color = selectedColor;
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

        iconImage.color = Color.white;
        iconImage.sprite = item.ingredient != null ? item.ingredient.icon : null;
    }
}