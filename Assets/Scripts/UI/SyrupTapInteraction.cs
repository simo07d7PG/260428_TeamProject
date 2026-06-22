using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>시럽 병을 탭하면 컵에 시럽 방울이 추가됩니다(최대 3회).</summary>
public class SyrupTapInteraction : MonoBehaviour, IPointerClickHandler
{
    static readonly Vector2[] SyrupSpots =
    {
        new(0.38f, 0.62f),
        new(0.60f, 0.66f),
        new(0.50f, 0.54f)
    };

    public Action<string> OnResult;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BeverageBuildManager.Instance == null || !BeverageBuildManager.Instance.CanOperate())
            return;

        int index = BeverageBuildManager.Instance.GetCurrentSnapshot().SyrupCount;
        Vector2 spot = SyrupSpots[Mathf.Clamp(index, 0, SyrupSpots.Length - 1)];

        if (BeverageBuildManager.Instance.TryAddSyrup(spot, out string message))
            OnResult?.Invoke(message);
        else
            OnResult?.Invoke(message);
    }
}
