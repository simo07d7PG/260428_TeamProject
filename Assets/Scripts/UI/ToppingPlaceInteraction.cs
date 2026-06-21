using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 토핑 모드가 켜진 동안 컵을 탭하면 그 위치에 토핑을 배치합니다.
/// 탭 좌표를 CupCanvasUI로 정규화해 BeverageBuildManager에 전달합니다.
/// </summary>
public class ToppingPlaceInteraction : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] CupCanvasUI cupCanvas;

    public bool ToppingMode { get; private set; }

    public Action<string> OnResult;

    public void Bind(CupCanvasUI canvas)
    {
        cupCanvas = canvas;
    }

    public void SetToppingMode(bool on)
    {
        ToppingMode = on;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!ToppingMode || cupCanvas == null || BeverageBuildManager.Instance == null)
            return;

        if (!BeverageBuildManager.Instance.CanOperate())
            return;

        if (!cupCanvas.TryGetNormalizedPoint(eventData.position, eventData.pressEventCamera, out Vector2 normalized))
            normalized = new Vector2(0.5f, 0.7f);

        if (BeverageBuildManager.Instance.TryAddTopping(normalized, out string message))
            OnResult?.Invoke(message);
        else
            OnResult?.Invoke(message);
    }
}
