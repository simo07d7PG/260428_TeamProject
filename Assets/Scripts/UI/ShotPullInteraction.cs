using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 에스프레소 머신 버튼을 홀드하는 조작입니다.
/// 누르고 있는 동안 게이지가 차고, 떼는 순간의 홀드 시간으로 추출 품질이 판정됩니다.
/// </summary>
public class ShotPullInteraction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] BeverageStationUI station;
    [SerializeField] float gaugeFullSeconds = 1.8f;

    bool _holding;
    float _holdStart;

    public Action<string> OnResult;

    public void Bind(BeverageStationUI stationUI)
    {
        station = stationUI;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (BeverageBuildManager.Instance == null || !BeverageBuildManager.Instance.CanOperate())
            return;

        _holding = true;
        _holdStart = Time.unscaledTime;
        station?.SetGauge(0f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_holding)
            return;

        _holding = false;
        station?.SetGauge(0f);

        float holdDuration = Time.unscaledTime - _holdStart;
        if (BeverageBuildManager.Instance == null)
            return;

        if (BeverageBuildManager.Instance.TryPullShot(holdDuration, out string message))
            OnResult?.Invoke(message);
        else
            OnResult?.Invoke(message);
    }

    void Update()
    {
        if (!_holding)
            return;

        float elapsed = Time.unscaledTime - _holdStart;
        float fill = gaugeFullSeconds > 0f ? elapsed / gaugeFullSeconds : 0f;
        bool sweetSpot = BeverageBuildManager.Instance != null
            && elapsed >= BeverageBuildManager.Instance.ShotIdealMin
            && elapsed <= BeverageBuildManager.Instance.ShotIdealMax;
        station?.SetGauge(fill, sweetSpot);
    }

    void OnDisable()
    {
        _holding = false;
        station?.SetGauge(0f);
    }
}
