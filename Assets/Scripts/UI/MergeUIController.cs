using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Merge UI 전체를 갱신하고 결과/폐기 비용 피드백을 표시합니다.
/// 하위·형제 오브젝트를 이름으로 자동 참조합니다.
/// </summary>
public class MergeUIController : MonoBehaviour
{
    [SerializeField] Transform basicInventoryRoot;
    [SerializeField] Transform advancedInventoryRoot;
    [SerializeField] MergeInventoryItemUI inventoryItemPrefab;
    [SerializeField] Text disposalCostText;
    [SerializeField] Text feedbackText;
    [SerializeField] float feedbackDuration = 2f;

    float _feedbackTimer;

    void Awake()
    {
        ResolveReferences();
    }

    void ResolveReferences()
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform;

        if (basicInventoryRoot == null)
            basicInventoryRoot = ManagerUtility.FindDeepChild(searchRoot, "BasicInventoryPanel");

        if (advancedInventoryRoot == null)
            advancedInventoryRoot = ManagerUtility.FindDeepChild(searchRoot, "AdvancedInventoryPanel");

        if (disposalCostText == null)
            disposalCostText = ManagerUtility.FindDeepChild(transform, "DisposalCostText")?.GetComponent<Text>();

        if (feedbackText == null)
            feedbackText = ManagerUtility.FindDeepChild(transform, "FeedbackText")?.GetComponent<Text>();
    }

    void OnEnable()
    {
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnInventoryChanged += RefreshInventory;
            PreparationManager.Instance.OnDisposalCostChanged += RefreshDisposalCost;
            PreparationManager.Instance.OnMergeCompleted += ShowMergeResult;
        }

        RefreshInventory();
        RefreshDisposalCost(PreparationManager.Instance != null ? PreparationManager.Instance.GarbageDisposalCost : 0);
    }

    void OnDisable()
    {
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnInventoryChanged -= RefreshInventory;
            PreparationManager.Instance.OnDisposalCostChanged -= RefreshDisposalCost;
            PreparationManager.Instance.OnMergeCompleted -= ShowMergeResult;
        }
    }

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
            return;

        RebuildInventory(
            basicInventoryRoot,
            ConvertBasicInventory(PreparationManager.Instance.BasicInventory));

        RebuildInventory(
            advancedInventoryRoot,
            ConvertAdvancedInventory(PreparationManager.Instance.AdvancedInventory));
    }

    void RebuildInventory(Transform root, List<InventoryViewData> items)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        foreach (InventoryViewData item in items)
        {
            MergeInventoryItemUI view = CreateInventoryItem(root);
            view.Setup(item.ingredient, item.level, item.count);
        }
    }

    MergeInventoryItemUI CreateInventoryItem(Transform root)
    {
        if (inventoryItemPrefab != null)
            return Instantiate(inventoryItemPrefab, root);

        GameObject itemObject = new GameObject(
            "InventoryItem",
            typeof(RectTransform),
            typeof(Image),
            typeof(MergeInventoryItemUI));
        itemObject.transform.SetParent(root, false);

        RectTransform rect = itemObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(72f, 72f);

        GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(Text));
        countObject.transform.SetParent(itemObject.transform, false);
        RectTransform countRect = countObject.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.6f, 0f);
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;

        MergeInventoryItemUI item = itemObject.GetComponent<MergeInventoryItemUI>();
        Text countLabel = countObject.GetComponent<Text>();
        countLabel.alignment = TextAnchor.LowerRight;
        countLabel.fontSize = 14;
        countLabel.color = Color.white;
        countLabel.font = ManagerUtility.GetDefaultFont();
        countLabel.raycastTarget = false;

        item.Initialize(itemObject.GetComponent<Image>(), countLabel);
        return item;
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

    void RefreshDisposalCost(int cost)
    {
        if (disposalCostText != null)
            disposalCostText.text = $"쓰레기 폐기 비용: {cost} Coin";
    }

    void ShowMergeResult(MergeResult result)
    {
        if (feedbackText == null)
        {
            Debug.Log(result.message);
            return;
        }

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = result.message;
        _feedbackTimer = feedbackDuration;
    }

    struct InventoryViewData
    {
        public IngredientSO ingredient;
        public int level;
        public int count;
    }
}