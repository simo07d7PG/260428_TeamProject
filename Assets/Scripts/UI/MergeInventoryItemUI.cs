using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 재료를 Merge 그리드로 드래그할 수 있는 UI 슬롯입니다.
/// Image, Count 텍스트를 자동 탐색합니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class MergeInventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image iconImage;
    [SerializeField] Text countText;

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

        if (countText == null)
            countText = transform.Find("Count")?.GetComponent<Text>();
    }

    public void Initialize(Image icon, Text countLabel)
    {
        if (icon != null)
            iconImage = icon;

        if (countLabel != null)
            countText = countLabel;

        ResolveReferences();
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
            countText.text = count.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_ingredient == null || _count <= 0)
            return;

        MergeDragContext.Begin(_ingredient, _level);
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
}