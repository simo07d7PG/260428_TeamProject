using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>에스프레소 머신 레버입니다. 아래로 당겨 홀드하면 추출되고, 놓으면 홀드 시간으로 품질이 판정됩니다.</summary>
public class EspressoLeverTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] BeverageStationUI station;
    [SerializeField] float pullDistance = 72f;
    [SerializeField] float activateFraction = 0.55f;
    [SerializeField] float gaugeFullSeconds = 1.8f;
    [SerializeField] float returnDuration = 0.14f;

    RectTransform _rect;
    Vector2 _restPos;
    bool _restCaptured;
    bool _dragging;
    float _startY;
    float _currentFraction;
    float _activeTime;
    Coroutine _returnRoutine;

    public Action<string> OnResult;

    public void Bind(BeverageStationUI stationUI)
    {
        station = stationUI;
    }

    void Awake()
    {
        _rect = transform as RectTransform;
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
        _activeTime = 0f;
        _currentFraction = 0f;
        _startY = eventData.position.y;
        station?.SetGauge(0f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _rect == null)
            return;

        float pulled = Mathf.Clamp(_startY - eventData.position.y, 0f, pullDistance);
        _rect.anchoredPosition = new Vector2(_restPos.x, _restPos.y - pulled);
        _currentFraction = pullDistance > 0f ? pulled / pullDistance : 0f;
    }

    void Update()
    {
        if (!_dragging)
            return;

        if (_currentFraction >= activateFraction && CanOperate())
            _activeTime += Time.unscaledDeltaTime;

        bool sweet = BeverageBuildManager.Instance != null
            && _activeTime >= BeverageBuildManager.Instance.ShotIdealMin
            && _activeTime <= BeverageBuildManager.Instance.ShotIdealMax;
        station?.SetGauge(gaugeFullSeconds > 0f ? _activeTime / gaugeFullSeconds : 0f, sweet);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragging = false;
        _currentFraction = 0f;
        station?.SetGauge(0f);

        if (_activeTime > 0f && BeverageBuildManager.Instance != null &&
            BeverageBuildManager.Instance.TryPullShot(_activeTime, out string message))
            OnResult?.Invoke(message);
        else if (BeverageBuildManager.Instance != null)
            OnResult?.Invoke("레버를 더 깊게 당겨 추출하세요.");

        if (_rect != null)
            _returnRoutine = StartCoroutine(ReturnUp());
    }

    IEnumerator ReturnUp()
    {
        float t = 0f;
        Vector2 start = _rect.anchoredPosition;

        while (t < returnDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / returnDuration);
            _rect.anchoredPosition = Vector2.Lerp(start, _restPos, k);
            yield return null;
        }

        _rect.anchoredPosition = _restPos;
        _returnRoutine = null;
    }

    void OnDisable()
    {
        _dragging = false;
        station?.SetGauge(0f);
        if (_rect != null && _restCaptured)
            _rect.anchoredPosition = _restPos;
    }
}
