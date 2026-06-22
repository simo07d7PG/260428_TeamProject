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
    [SerializeField] Image headImage;
    [SerializeField] Image faceImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI phraseText;
    [SerializeField] Image patienceFill;
    [SerializeField] Button button;

    Customer _customer;
    Action<Customer> _onClick;
    CustomerMood _lastMood = (CustomerMood)(-1);

    public Customer Customer => _customer;

    public void Bind(Image bg, Image icon, Image head, Image face, TextMeshProUGUI name, TextMeshProUGUI phrase, Image patience, Button btn)
    {
        background = bg;
        iconImage = icon;
        headImage = head;
        faceImage = face;
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
        _lastMood = (CustomerMood)(-1);

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

        // 손님 캐릭터 머리(변형별). 표정은 Refresh에서 인내심/상태에 따라 갱신.
        if (headImage != null)
        {
            Sprite head = ResolveHead(customer != null ? customer.CharacterIndex : 0);
            headImage.sprite = head;
            headImage.enabled = head != null;
        }

        Refresh(false);
    }

    static Sprite ResolveHead(int index)
    {
        Sprite sprite = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GetCustomerHead(index) : null;
        if (sprite == null)
            sprite = Resources.Load<Sprite>($"Characters/NPCHead/NPC{index + 1}_head");
        return sprite;
    }

    static Sprite ResolveFace(CustomerMood mood)
    {
        CafeAssetConfig cfg = CafeAssetConfig.Instance;
        Sprite sprite = cfg != null
            ? (mood == CustomerMood.Happy ? cfg.FaceHappy : mood == CustomerMood.Angry ? cfg.FaceAngry : cfg.FaceDefault)
            : null;
        if (sprite == null)
        {
            string file = mood == CustomerMood.Happy ? "happy" : mood == CustomerMood.Angry ? "angry" : "default";
            sprite = Resources.Load<Sprite>($"Characters/Face/{file}");
        }
        return sprite;
    }

    public void Refresh(bool selected)
    {
        if (_customer == null)
            return;

        if (patienceFill != null)
        {
            float ratio = _customer.PatienceRatio;
            patienceFill.fillAmount = ratio;
            patienceFill.color = CafeTheme.PatienceColor(ratio);
        }

        // 표정: 상태/인내심에 따라 변경(머리 위에 오버레이). 변할 때만 교체.
        if (faceImage != null)
        {
            CustomerMood mood = _customer.Mood;
            if (mood != _lastMood)
            {
                _lastMood = mood;
                Sprite face = ResolveFace(mood);
                faceImage.sprite = face;
                faceImage.enabled = face != null;
            }
        }

        if (background != null)
            background.color = selected ? SelectedBg : NormalBg;
    }

    void HandleClick()
    {
        _onClick?.Invoke(_customer);
    }
}
