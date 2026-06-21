using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 완성된 컵을 손님 카드로 드래그해 서빙하는 조작입니다.
/// 드롭 지점 아래의 CustomerCardUI를 찾아 ServiceManager에 전달합니다.
/// </summary>
public class ServeDragInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    static readonly List<RaycastResult> RaycastResults = new();

    [SerializeField] Vector2 ghostSize = new(140f, 170f);

    bool _dragging;

    public Action<string> OnResult;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanServe())
            return;

        _dragging = true;
        CanvasGroup canvasGroup = ManagerUtility.GetOrAddComponent<CanvasGroup>(gameObject);
        MergeDragVisualizer.Begin(null, eventData, ghostSize, canvasGroup);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragging)
            MergeDragVisualizer.UpdatePosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragging = false;
        MergeDragVisualizer.End();

        Customer target = FindCustomerUnderPointer(eventData);
        if (target == null)
        {
            OnResult?.Invoke("손님 위에 놓아주세요.");
            return;
        }

        if (ServiceManager.Instance == null)
        {
            OnResult?.Invoke("영업 매니저를 찾을 수 없습니다.");
            return;
        }

        ServiceManager.Instance.TryServe(target, out string message);
        OnResult?.Invoke(message);
    }

    static bool CanServe()
    {
        return ServiceManager.Instance != null
            && BeverageBuildManager.Instance != null
            && BeverageBuildManager.Instance.IsBuildComplete;
    }

    static Customer FindCustomerUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return null;

        RaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastResults);

        foreach (RaycastResult result in RaycastResults)
        {
            if (result.gameObject == null)
                continue;

            CustomerCardUI card = result.gameObject.GetComponentInParent<CustomerCardUI>();
            if (card != null && card.Customer != null && card.Customer.IsActive)
                return card.Customer;
        }

        return null;
    }
}
