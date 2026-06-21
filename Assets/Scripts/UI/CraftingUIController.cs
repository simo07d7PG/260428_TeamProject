using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3단계 음식 제작 UI를 표시하고 CraftingManager와 연동합니다.
/// </summary>
public class CraftingUIController : MonoBehaviour
{
    [SerializeField] RectTransform panelRoot;
    [SerializeField] RectTransform listContentRoot;
    [SerializeField] TextMeshProUGUI stageText;
    [SerializeField] TextMeshProUGUI successRateText;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] Image progressFill;
    [SerializeField] Image successFill;
    [SerializeField] Button startButton;
    [SerializeField] Button skipButton;
    [SerializeField] Button deliverButton;
    [SerializeField] Button resetButton;

    readonly List<CraftingIngredientSlotUI> _rows = new();
    GameState _lastVisibleState = (GameState)(-1);

    public static void ConfigureHostTransform(RectTransform host)
    {
        if (host == null)
            return;

        host.anchorMin = Vector2.zero;
        host.anchorMax = Vector2.one;
        host.pivot = new Vector2(0.5f, 0.5f);
        host.anchoredPosition = Vector2.zero;
        host.sizeDelta = Vector2.zero;
        host.localScale = Vector3.one;
    }

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        EnsureManagers();
        EnsurePanelReferences();
        UIFontUtility.ApplyToHierarchy(transform.root);
        BindButtons();
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnCraftingStateChanged += RefreshAll;

        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnInventoryChanged += RefreshIngredientList;

        RefreshPanelVisibility();
        RefreshAll();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;

        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnCraftingStateChanged -= RefreshAll;

        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnInventoryChanged -= RefreshIngredientList;
    }

    void Update()
    {
        RefreshPanelVisibility();
    }

    void OnGameStateChanged(GameState state)
    {
        RefreshPanelVisibility();
        RefreshAll();
    }

    void RefreshPanelVisibility()
    {
        if (GameManager.Instance == null || panelRoot == null)
            return;

        GameState state = GameManager.Instance.CurrentState;
        if (_lastVisibleState == state)
            return;

        _lastVisibleState = state;
        bool isService = state == GameState.Service;
        panelRoot.gameObject.SetActive(isService);
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && CraftingManager.Instance == null)
            ManagerUtility.GetOrAddComponent<CraftingManager>(PreparationManager.Instance.gameObject);
    }

    void EnsurePanelReferences()
    {
        CraftingUIPanelFactory.CraftingPanelRefs refs = CraftingUIPanelFactory.EnsurePanel(transform);
        panelRoot = refs.panelRoot;
        listContentRoot = refs.listContentRoot;
        stageText = refs.stageText;
        successRateText = refs.successRateText;
        statusText = refs.statusText;
        progressFill = refs.progressFill;
        successFill = refs.successFill;
        startButton = refs.startButton;
        skipButton = refs.skipButton;
        deliverButton = refs.deliverButton;
        resetButton = refs.resetButton;
    }

    void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        if (deliverButton != null)
        {
            deliverButton.onClick.RemoveAllListeners();
            deliverButton.onClick.AddListener(OnDeliverClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetClicked);
        }
    }

    void OnStartClicked()
    {
        if (CraftingManager.Instance == null)
            return;

        if (!CraftingManager.Instance.TryStartSession(out string errorMessage))
        {
            SetStatus(errorMessage);
            return;
        }

        SetStatus("재료를 선택해 주세요.");
        RefreshAll();
    }

    void OnSkipClicked()
    {
        if (CraftingManager.Instance == null)
            return;

        if (!CraftingManager.Instance.TrySkipTopping(out string errorMessage))
        {
            SetStatus(errorMessage);
            return;
        }

        RefreshAll();
    }

    void OnDeliverClicked()
    {
        if (CraftingManager.Instance == null)
            return;

        if (!CraftingManager.Instance.TryDeliverToCustomer(out string message))
        {
            SetStatus(message);
            return;
        }

        SetStatus(message);
        RefreshAll();
    }

    void OnResetClicked()
    {
        CraftingManager.Instance?.ResetSession();
        SetStatus("제작을 초기화했습니다.");
        RefreshAll();
    }

    void OnIngredientSelected(IngredientSO ingredient, int level)
    {
        if (CraftingManager.Instance == null)
            return;

        if (!CraftingManager.Instance.TrySelectIngredient(ingredient, level, out string errorMessage))
        {
            SetStatus(errorMessage);
            return;
        }

        SetStatus($"{ingredient.ingredientName} 투입 중...");
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshStageInfo();
        RefreshIngredientList();
        RefreshButtons();
    }

    void RefreshStageInfo()
    {
        if (CraftingManager.Instance == null)
            return;

        CraftStage stage = CraftingManager.Instance.CurrentStage;
        float successRate = CraftingManager.Instance.CurrentSuccessRate;
        float progress = CraftingManager.Instance.StageProgress;

        if (stageText != null)
        {
            string stageLabel = CraftingManager.GetStageLabel(stage);
            stageText.text = UIFontUtility.Sanitize(stage == CraftStage.Complete
                ? $"단계: 완료 - {CraftingManager.Instance.LastResult?.Message}"
                : $"단계: {stageLabel} ({(int)stage + 1}/3)");
        }

        if (successRateText != null)
            successRateText.text = UIFontUtility.Sanitize(
                $"성공도: {successRate:0}% (85%+ x1.25 / 50% 미만 x0.85)");

        if (successFill != null)
            successFill.fillAmount = successRate / 100f;

        if (progressFill != null)
        {
            progressFill.fillAmount = stage == CraftStage.Complete
                ? 1f
                : progress;
        }

        if (stage == CraftStage.Complete && CraftingManager.Instance.LastResult != null && statusText != null)
        {
            CraftResult result = CraftingManager.Instance.LastResult;
            statusText.text = UIFontUtility.Sanitize(
                $"{result.Message} | 예상 수령 {result.FinalPayout} Coin");
        }
    }

    void RefreshIngredientList()
    {
        if (listContentRoot == null || CraftingManager.Instance == null)
            return;

        ClearRows();

        if (!CraftingManager.Instance.IsSessionActive || CraftingManager.Instance.CurrentStage == CraftStage.Complete)
            return;

        List<CraftingIngredientEntry> entries =
            CraftingManager.Instance.GetAvailableIngredients(CraftingManager.Instance.CurrentStage);

        bool canSelect = CraftingManager.Instance.CanCraft();

        foreach (CraftingIngredientEntry entry in entries)
        {
            CraftingIngredientSlotUI row = CraftingUIPanelFactory.CreateIngredientRow(listContentRoot);
            row.Initialize(entry, canSelect, OnIngredientSelected);
            _rows.Add(row);
        }

        if (entries.Count == 0 && statusText != null && CraftingManager.Instance.IsSessionActive)
            statusText.text = UIFontUtility.Sanitize(
                $"{CraftingManager.GetStageLabel(CraftingManager.Instance.CurrentStage)} 단계에 사용할 재료가 없습니다.");
    }

    void RefreshButtons()
    {
        if (CraftingManager.Instance == null)
            return;

        bool isService = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Service;
        bool sessionActive = CraftingManager.Instance.IsSessionActive;
        bool isCrafting = CraftingManager.Instance.IsCrafting;
        CraftStage stage = CraftingManager.Instance.CurrentStage;

        if (startButton != null)
            startButton.interactable = isService && !sessionActive && !isCrafting;

        if (skipButton != null)
            skipButton.interactable = isService && sessionActive && stage == CraftStage.Topping && !isCrafting;

        if (deliverButton != null)
            deliverButton.interactable = isService && stage == CraftStage.Complete;

        if (resetButton != null)
            resetButton.interactable = isService && (sessionActive || stage == CraftStage.Complete);
    }

    void ClearRows()
    {
        if (listContentRoot == null)
            return;

        for (int i = listContentRoot.childCount - 1; i >= 0; i--)
            Destroy(listContentRoot.GetChild(i).gameObject);

        _rows.Clear();
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = UIFontUtility.Sanitize(message);
    }
}