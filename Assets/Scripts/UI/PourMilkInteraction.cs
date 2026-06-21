using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 피처를 눌러(드래그) 우유를 붓는 조작입니다.
/// 누르고 있는 동안 게이지가 차오르며, BeverageBuildManager에 증가량을 전달합니다.
/// </summary>
public class PourMilkInteraction : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] BeverageStationUI station;
    [SerializeField] float pourPerSecond = 0.6f;

    bool _pouring;

    public Action<string> OnResult;

    public void Bind(BeverageStationUI stationUI)
    {
        station = stationUI;
    }

    public void OnPointerDown(PointerEventData eventData) => BeginPour();

    public void OnBeginDrag(PointerEventData eventData) => BeginPour();

    public void OnDrag(PointerEventData eventData) { /* 드래그를 흡수해 스크롤로 넘어가지 않게 합니다. */ }

    public void OnPointerUp(PointerEventData eventData) => EndPour();

    public void OnEndDrag(PointerEventData eventData) => EndPour();

    void BeginPour()
    {
        if (BeverageBuildManager.Instance == null || !BeverageBuildManager.Instance.CanOperate())
            return;

        _pouring = true;
    }

    void EndPour()
    {
        _pouring = false;
    }

    void Update()
    {
        if (!_pouring || BeverageBuildManager.Instance == null)
            return;

        if (!BeverageBuildManager.Instance.CanOperate())
        {
            _pouring = false;
            return;
        }

        float delta = pourPerSecond * Time.unscaledDeltaTime;
        if (BeverageBuildManager.Instance.TryPourMilk(delta, out string message))
        {
            float milk = BeverageBuildManager.Instance.GetCurrentSnapshot().MilkAmount;
            station?.SetGauge(milk);
            OnResult?.Invoke(message);
        }
        else
        {
            _pouring = false;
            OnResult?.Invoke(message);
        }
    }

    void OnDisable()
    {
        _pouring = false;
    }
}
