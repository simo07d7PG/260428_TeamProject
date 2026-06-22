using System;
using UnityEngine;

/// <summary>
/// GPGP식 음료 제작을 관리하는 싱글톤 매니저입니다.
/// 빈 컵에서 시작해 스테이션별 조작(샷 홀드, 밀크 붓기, 시럽/토핑 탭)으로 레이어를 쌓고,
/// 완성 시 MenuCatalog로 메뉴를 추정합니다. 인벤토리 소비는 PreparationManager에 위임합니다.
/// </summary>
[DefaultExecutionOrder(-4)]
public class BeverageBuildManager : MonoBehaviour
{
    public const int MaxShots = 2;
    public const int MaxSyrupTaps = 3;
    public const int MaxToppings = 5;

    [Header("샷 추출 타이밍(초)")]
    [SerializeField] float shotMinHold = 0.45f;
    [SerializeField] float shotIdealMin = 1.0f;
    [SerializeField] float shotIdealMax = 1.8f;
    [SerializeField] float shotMaxHold = 2.8f;

    [Header("밀크")]
    [SerializeField] float milkMaxGauge = 1f;

    public static BeverageBuildManager Instance { get; private set; }

    readonly BeverageBuildSnapshot _snapshot = new();
    CustomerOrder _currentOrder;
    bool _buildActive;
    string _lastMessage = string.Empty;

    public bool IsBuildActive => _buildActive;
    public bool IsBuildComplete => _snapshot.IsComplete;
    public bool IsPreview => _buildActive && _currentOrder == null;
    public CustomerOrder CurrentOrder => _currentOrder;
    public string LastMessage => _lastMessage;
    public float ShotIdealMin => shotIdealMin;
    public float ShotIdealMax => shotIdealMax;

    public event Action OnBuildStateChanged;
    public event Action<BeverageBuildSnapshot> OnBuildCompleted;
    public event Action OnShotPulled;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public BeverageBuildSnapshot GetCurrentSnapshot() => _snapshot;

    public bool CanOperate()
    {
        return _buildActive
            && !_snapshot.IsComplete
            && GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameState.Service;
    }

    /// <summary>지정 타입 재료의 보유 수량(Lv1+Lv2 합)을 반환합니다. UI 재고 표시용.</summary>
    public int CountAvailable(IngredientType type)
    {
        PreparationManager prep = PreparationManager.Instance;
        if (prep == null)
            return 0;

        int count = 0;
        foreach (var pair in prep.BasicInventory)
        {
            if (pair.Key != null && pair.Key.type == type)
                count += pair.Value;
        }

        foreach (MergeGridItem item in prep.AdvancedInventory)
        {
            if (item != null && item.ingredient != null && item.ingredient.type == type)
                count++;
        }

        return count;
    }

    /// <summary>새 음료 제작을 시작합니다. order가 null이면 손님 없는 미리보기 모드입니다.</summary>
    public void StartBuild(CustomerOrder order)
    {
        _currentOrder = order;
        _snapshot.Clear();
        _buildActive = true;
        _lastMessage = order != null
            ? $"주문: {order.phrase}"
            : "미리보기: 자유롭게 만들어 보세요";

        UpdateEstimate();
        NotifyStateChanged();
    }

    public void StartPreviewBuild() => StartBuild(null);

    public void ClearBuild()
    {
        _snapshot.Clear();
        _buildActive = false;
        _currentOrder = null;
        _lastMessage = string.Empty;
        NotifyStateChanged();
    }

    public void ResetSession() => ClearBuild();

    // --- 스테이션 조작 ---------------------------------------------------

    /// <summary>에스프레소 머신 버튼 홀드. holdDuration으로 추출 품질을 판정합니다.</summary>
    public bool TryPullShot(float holdDuration, out string message)
    {
        message = string.Empty;
        if (!GuardOperate(out message))
            return false;

        if (_snapshot.ShotCount >= MaxShots)
        {
            message = "샷이 가득 찼습니다.";
            return false;
        }

        if (!TryResolveAndConsume(IngredientType.Base, out IngredientSO bean, out int level))
        {
            message = "원두(Base) 재고가 없습니다.";
            return false;
        }

        float quality = EvaluateShotQuality(holdDuration);
        BeverageLayer layer = _snapshot.GetOrAddLayer(StationType.EspressoShot, bean, level);

        float blended = (layer.amount * layer.count + quality) / (layer.count + 1);
        layer.count += 1;
        layer.amount = Mathf.Clamp01(blended);

        message = ShotQualityLabel(quality);
        OnShotPulled?.Invoke();
        AfterChange();
        return true;
    }

    /// <summary>피처→컵 드래그로 밀크를 붓습니다. deltaAmount는 이번 프레임 증가량(0~1).</summary>
    public bool TryPourMilk(float deltaAmount, out string message)
    {
        message = string.Empty;
        if (!GuardOperate(out message))
            return false;

        bool firstPour = !_snapshot.HasStation(StationType.SteamMilk);
        IngredientSO milk = null;
        int level = 1;
        if (firstPour && !TryResolveAndConsume(IngredientType.Milk, out milk, out level))
        {
            message = "우유(Milk) 재고가 없습니다.";
            return false;
        }

        BeverageLayer layer = _snapshot.GetOrAddLayer(StationType.SteamMilk, milk, Mathf.Max(1, level));
        layer.amount = Mathf.Clamp(layer.amount + Mathf.Max(0f, deltaAmount), 0f, milkMaxGauge);

        message = $"밀크 {(layer.amount * 100f):0}%";
        AfterChange();
        return true;
    }

    /// <summary>시럽 병 탭. 최대 3회. tapPosition은 컵 위 정규화 좌표.</summary>
    public bool TryAddSyrup(Vector2 tapPosition, out string message)
    {
        message = string.Empty;
        if (!GuardOperate(out message))
            return false;

        if (_snapshot.SyrupCount >= MaxSyrupTaps)
        {
            message = "시럽은 3회까지만 넣을 수 있습니다.";
            return false;
        }

        bool firstTap = !_snapshot.HasStation(StationType.Syrup);
        IngredientSO syrup = null;
        int level = 1;
        if (firstTap && !TryResolveAndConsume(IngredientType.Syrup, out syrup, out level))
        {
            message = "시럽(Syrup) 재고가 없습니다.";
            return false;
        }

        BeverageLayer layer = _snapshot.GetOrAddLayer(StationType.Syrup, syrup, Mathf.Max(1, level));
        layer.AddPoint(tapPosition);

        message = $"시럽 {layer.count}방울";
        AfterChange();
        return true;
    }

    /// <summary>토핑(휘핑/과일) 탭 배치. tapPosition은 컵 위 정규화 좌표.</summary>
    public bool TryAddTopping(Vector2 tapPosition, out string message)
    {
        message = string.Empty;
        if (!GuardOperate(out message))
            return false;

        if (_snapshot.ToppingCount >= MaxToppings)
        {
            message = "토핑이 가득 찼습니다.";
            return false;
        }

        bool first = !_snapshot.HasStation(StationType.Topping);
        IngredientSO topping = null;
        int level = 1;
        if (first && !TryResolveAndConsume(IngredientType.Topping, out topping, out level))
        {
            message = "토핑(Topping) 재고가 없습니다.";
            return false;
        }

        BeverageLayer layer = _snapshot.GetOrAddLayer(StationType.Topping, topping, Mathf.Max(1, level));
        layer.AddPoint(tapPosition);

        message = $"토핑 {layer.count}개";
        AfterChange();
        return true;
    }

    public bool TryAddIce(out string message)
    {
        message = string.Empty;
        if (!GuardOperate(out message))
            return false;

        BeverageLayer layer = _snapshot.GetOrAddLayer(StationType.Ice, null, 1);
        layer.count = Mathf.Max(1, layer.count + 1);
        message = "얼음 추가";
        AfterChange();
        return true;
    }

    public bool TryApplyLid(out string message)
    {
        message = string.Empty;
        if (!GuardOperate(out message))
            return false;

        _snapshot.GetOrAddLayer(StationType.Lid, null, 1).count = 1;
        message = "리드 씌움";
        AfterChange();
        return true;
    }

    /// <summary>제작을 완료하고 메뉴를 확정 추정합니다.</summary>
    public bool CompleteBuild(out string message)
    {
        message = string.Empty;
        if (!_buildActive)
        {
            message = "제작 중인 음료가 없습니다.";
            return false;
        }

        if (_snapshot.IsComplete)
        {
            message = "이미 완성된 음료입니다.";
            return false;
        }

        if (_snapshot.IsEmpty)
        {
            message = "컵이 비어 있습니다. 최소 한 가지는 넣어 주세요.";
            return false;
        }

        _snapshot.IsComplete = true;
        UpdateEstimate();
        _lastMessage = $"완성: {_snapshot.EstimatedMenuName}";
        message = _lastMessage;

        OnBuildCompleted?.Invoke(_snapshot);
        NotifyStateChanged();
        return true;
    }

    // --- 내부 ------------------------------------------------------------

    bool GuardOperate(out string message)
    {
        if (!CanOperate())
        {
            message = _buildActive ? "지금은 조작할 수 없습니다." : "먼저 컵을 준비해 주세요.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    void AfterChange()
    {
        UpdateEstimate();
        NotifyStateChanged();
    }

    void UpdateEstimate()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        MenuDefinition match = MenuCatalog.Match(_snapshot, day, out float confidence);

        _snapshot.MatchedMenu = match;
        _snapshot.MatchConfidence = confidence;
        _snapshot.EstimatedMenuName = _snapshot.IsEmpty
            ? "빈 컵"
            : match != null ? match.menuName : "알 수 없는 음료";
    }

    /// <summary>
    /// 지정 타입의 재료를 인벤토리에서 찾아 소비합니다. Lv2를 우선합니다.
    /// 미리보기 모드(손님 없음)에서 재고가 없으면 DataManager의 대표 재료로 대체하고 소비하지 않습니다.
    /// </summary>
    bool TryResolveAndConsume(IngredientType type, out IngredientSO ingredient, out int level)
    {
        ingredient = null;
        level = 1;

        if (TryFindInventoryIngredient(type, out ingredient, out level))
        {
            if (SupplyManager.Instance != null && !SupplyManager.Instance.CanCraftRequiring(ingredient))
                return false;

            if (PreparationManager.Instance != null &&
                PreparationManager.Instance.TryConsumeFromInventory(ingredient, level))
                return true;

            return false;
        }

        // 미리보기: 재고가 없어도 3단계 흐름을 테스트할 수 있도록 대표 재료로 대체.
        if (IsPreview)
        {
            ingredient = GetRepresentativeIngredient(type);
            level = 1;
            return true;
        }

        return false;
    }

    static bool TryFindInventoryIngredient(IngredientType type, out IngredientSO ingredient, out int level)
    {
        ingredient = null;
        level = 1;

        PreparationManager prep = PreparationManager.Instance;
        if (prep == null)
            return false;

        foreach (MergeGridItem item in prep.AdvancedInventory)
        {
            if (item != null && item.ingredient != null && item.ingredient.type == type)
            {
                ingredient = item.ingredient;
                level = item.level;
                return true;
            }
        }

        foreach (var pair in prep.BasicInventory)
        {
            if (pair.Key != null && pair.Value > 0 && pair.Key.type == type)
            {
                ingredient = pair.Key;
                level = 1;
                return true;
            }
        }

        return false;
    }

    static IngredientSO GetRepresentativeIngredient(IngredientType type)
    {
        if (DataManager.Instance == null)
            return null;

        foreach (IngredientSO ingredient in DataManager.Instance.GetIngredientsByType(type))
        {
            if (ingredient != null)
                return ingredient;
        }

        return null;
    }

    float EvaluateShotQuality(float holdDuration)
    {
        if (holdDuration <= 0f)
            return 0f;

        if (holdDuration >= shotIdealMin && holdDuration <= shotIdealMax)
            return 1f;

        if (holdDuration < shotIdealMin)
        {
            float t = Mathf.InverseLerp(shotMinHold, shotIdealMin, holdDuration);
            return Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(t));
        }

        float over = Mathf.InverseLerp(shotMaxHold, shotIdealMax, holdDuration);
        return Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(over));
    }

    static string ShotQualityLabel(float quality)
    {
        if (quality >= 0.85f)
            return "완벽한 샷!";
        if (quality >= 0.55f)
            return "괜찮은 샷";
        return "아쉬운 샷";
    }

    void NotifyStateChanged() => OnBuildStateChanged?.Invoke();
}
