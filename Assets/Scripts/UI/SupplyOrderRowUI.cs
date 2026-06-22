using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>발주 UI의 재료 한 줄(이름, 단가, 수량 조절)을 표시합니다.</summary>
public class SupplyOrderRowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] TextMeshProUGUI quantityText;
    [SerializeField] Button decreaseButton;
    [SerializeField] Button increaseButton;

    IngredientSO _ingredient;
    int _quantity;
    Action<IngredientSO, int> _onQuantityChanged;

    public IngredientSO Ingredient => _ingredient;
    public int Quantity => _quantity;

    public void Bind(
        TextMeshProUGUI name,
        TextMeshProUGUI price,
        TextMeshProUGUI quantity,
        Button decrease,
        Button increase)
    {
        nameText = name;
        priceText = price;
        quantityText = quantity;
        decreaseButton = decrease;
        increaseButton = increase;
    }

    public void Initialize(
        IngredientSO ingredient,
        int initialQuantity,
        Action<IngredientSO, int> onQuantityChanged)
    {
        _ingredient = ingredient;
        _quantity = Mathf.Max(0, initialQuantity);
        _onQuantityChanged = onQuantityChanged;

        if (nameText != null)
        {
            nameText.text = UIFontUtility.Sanitize(ingredient != null ? ingredient.ingredientName : "-");
            UIFontUtility.Apply(nameText);
        }

        if (priceText != null)
        {
            priceText.text = UIFontUtility.Sanitize(ingredient != null ? $"{ingredient.buyPrice} Coin" : "-");
            UIFontUtility.Apply(priceText);
        }

        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(Decrease);
        }

        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(Increase);
        }

        Refresh();
    }

    public void SetQuantity(int quantity)
    {
        _quantity = Mathf.Max(0, quantity);
        Refresh();
    }

    /// <summary>단가 줄에 현재 보유 수량을 함께 표시합니다.</summary>
    public void SetStock(int stock)
    {
        if (priceText == null || _ingredient == null)
            return;

        priceText.text = UIFontUtility.Sanitize($"{_ingredient.buyPrice} Coin   보유 {stock}");
    }

    void Increase()
    {
        _quantity++;
        Refresh();
        _onQuantityChanged?.Invoke(_ingredient, _quantity);
    }

    void Decrease()
    {
        if (_quantity <= 0)
            return;

        _quantity--;
        Refresh();
        _onQuantityChanged?.Invoke(_ingredient, _quantity);
    }

    void Refresh()
    {
        if (quantityText != null)
            quantityText.text = _quantity.ToString();
    }
}