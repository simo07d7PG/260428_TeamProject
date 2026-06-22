using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Merge UI 전체를 갱신하고 결과/폐기 비용 피드백을 표시합니다.</summary>
public class MergeUIController : MonoBehaviour
{
    [SerializeField] Transform basicInventoryRoot;
    [SerializeField] Transform advancedInventoryRoot;
    [SerializeField] MergeInventoryItemUI inventoryItemPrefab;
    [SerializeField] TextMeshProUGUI disposalCostText;
    [SerializeField] TextMeshProUGUI feedbackText;
    [SerializeField] float feedbackDuration = 2f;

    float _feedbackTimer;
    GameObject _autoMergeButton;

    const float InventoryItemSize = 80f;

    void Awake()
    {
        ResolveReferences();
        basicInventoryRoot = InventoryScrollUtility.EnsureSetup(basicInventoryRoot, InventoryItemSize, 8f);
        advancedInventoryRoot = InventoryScrollUtility.EnsureSetup(advancedInventoryRoot, InventoryItemSize, 8f);
        BuildAutoMergeButton();
        UIFontUtility.ApplyToHierarchy(transform.root);
        ValidateReferences();
    }

    /// <summary>준비 단계 한정으로 보이는 '자동 합성' 버튼을 런타임에 생성합니다.</summary>
    void BuildAutoMergeButton()
    {
        RectTransform canvasRect = transform.root as RectTransform;
        if (canvasRect == null)
            return;

        Button button = UIFactoryUtility.CreateButton(canvasRect, "AutoMergeButton", "자동 합성", new Color(0.28f, 0.5f, 0.42f, 1f));
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(60f, -120f);
        rect.sizeDelta = new Vector2(150f, 44f);
        button.onClick.AddListener(() => PreparationManager.Instance?.MergeAllPairs());

        _autoMergeButton = button.gameObject;
        RefreshAutoMergeVisibility();
    }

    void RefreshAutoMergeVisibility()
    {
        if (_autoMergeButton == null)
            return;

        bool prep = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Preparation;
        if (_autoMergeButton.activeSelf != prep)
            _autoMergeButton.SetActive(prep);
    }

    void ResolveReferences()
    {
        Transform canvasRoot = transform.root;

        if (basicInventoryRoot == null)
            basicInventoryRoot = ManagerUtility.FindDeepChild(canvasRoot, "BasicInvPanel");

        if (advancedInventoryRoot == null)
            advancedInventoryRoot = ManagerUtility.FindDeepChild(canvasRoot, "AdvancedInvPanel");
    }

    void ValidateReferences()
    {
        if (basicInventoryRoot == null)
            Debug.LogError("[MergeUIController] Basic Inventory Root가 연결되지 않았습니다.");

        if (advancedInventoryRoot == null)
            Debug.LogError("[MergeUIController] Advanced Inventory Root가 연결되지 않았습니다.");

        if (inventoryItemPrefab == null)
            Debug.LogError("[MergeUIController] Inventory Item Prefab이 연결되지 않았습니다.");

        if (disposalCostText == null)
            Debug.LogError("[MergeUIController] Disposal Cost Text가 연결되지 않았습니다.");
    }

    void Start()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshInventory();
        RefreshDisposalCost(PreparationManager.Instance != null ? PreparationManager.Instance.GarbageDisposalCost : 0);
    }

    void OnEnable()
    {
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnInventoryChanged += RefreshInventory;
            PreparationManager.Instance.OnDisposalCostChanged += RefreshDisposalCost;
            PreparationManager.Instance.OnMergeCompleted += ShowMergeResult;
            PreparationManager.Instance.OnMergeStatsChanged += RefreshStatus;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;

        RefreshInventory();
        RefreshStatus();
        RefreshAutoMergeVisibility();
    }

    void OnDisable()
    {
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnInventoryChanged -= RefreshInventory;
            PreparationManager.Instance.OnDisposalCostChanged -= RefreshDisposalCost;
            PreparationManager.Instance.OnMergeCompleted -= ShowMergeResult;
            PreparationManager.Instance.OnMergeStatsChanged -= RefreshStatus;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state) => RefreshAutoMergeVisibility();

    void Update()
    {
        if (feedbackText == null || !feedbackText.gameObject.activeSelf)
            return;

        _feedbackTimer -= Time.deltaTime;
        if (_feedbackTimer <= 0f)
            feedbackText.gameObject.SetActive(false);
    }

    void RefreshInventory()
    {
        if (PreparationManager.Instance == null)
        {
            Debug.LogWarning("[MergeUIController] PreparationManager가 아직 없어 인벤토리를 갱신하지 못했습니다.");
            return;
        }

        RebuildInventory(
            basicInventoryRoot,
            ConvertBasicInventory(PreparationManager.Instance.BasicInventory));

        RebuildInventory(
            advancedInventoryRoot,
            ConvertAdvancedInventory(PreparationManager.Instance.AdvancedInventory));
    }

    void RebuildInventory(Transform root, List<InventoryViewData> items)
    {
        if (root == null || inventoryItemPrefab == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        foreach (InventoryViewData item in items)
        {
            MergeInventoryItemUI view = Instantiate(inventoryItemPrefab.gameObject, root)
                .GetComponent<MergeInventoryItemUI>();
            ConfigureInventoryItemLayout(view);
            view.Setup(item.ingredient, item.level, item.count);
        }
    }

    static void ConfigureInventoryItemLayout(MergeInventoryItemUI view)
    {
        if (view == null)
            return;

        LayoutElement layoutElement = ManagerUtility.GetOrAddComponent<LayoutElement>(view.gameObject);
        layoutElement.preferredWidth = InventoryItemSize;
        layoutElement.preferredHeight = InventoryItemSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    static List<InventoryViewData> ConvertBasicInventory(IReadOnlyDictionary<IngredientSO, int> inventory)
    {
        List<InventoryViewData> result = new List<InventoryViewData>();

        foreach (KeyValuePair<IngredientSO, int> pair in inventory)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            result.Add(new InventoryViewData
            {
                ingredient = pair.Key,
                level = 1,
                count = pair.Value
            });
        }

        return result;
    }

    static List<InventoryViewData> ConvertAdvancedInventory(IReadOnlyList<MergeGridItem> inventory)
    {
        Dictionary<string, InventoryViewData> grouped = new Dictionary<string, InventoryViewData>();

        foreach (MergeGridItem item in inventory)
        {
            if (item == null || item.ingredient == null)
                continue;

            string key = $"{item.ingredient.name}_{item.level}";
            if (grouped.TryGetValue(key, out InventoryViewData existing))
            {
                existing.count++;
                grouped[key] = existing;
                continue;
            }

            grouped[key] = new InventoryViewData
            {
                ingredient = item.ingredient,
                level = item.level,
                count = 1
            };
        }

        return new List<InventoryViewData>(grouped.Values);
    }

    void RefreshDisposalCost(int cost) => RefreshStatus();

    /// <summary>합성 목표 진행·콤보·폐기 비용을 한 줄로 표시합니다.</summary>
    void RefreshStatus()
    {
        if (disposalCostText == null || PreparationManager.Instance == null)
            return;

        PreparationManager pm = PreparationManager.Instance;
        string goal = pm.IsDailyGoalComplete ? "달성 ✓" : $"{pm.DailyMergeCount}/{pm.DailyMergeGoal}";
        string combo = pm.CurrentCombo >= 2 ? $"   콤보 x{pm.CurrentCombo}" : string.Empty;
        disposalCostText.text = UIFontUtility.Sanitize($"합성 목표 {goal}{combo}   ·   폐기 {pm.GarbageDisposalCost} Coin");
    }

    void ShowMergeResult(MergeResult result)
    {
        if (feedbackText == null)
        {
            Debug.Log(result.message);
            return;
        }

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = UIFontUtility.Sanitize(result.message);
        _feedbackTimer = feedbackDuration;
    }

    struct InventoryViewData
    {
        public IngredientSO ingredient;
        public int level;
        public int count;
    }
}