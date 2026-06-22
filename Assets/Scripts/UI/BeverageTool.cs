using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>도구 종류. 컵 위에서의 조작 방식이 달라집니다.</summary>
public enum BeverageToolKind
{
    Milk,
    Syrup,
    Topping,
    Lid,
    Ice
}

/// <summary>직접 잡아 컵 위로 가져가 조작하는 도구입니다.</summary>
public class BeverageTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] BeverageToolKind kind;
    [SerializeField] float pourPerSecond = 0.6f;
    [SerializeField] float syrupInterval = 0.34f;
    [SerializeField] float returnDuration = 0.16f;
    [SerializeField] float tiltDegrees = -32f;

    CupCanvasUI _cup;
    RectTransform _rect;
    RectTransform _parent;
    Vector2 _restPos;
    bool _restCaptured;
    bool _dragging;
    bool _overCup;
    Vector2 _lastScreenPos;
    Camera _cam;
    float _syrupTimer;
    Coroutine _returnRoutine;

    [SerializeField] TextMeshProUGUI stockText;

    public Action<string> OnResult;
    public BeverageToolKind Kind => kind;

    public void Bind(BeverageToolKind toolKind, CupCanvasUI cup)
    {
        kind = toolKind;
        _cup = cup;
    }

    public void BindStockBadge(TextMeshProUGUI badge)
    {
        stockText = badge;
    }

    /// <summary>해당 재료 보유량을 배지에 표시합니다. (Lid/Ice는 소비 없음 → 숨김)</summary>
    public void RefreshStock()
    {
        if (stockText == null)
            return;

        if (!TryGetIngredientType(out IngredientType type) || BeverageBuildManager.Instance == null)
        {
            stockText.gameObject.SetActive(false);
            return;
        }

        int count = BeverageBuildManager.Instance.CountAvailable(type);
        stockText.gameObject.SetActive(true);
        stockText.text = $"x{count}";
        stockText.color = count > 0 ? new Color(0.1f, 0.1f, 0.12f, 1f) : new Color(0.85f, 0.25f, 0.2f, 1f);
    }

    bool TryGetIngredientType(out IngredientType type)
    {
        switch (kind)
        {
            case BeverageToolKind.Milk: type = IngredientType.Milk; return true;
            case BeverageToolKind.Syrup: type = IngredientType.Syrup; return true;
            case BeverageToolKind.Topping: type = IngredientType.Topping; return true;
            default: type = IngredientType.Base; return false;
        }
    }

    void Awake()
    {
        _rect = transform as RectTransform;
        _parent = transform.parent as RectTransform;
    }

    void CaptureRest()
    {
        if (!_restCaptured && _rect != null)
        {
            _restPos = _rect.anchoredPosition;
            _restCaptured = true;
        }
    }

    static bool CanOperate()
    {
        return BeverageBuildManager.Instance != null && BeverageBuildManager.Instance.CanOperate();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanOperate())
            return;

        CaptureRest();
        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }

        _dragging = true;
        _syrupTimer = 0f;
        _rect.SetAsLastSibling();
        MoveTo(eventData);
        UpdateOverCup(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        MoveTo(eventData);
        UpdateOverCup(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragging = false;
        if (_overCup)
            HandleDrop(eventData.position, eventData.pressEventCamera);

        SetOverCup(false);
        if (_rect != null)
            _returnRoutine = StartCoroutine(ReturnToRest());
    }

    void MoveTo(PointerEventData eventData)
    {
        _lastScreenPos = eventData.position;
        _cam = eventData.pressEventCamera;

        if (_parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
            _rect.anchoredPosition = local;
    }

    void UpdateOverCup(PointerEventData eventData)
    {
        SetOverCup(_cup != null && _cup.IsOverCup(eventData.position, eventData.pressEventCamera));
    }

    void SetOverCup(bool over)
    {
        if (_overCup == over)
            return;

        _overCup = over;

        if (_rect != null && kind == BeverageToolKind.Milk)
        {
            _rect.localRotation = Quaternion.Euler(0f, 0f, over ? tiltDegrees : 0f);
            if (over)
                AudioManager.PlaySfx("milk_pour");
        }
    }

    void Update()
    {
        if (!_dragging || !_overCup || !CanOperate())
            return;

        if (kind == BeverageToolKind.Milk)
            PourMilk();
    }

    void PourMilk()
    {
        if (BeverageBuildManager.Instance.TryPourMilk(pourPerSecond * Time.unscaledDeltaTime, out string message))
            OnResult?.Invoke(message);
    }

    void HandleDrop(Vector2 screenPos, Camera cam)
    {
        _cam = cam;
        bool ok;
        string message;
        string sfx;
        switch (kind)
        {
            case BeverageToolKind.Syrup:
                ok = BeverageBuildManager.Instance.TryAddSyrup(NormAt(screenPos), out message);
                sfx = "syrup_drop";
                break;
            case BeverageToolKind.Topping:
                ok = BeverageBuildManager.Instance.TryAddTopping(NormAt(screenPos), out message);
                sfx = "topping_add";
                break;
            case BeverageToolKind.Ice:
                ok = BeverageBuildManager.Instance.TryAddIce(out message);
                sfx = "ice_add";
                break;
            case BeverageToolKind.Lid:
                ok = BeverageBuildManager.Instance.TryApplyLid(out message);
                sfx = "lid_close";
                break;
            default:
                return;
        }

        if (ok)
            AudioManager.PlaySfx(sfx);
        OnResult?.Invoke(message);
    }

    Vector2 NormAt(Vector2 screenPos)
    {
        if (_cup != null && _cup.TryGetNormalizedPoint(screenPos, _cam, out Vector2 normalized))
            return normalized;

        return new Vector2(0.5f, 0.6f);
    }

    IEnumerator ReturnToRest()
    {
        float t = 0f;
        Vector2 start = _rect.anchoredPosition;
        Quaternion rotStart = _rect.localRotation;

        while (t < returnDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / returnDuration);
            _rect.anchoredPosition = Vector2.Lerp(start, _restPos, k);
            _rect.localRotation = Quaternion.Lerp(rotStart, Quaternion.identity, k);
            yield return null;
        }

        _rect.anchoredPosition = _restPos;
        _rect.localRotation = Quaternion.identity;
        _returnRoutine = null;
    }

    void OnDisable()
    {
        _dragging = false;
        _overCup = false;
        if (_rect != null && _restCaptured)
        {
            _rect.anchoredPosition = _restPos;
            _rect.localRotation = Quaternion.identity;
        }
    }
}
