using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 발주 UI를 표시하고 SupplyManager와 연동합니다.
/// </summary>
public class SupplyUIController : MonoBehaviour
{
    [SerializeField] RectTransform panelRoot;
    [SerializeField] RectTransform listContentRoot;
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] TextMeshProUGUI totalCostText;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] TextMeshProUGUI criticalWarningText;
    [SerializeField] Button confirmButton;

    readonly Dictionary<IngredientSO, int> _orderQuantities = new();
    readonly List<SupplyOrderRowUI> _rows = new();

    void Awake()
    {
        EnsureManagers();
        EnsurePanelReferences();
        BindConfirmButton();
        BuildOrderList();
    }

    void OnEnable()
    {
        if (SupplyManager.Instance != null)
            SupplyManager.Instance.OnSupplyStateChanged += RefreshAll;

        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnInventoryChanged += RefreshWarnings;

        RefreshAll();
    }

    void OnDisable()
    {
        if (SupplyManager.Instance != null)
            SupplyManager.Instance.OnSupplyStateChanged -= RefreshAll;

        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnInventoryChanged -= RefreshWarnings;
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && SupplyManager.Instance == null)
            ManagerUtility.GetOrAddComponent<SupplyManager>(PreparationManager.Instance.gameObject);
    }

    void EnsurePanelReferences()
    {
        if (panelRoot != null && listContentRoot != null)
            return;

        Transform canvasRoot = transform.root;
        SupplyUIPanelFactory.SupplyPanelRefs refs = SupplyUIPanelFactory.EnsurePanel(canvasRoot);

        panelRoot = refs.panelRoot;
        listContentRoot = refs.listContentRoot;
        coinText = refs.coinText;
        totalCostText = refs.totalCostText;
        statusText = refs.statusText;
        criticalWarningText = refs.criticalWarningText;
        confirmButton = refs.confirmButton;
    }

    void BindConfirmButton()
    {
        if (confirmButton == null)
            return;

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    void BuildOrderList()
    {
        if (listContentRoot == null || DataManager.Instance == null)
            return;

        ClearRows();

        foreach (IngredientSO ingredient in DataManager.Instance.GetOrderableIngredients())
        {
            if (ingredient == null)
                continue;

            if (!_orderQuantities.ContainsKey(ingredient))
                _orderQuantities[ingredient] = 0;

            SupplyOrderRowUI row = SupplyUIPanelFactory.CreateOrderRow(listContentRoot);
            row.Initialize(ingredient, _orderQuantities[ingredient], OnRowQuantityChanged);
            _rows.Add(row);
        }
    }

    void ClearRows()
    {
        for (int i = listContentRoot.childCount - 1; i >= 0; i--)
            Destroy(listContentRoot.GetChild(i).gameObject);

        _rows.Clear();
    }

    void OnRowQuantityChanged(IngredientSO ingredient, int quantity)
    {
        if (ingredient == null)
            return;

        _orderQuantities[ingredient] = Mathf.Max(0, quantity);
        RefreshSummary();
    }

    void OnConfirmClicked()
    {
        if (SupplyManager.Instance == null)
            return;

        Dictionary<IngredientSO, int> order = BuildActiveOrder();
        if (!SupplyManager.Instance.TrySubmitOrder(order, out string errorMessage))
        {
            if (statusText != null)
                statusText.text = errorMessage;
            return;
        }

        ResetOrderQuantities();
        if (statusText != null)
            statusText.text = "발주가 완료되었습니다.";

        RefreshAll();
    }

    void ResetOrderQuantities()
    {
        List<IngredientSO> keys = new List<IngredientSO>(_orderQuantities.Keys);
        foreach (IngredientSO ingredient in keys)
            _orderQuantities[ingredient] = 0;

        foreach (SupplyOrderRowUI row in _rows)
            row.SetQuantity(0);
    }

    Dictionary<IngredientSO, int> BuildActiveOrder()
    {
        Dictionary<IngredientSO, int> order = new Dictionary<IngredientSO, int>();

        foreach (KeyValuePair<IngredientSO, int> pair in _orderQuantities)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            order[pair.Key] = pair.Value;
        }

        return order;
    }

    public void RefreshAll()
    {
        RefreshSummary();
        RefreshWarnings();
        RefreshInteractable();
    }

    void RefreshSummary()
    {
        if (coinText != null && GameManager.Instance != null)
            coinText.text = $"보유 코인: {GameManager.Instance.Coin}";

        if (totalCostText != null && SupplyManager.Instance != null)
        {
            int totalCost = SupplyManager.Instance.CalculateOrderCost(BuildActiveOrder());
            totalCostText.text = $"발주 합계: {totalCost} Coin";
        }
    }

    void RefreshWarnings()
    {
        if (criticalWarningText == null || SupplyManager.Instance == null)
            return;

        List<IngredientSO> shortIngredients = SupplyManager.Instance.GetShortCriticalIngredients();
        if (shortIngredients.Count == 0)
        {
            criticalWarningText.text = string.Empty;
            return;
        }

        List<string> names = new List<string>();
        foreach (IngredientSO ingredient in shortIngredients)
        {
            if (ingredient != null)
                names.Add(ingredient.ingredientName);
        }

        criticalWarningText.text = $"필수 재료 부족: {string.Join(", ", names)}\n해당 재료가 필요한 메뉴는 제작할 수 없습니다.";
    }

    void RefreshInteractable()
    {
        if (confirmButton == null || SupplyManager.Instance == null)
            return;

        confirmButton.interactable = SupplyManager.Instance.CanOrderToday();
    }
}