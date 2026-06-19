using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 재료를 Merge 그리드로 드래그할 수 있는 UI 슬롯입니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class MergeInventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI countText;

    IngredientSO _ingredient;
    int _level = 1;
    int _count;

    void Awake()
    {
        ResolveReferences();
    }

    void ResolveReferences()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        countText = InventoryCountTextUtility.EnsureCountText(transform, countText);
    }

    public void Setup(IngredientSO ingredient, int level, int count)
    {
        _ingredient = ingredient;
        _level = level;
        _count = count;

        ResolveReferences();

        if (iconImage != null)
        {
            iconImage.sprite = ingredient != null ? ingredient.icon : null;
            iconImage.enabled = ingredient != null && ingredient.icon != null;
        }

        if (countText != null)
        {
            countText.text = count.ToString();
            countText.gameObject.SetActive(count > 0);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_ingredient == null || _count <= 0)
            return;

        MergeDragContext.Begin(_ingredient, _level);

        Sprite dragSprite = _ingredient != null ? _ingredient.icon : null;
        if (dragSprite == null && iconImage != null)
            dragSprite = iconImage.sprite;
        Vector2 dragSize = ((RectTransform)transform).sizeDelta;
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
}