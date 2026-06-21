using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 단계에서 선택 가능한 재료 슬롯 UI입니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class CraftingIngredientSlotUI : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] Image backgroundImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI metaText;
    [SerializeField] Button selectButton;

    IngredientSO _ingredient;
    int _level;
    Action<IngredientSO, int> _onSelected;

    void Awake()
    {
        ResolveReferences();
    }

    void ResolveReferences()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();

        if (nameText == null)
            nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();

        if (metaText == null)
            metaText = transform.Find("MetaText")?.GetComponent<TextMeshProUGUI>();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnClicked);
            selectButton.onClick.AddListener(OnClicked);
        }
    }

    public void Bind(
        Image icon,
        Image background,
        TextMeshProUGUI name,
        TextMeshProUGUI meta,
        Button button)
    {
        iconImage = icon;
        backgroundImage = background;
        nameText = name;
        metaText = meta;
        selectButton = button;
        ResolveReferences();
    }

    public void Initialize(CraftingIngredientEntry entry, bool interactable, Action<IngredientSO, int> onSelected)
    {
        _ingredient = entry.Ingredient;
        _level = entry.Level;
        _onSelected = onSelected;

        ResolveReferences();

        if (iconImage != null)
        {
            iconImage.sprite = _ingredient != null ? _ingredient.icon : null;
            iconImage.enabled = _ingredient != null && _ingredient.icon != null;
        }

        if (nameText != null)
        {
            nameText.text = UIFontUtility.Sanitize(_ingredient != null ? _ingredient.ingredientName : "-");
            UIFontUtility.Apply(nameText);
        }

        if (metaText != null)
        {
            string levelLabel = _level >= 2 ? "고급" : "기본";
            metaText.text = UIFontUtility.Sanitize($"Lv{_level} | {levelLabel} | x{entry.Count}");
            UIFontUtility.Apply(metaText);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = _level >= 2
                ? new Color(0.28f, 0.42f, 0.62f, 0.35f)
                : new Color(1f, 1f, 1f, 0.08f);
        }

        if (selectButton != null)
            selectButton.interactable = interactable && entry.Count > 0;
    }

    void OnClicked()
    {
        if (_ingredient == null)
            return;

        _onSelected?.Invoke(_ingredient, _level);
    }
}