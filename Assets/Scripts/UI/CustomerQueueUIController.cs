using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영업 중 손님 대기열을 화면 오른쪽에 표시하고 CustomerManager와 연동합니다.
/// 카드 클릭으로 손님을 선택하며, 인내심 바를 매 프레임 갱신합니다.
/// </summary>
public class CustomerQueueUIController : MonoBehaviour
{
    const int MaxCards = 4;
    const float CardWidth = 240f;
    const float CardHeight = 86f;

    RectTransform _panelRoot;
    TextMeshProUGUI _counterText;
    readonly List<CustomerCardUI> _cards = new();
    GameState _lastVisibleState = (GameState)(-1);

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        EnsureManagers();
        BuildPanel();
        UIFontUtility.ApplyToHierarchy(transform);
    }

    void OnEnable()
    {
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnQueueChanged += RebuildBinding;
            CustomerManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;

        RebuildBinding();
        RefreshVisibility();
    }

    void OnDisable()
    {
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnQueueChanged -= RebuildBinding;
            CustomerManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        RefreshVisibility();

        Customer selected = CustomerManager.Instance?.Selected;
        foreach (CustomerCardUI card in _cards)
        {
            if (card != null && card.gameObject.activeSelf && card.Customer != null)
                card.Refresh(card.Customer == selected);
        }
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && CustomerManager.Instance == null)
            ManagerUtility.GetOrAddComponent<CustomerManager>(PreparationManager.Instance.gameObject);
    }

    void BuildPanel()
    {
        GameObject panelObject = UIFactoryUtility.CreateUIObject(
            "CustomerQueuePanel", transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        _panelRoot = panelObject.GetComponent<RectTransform>();
        _panelRoot.anchorMin = new Vector2(1f, 1f);
        _panelRoot.anchorMax = new Vector2(1f, 1f);
        _panelRoot.pivot = new Vector2(1f, 1f);
        _panelRoot.anchoredPosition = new Vector2(-16f, -64f);
        _panelRoot.sizeDelta = new Vector2(CardWidth + 16f, 0f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _counterText = UIFactoryUtility.CreateLabel(_panelRoot, "HeaderText", "대기 손님", 18f);
        _counterText.alignment = TextAlignmentOptions.Right;
        AddLayoutElement(_counterText.rectTransform, CardWidth, 28f);

        for (int i = 0; i < MaxCards; i++)
        {
            CustomerCardUI card = CreateCard(_panelRoot);
            card.gameObject.SetActive(false);
            _cards.Add(card);
        }
    }

    CustomerCardUI CreateCard(RectTransform parent)
    {
        GameObject cardObject = UIFactoryUtility.CreateUIObject(
            "CustomerCard", parent, typeof(Image), typeof(Button), typeof(CustomerCardUI));
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
        AddLayoutElement(cardRect, CardWidth, CardHeight);

        Image background = cardObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.18f, 0.22f, 0.96f);

        Image icon = UIFactoryUtility.CreateImage(cardRect, "Icon", Color.white);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 8f);
        iconRect.sizeDelta = new Vector2(52f, 52f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        TextMeshProUGUI phrase = UIFactoryUtility.CreateLabel(cardRect, "PhraseText", string.Empty, 15f);
        phrase.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform phraseRect = phrase.rectTransform;
        phraseRect.anchorMin = new Vector2(0f, 1f);
        phraseRect.anchorMax = new Vector2(1f, 1f);
        phraseRect.pivot = new Vector2(0f, 1f);
        phraseRect.offsetMin = new Vector2(72f, -42f);
        phraseRect.offsetMax = new Vector2(-10f, -8f);

        Image patienceFill = UIFactoryUtility.CreateFilledBar(
            cardRect, "PatienceBar",
            new Color(0f, 0f, 0f, 0.35f),
            new Color(0.35f, 0.8f, 0.4f, 1f));
        RectTransform barRect = patienceFill.rectTransform.parent as RectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = new Vector2(72f, 12f);
        barRect.offsetMax = new Vector2(-10f, 24f);

        CustomerCardUI card = cardObject.GetComponent<CustomerCardUI>();
        card.Bind(background, icon, phrase, patienceFill, cardObject.GetComponent<Button>());
        return card;
    }

    static void AddLayoutElement(RectTransform rect, float width, float height)
    {
        LayoutElement layout = ManagerUtility.GetOrAddComponent<LayoutElement>(rect.gameObject);
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    void RebuildBinding()
    {
        CustomerManager manager = CustomerManager.Instance;
        IReadOnlyList<Customer> queue = manager?.Queue;
        Customer selected = manager?.Selected;

        for (int i = 0; i < _cards.Count; i++)
        {
            CustomerCardUI card = _cards[i];
            if (card == null)
                continue;

            if (queue != null && i < queue.Count)
            {
                card.gameObject.SetActive(true);
                card.Set(queue[i], OnCardClicked);
                card.Refresh(queue[i] == selected);
            }
            else
            {
                card.gameObject.SetActive(false);
            }
        }

        RefreshCounter();
    }

    void RefreshCounter()
    {
        if (_counterText == null || CustomerManager.Instance == null)
            return;

        CustomerManager manager = CustomerManager.Instance;
        _counterText.text = UIFontUtility.Sanitize(
            $"손님 {manager.ServedCount + manager.LeftCount}/{manager.CustomersPerDay} (대기 {manager.Queue.Count})");
    }

    void OnCardClicked(Customer customer)
    {
        CustomerManager.Instance?.Select(customer);
    }

    void HandleSelectionChanged(Customer selected)
    {
        foreach (CustomerCardUI card in _cards)
        {
            if (card != null && card.gameObject.activeSelf)
                card.Refresh(card.Customer == selected);
        }
    }

    void HandleStateChanged(GameState state)
    {
        RefreshVisibility();
        RebuildBinding();
    }

    void RefreshVisibility()
    {
        if (GameManager.Instance == null || _panelRoot == null)
            return;

        GameState state = GameManager.Instance.CurrentState;
        if (_lastVisibleState == state)
            return;

        _lastVisibleState = state;
        _panelRoot.gameObject.SetActive(state == GameState.Service);
    }
}
