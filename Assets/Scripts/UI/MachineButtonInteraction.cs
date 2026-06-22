using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 에스프레소 머신 버튼입니다. 컵이 도킹된 상태에서 홀드하면 다이얼 바늘이 시계방향으로 돕니다.
/// 바늘이 초록(안전) 구역에서 떼면 완벽한 샷, 빨강(과추출) 구역이면 품질이 떨어집니다.
/// 안전 구역이 넓어 예측·조작이 쉽습니다.
/// </summary>
public class MachineButtonInteraction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public const float MinFill = 0.5f;    // 이만큼은 채워야 1샷
    public const float SweetMin = 0.74f;  // 초록(안전) 시작 (각도 266도)
    public const float SweetMax = 1.0f;   // 초록 끝 (한 바퀴 = 360도)
    public const float MaxFill = 1.12f;    // 빨강(과추출) 상한

    [SerializeField] RectTransform needle;
    [SerializeField] CupDragHandler cup;
    [SerializeField] float fillPerSecond = 0.62f; // 한 바퀴 약 1.6초

    bool _holding;
    float _fill;
    IngredientSO _heldBean; // 홀드 시작 시 소비한 원두
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

        // 홀드 시작 시 원두를 먼저 소비합니다. 취소/실패해도 차감됩니다.
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
        OnResult?.Invoke("추출 중… 초록 구역에서 떼세요.");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_holding)
            return;

        _holding = false;

        if (_fill >= MinFill && BeverageBuildManager.Instance != null)
        {
            if (BeverageBuildManager.Instance.AddPulledShot(QualityFromFill(_fill), _heldBean, _heldLevel, out string message))
            {
                AudioManager.PlaySfx("shot_extract");
                OnResult?.Invoke(message);
            }
            else
            {
                OnResult?.Invoke(message);
            }
        }
        else
        {
            // 초록 이전에 떼면 원두를 버립니다.
            OnResult?.Invoke("추출 실패 — 원두를 버렸습니다.");
        }

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
            // 홀드 도중 컵이 빠지면 취소 — 이미 소비한 원두는 버려집니다.
            _holding = false;
            _heldBean = null;
            _heldLevel = 1;
            _fill = 0f;
            UpdateNeedle();
            OnResult?.Invoke("추출 취소 — 원두를 버렸습니다.");
            return;
        }

        _fill = Mathf.Min(MaxFill, _fill + fillPerSecond * Time.unscaledDeltaTime);
        UpdateNeedle();
    }

    void UpdateNeedle()
    {
        // 전체 채움 범위(0~MaxFill)를 한 바퀴(360도)에 매핑해 3색 구역과 정확히 일치시킵니다.
        if (needle != null)
            needle.localEulerAngles = new Vector3(0f, 0f, -(_fill / MaxFill) * 360f); // 시계방향
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
        _holding = false;
        _heldBean = null;
        _heldLevel = 1;
        _fill = 0f;
        UpdateNeedle();
    }
}
