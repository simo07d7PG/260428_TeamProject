using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>영업 중 손님 대기열을 표시하고 CustomerManager와 연동합니다.</summary>
public class CustomerQueueUIController : MonoBehaviour
{
    const int MaxCards = 4;
    const float CardWidth = 248f;
    const float CardHeight = 96f;

    RectTransform _panelRoot;
    RectTransform _rowRoot;
    TextMeshProUGUI _counterText;
    readonly List<CustomerCardUI> _cards = new();
    GameState _lastVisibleState = (GameState)(-1);

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        EnsureManagers();

        RectTransform existing = transform.Find("CustomerQueuePanel") as RectTransform;
        if (existing != null)
            BindPanel(existing);
        else
            BuildPanel();

        UIFontUtility.ApplyToHierarchy(transform);
    }

    /// <summary>에디터에서 씬에 패널을 굽기 위한 진입점.</summary>
    public void EditorBuild()
    {
        ConfigureHostTransform(transform as RectTransform);
        if (transform.Find("CustomerQueuePanel") == null)
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
        RectTransform queueMarker = CafeLayoutAnchors.Instance != null ? CafeLayoutAnchors.Instance.queue : null;
        if (queueMarker != null)
        {
            _panelRoot.anchorMin = queueMarker.anchorMin;
            _panelRoot.anchorMax = queueMarker.anchorMax;
            _panelRoot.pivot = queueMarker.pivot;
            _panelRoot.anchoredPosition = queueMarker.anchoredPosition;
        }
        else
        {
            _panelRoot.anchorMin = new Vector2(0.5f, 1f);
            _panelRoot.anchorMax = new Vector2(0.5f, 1f);
            _panelRoot.pivot = new Vector2(0.5f, 1f);
            _panelRoot.anchoredPosition = new Vector2(0f, -CafeLayoutConfig.QueueTopOffset);
        }

        VerticalLayoutGroup vlayout = panelObject.GetComponent<VerticalLayoutGroup>();
        vlayout.spacing = 4f;
        vlayout.childAlignment = TextAnchor.UpperCenter;
        vlayout.childControlWidth = true;
        vlayout.childControlHeight = true;
        vlayout.childForceExpandWidth = false;
        vlayout.childForceExpandHeight = false;
        vlayout.padding = new RectOffset(8, 8, 6, 6);

        ContentSizeFitter vfitter = panelObject.GetComponent<ContentSizeFitter>();
        vfitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        vfitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        _counterText = UIFactoryUtility.CreateLabel(_panelRoot, "HeaderText", "대기 손님", 16f);
        _counterText.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(_counterText.rectTransform, 360f, 22f);

        GameObject rowObject = UIFactoryUtility.CreateUIObject(
            "QueueRow", _panelRoot, typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        _rowRoot = rowObject.GetComponent<RectTransform>();

        HorizontalLayoutGroup hlayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        hlayout.spacing = 10f;
        hlayout.childAlignment = TextAnchor.UpperCenter;
        hlayout.childControlWidth = true;
        hlayout.childControlHeight = true;
        hlayout.childForceExpandWidth = false;
        hlayout.childForceExpandHeight = false;

        ContentSizeFitter hfitter = rowObject.GetComponent<ContentSizeFitter>();
        hfitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        hfitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildCards();
    }

    void BindPanel(RectTransform panel)
    {
        _panelRoot = panel;
        _counterText = panel.Find("HeaderText")?.GetComponent<TextMeshProUGUI>();
        _rowRoot = panel.Find("QueueRow") as RectTransform;
        BuildCards();
    }

    void BuildCards()
    {
        _cards.Clear();
        if (_rowRoot == null)
            return;

        for (int i = 0; i < MaxCards; i++)
        {
            CustomerCardUI card = CreateCard(_rowRoot);
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

        Image head = UIFactoryUtility.CreateImage(cardRect, "Head", Color.white);
        RectTransform headRect = head.rectTransform;
        headRect.anchorMin = headRect.anchorMax = new Vector2(0f, 0.5f);
        headRect.pivot = new Vector2(0f, 0.5f);
        headRect.anchoredPosition = new Vector2(8f, 0f);
        headRect.sizeDelta = new Vector2(62f, 62f);
        head.preserveAspect = true;
        head.raycastTarget = false;
        head.enabled = false;

        Image face = UIFactoryUtility.CreateImage(cardRect, "Face", Color.white);
        RectTransform faceRect = face.rectTransform;
        faceRect.anchorMin = faceRect.anchorMax = new Vector2(0f, 0.5f);
        faceRect.pivot = new Vector2(0f, 0.5f);
        faceRect.anchoredPosition = new Vector2(8f, 0f);
        faceRect.sizeDelta = new Vector2(62f, 62f);
        face.preserveAspect = true;
        face.raycastTarget = false;
        face.enabled = false;

        Image icon = UIFactoryUtility.CreateImage(cardRect, "Icon", Color.white);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-8f, 8f);
        iconRect.sizeDelta = new Vector2(40f, 40f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        TextMeshProUGUI name = UIFactoryUtility.CreateLabel(cardRect, "NameText", string.Empty, 18f);
        name.fontStyle = FontStyles.Bold;
        name.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.offsetMin = new Vector2(78f, -34f);
        nameRect.offsetMax = new Vector2(-52f, -6f);

        TextMeshProUGUI phrase = UIFactoryUtility.CreateLabel(cardRect, "PhraseText", string.Empty, 13f);
        phrase.alignment = TextAlignmentOptions.MidlineLeft;
        phrase.color = new Color(0.8f, 0.82f, 0.86f, 1f);
        RectTransform phraseRect = phrase.rectTransform;
        phraseRect.anchorMin = new Vector2(0f, 1f);
        phraseRect.anchorMax = new Vector2(1f, 1f);
        phraseRect.pivot = new Vector2(0f, 1f);
        phraseRect.offsetMin = new Vector2(78f, -56f);
        phraseRect.offsetMax = new Vector2(-52f, -36f);

        Image patienceFill = UIFactoryUtility.CreateFilledBar(
            cardRect, "PatienceBar",
            new Color(0f, 0f, 0f, 0.35f),
            new Color(0.35f, 0.8f, 0.4f, 1f));
        RectTransform barRect = patienceFill.rectTransform.parent as RectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = new Vector2(78f, 12f);
        barRect.offsetMax = new Vector2(-52f, 26f);

        CustomerCardUI card = cardObject.GetComponent<CustomerCardUI>();
        card.Bind(background, icon, head, face, name, phrase, patienceFill, cardObject.GetComponent<Button>());
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
