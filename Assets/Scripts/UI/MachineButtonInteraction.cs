using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>에스프레소 머신 버튼입니다. 컵이 도킹된 상태에서 홀드하면 다이얼 바늘이 시계방향으로 돕니다.</summary>
public class MachineButtonInteraction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public const float MinFill = 0.5f;
    public const float SweetMin = 0.74f;
    public const float SweetMax = 1.0f;
    public const float MaxFill = 1.12f;

    [SerializeField] RectTransform needle;
    [SerializeField] CupDragHandler cup;
    [SerializeField] float fillPerSecond = 0.62f;

    bool _holding;
    float _fill;
    IngredientSO _heldBean;
    int _heldLevel = 1;

    public Action<string> OnResult;

    public void Bind(RectTransform needleRect, CupDragHandler cupHandler)
    {
        needle = needleRect;
        cup = cupHandler;
    }

    bool Ready()
    {
        return cup != null && cup.OnMachine
            && BeverageBuildManager.Instance != null && BeverageBuildManager.Instance.CanOperate();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Ready())
        {
            OnResult?.Invoke("먼저 컵을 머신 위에 올려주세요.");
            return;
        }

        if (BeverageBuildManager.Instance == null)
        {
            OnResult?.Invoke("커피 머신을 준비 중입니다.");
            return;
        }

        if (!BeverageBuildManager.Instance.TryBeginShot(out _heldBean, out _heldLevel, out string message))
        {
            OnResult?.Invoke(message);
            return;
        }

        _holding = true;
        _fill = 0f;
        UpdateNeedle();
        AudioManager.PlayHold("shot_extract");
        OnResult?.Invoke("추출 중… 초록 구역에서 떼세요.");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_holding)
            return;

        _holding = false;

        bool success = false;
        if (_fill >= MinFill && BeverageBuildManager.Instance != null)
        {
            success = BeverageBuildManager.Instance.AddPulledShot(QualityFromFill(_fill), _heldBean, _heldLevel, out string message);
            OnResult?.Invoke(message);
        }
        else
        {
            OnResult?.Invoke("추출 실패 — 원두를 버렸습니다.");
        }

        // 성공: 홀드 사운드를 끊지 않아 클립이 끝까지 재생됨. 실패: 즉시 끊음.
        if (!success)
            AudioManager.StopHold();

        _heldBean = null;
        _heldLevel = 1;
        _fill = 0f;
        UpdateNeedle();
    }

    void Update()
    {
        if (!_holding)
            return;

        if (!Ready())
        {
            _holding = false;
            _heldBean = null;
            _heldLevel = 1;
            _fill = 0f;
            UpdateNeedle();
            AudioManager.StopHold();
            OnResult?.Invoke("추출 취소 — 원두를 버렸습니다.");
            return;
        }

        _fill = Mathf.Min(MaxFill, _fill + fillPerSecond * Time.unscaledDeltaTime);
        UpdateNeedle();
    }

    void UpdateNeedle()
    {
        if (needle != null)
            needle.localEulerAngles = new Vector3(0f, 0f, -(_fill / MaxFill) * 360f);
    }

    static float QualityFromFill(float fill)
    {
        if (fill < MinFill)
            return 0f;
        if (fill < SweetMin)
            return Mathf.Lerp(0.5f, 1f, (fill - MinFill) / (SweetMin - MinFill));
        if (fill <= SweetMax)
            return 1f;
        return Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((fill - SweetMax) / (MaxFill - SweetMax)));
    }

    void OnDisable()
    {
        bool wasHolding = _holding;
        _holding = false;
        _heldBean = null;
        _heldLevel = 1;
        _fill = 0f;
        UpdateNeedle();
        if (wasHolding)
            AudioManager.StopHold();
    }
}
