using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대기열의 손님 한 명을 표시하는 카드입니다. 아이콘, 주문 대사, 인내심 바, 선택 버튼을 묶습니다.
/// </summary>
public class CustomerCardUI : MonoBehaviour
{
    static readonly Color NormalBg = new(0.16f, 0.18f, 0.22f, 0.96f);
    static readonly Color SelectedBg = new(0.26f, 0.42f, 0.62f, 0.98f);

    [SerializeField] Image background;
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI phraseText;
    [SerializeField] Image patienceFill;
    [SerializeField] Button button;

    Customer _customer;
    Action<Customer> _onClick;

    public Customer Customer => _customer;

    public void Bind(Image bg, Image icon, TextMeshProUGUI name, TextMeshProUGUI phrase, Image patience, Button btn)
    {
        background = bg;
        iconImage = icon;
        nameText = name;
        phraseText = phrase;
        patienceFill = patience;
        button = btn;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Set(Customer customer, Action<Customer> onClick)
    {
        _customer = customer;
        _onClick = onClick;

        if (nameText != null)
            nameText.text = UIFontUtility.Sanitize(customer?.Order?.MenuName ?? string.Empty);

        if (phraseText != null)
            phraseText.text = UIFontUtility.Sanitize(customer?.Order?.phrase ?? string.Empty);

        if (iconImage != null)
        {
            Sprite icon = customer?.Order?.menu?.icon;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        Refresh(false);
    }

    public void Refresh(bool selected)
    {
        if (_customer == null)
            return;

        if (patienceFill != null)
        {
            float ratio = _customer.PatienceRatio;
            patienceFill.fillAmount = ratio;
            patienceFill.color = ratio > 0.5f
                ? new Color(0.35f, 0.8f, 0.4f, 1f)
                : ratio > 0.25f
                    ? new Color(0.95f, 0.8f, 0.3f, 1f)
                    : new Color(0.9f, 0.35f, 0.3f, 1f);
        }

        if (background != null)
            background.color = selected ? SelectedBg : NormalBg;
    }

    void HandleClick()
    {
        _onClick?.Invoke(_customer);
    }
}
