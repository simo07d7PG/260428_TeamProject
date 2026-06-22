using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 활성 컵을 잡아 배치하는 핸들러입니다.
/// 머신 슬롯에 드롭하면 도킹(머신 버튼으로 추출), 그 외에 드롭하면 카운터 아래 '드는 상태'로,
/// 손님 카드에 드롭하면 완성 후 서빙합니다. 추출/재료 투입은 머신 버튼·재료 도구가 담당합니다.
/// </summary>
public class CupDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    static readonly List<RaycastResult> RaycastResults = new();

    RectTransform _rect;
    RectTransform _parent;
    RectTransform _machineSlot;
    RectTransform _machineButton;
    RectTransform _serveZone;
    Vector2 _machinePos;
    Vector2 _heldHome;
    Vector2 _stackHome;
    bool _dragging;
    bool _onMachine;

    public Action<string> OnResult;
    public bool OnMachine => _onMachine;

    public void Bind(RectTransform machineSlot, RectTransform machineButton, RectTransform serveZone, Vector2 machinePos, Vector2 heldHome, Vector2 stackHome)
    {
        _machineSlot = machineSlot;
        _machineButton = machineButton;
        _serveZone = serveZone;
        _machinePos = machinePos;
        _heldHome = heldHome;
        _stackHome = stackHome;
    }

    void Awake()
    {
        _rect = transform as RectTransform;
        _parent = transform.parent as RectTransform;
    }

    public void ResetToStack()
    {
        _onMachine = false;
        if (_rect != null && !_dragging)
            _rect.anchoredPosition = _stackHome;
    }

    static bool CanOperate()
    {
        return BeverageBuildManager.Instance != null && BeverageBuildManager.Instance.CanOperate();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanOperate())
            return;

        _dragging = true;
        _onMachine = false;
        _rect.SetAsLastSibling();
        AudioManager.PlaySfx("cup_take");
        MoveTo(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragging)
            MoveTo(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragging = false;

        // 1) 손님 카드 위에 직접 드롭 → 그 손님에게 서빙
        Customer target = FindCustomer(eventData);
        if (target != null)
        {
            ServeTo(target);
            return;
        }

        // 2) 오른쪽 서빙 존에 드롭 → 현재 선택된(주문 받은) 손님에게 서빙
        if (_serveZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_serveZone, eventData.position, eventData.pressEventCamera))
        {
            ServeToActive();
            return;
        }

        if (_machineSlot != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_machineSlot, eventData.position, eventData.pressEventCamera))
        {
            // 머신에 도킹
            _onMachine = true;
            if (_rect != null)
                _rect.anchoredPosition = _machinePos;
            _machineButton?.SetAsLastSibling(); // 버튼을 컵 위로 올려 누를 수 있게
            AudioManager.PlaySfx("cup_place");
        }
        else
        {
            // 카운터 아래 '드는 상태'
            _onMachine = false;
            if (_rect != null)
                _rect.anchoredPosition = _heldHome;
        }
    }

    void MoveTo(PointerEventData eventData)
    {
        if (_parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
            _rect.anchoredPosition = local;
    }

    Customer FindCustomer(PointerEventData eventData)
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

    void ServeToActive()
    {
        Customer customer = ServiceManager.Instance != null ? ServiceManager.Instance.ActiveCustomer : null;
        if (customer == null || !customer.IsActive)
        {
            OnResult?.Invoke("전달할 손님을 먼저 선택하세요.");
            ReturnToHeld();
            return;
        }

        ServeTo(customer);
    }

    void ServeTo(Customer customer)
    {
        if (BeverageBuildManager.Instance == null || ServiceManager.Instance == null)
            return;

        if (!BeverageBuildManager.Instance.IsBuildComplete)
            BeverageBuildManager.Instance.CompleteBuild(out _);

        // 서빙 성공/실패 SFX는 ServiceManager.OnServed 구독(AudioManager)에서 처리됩니다.
        ServiceManager.Instance.TryServe(customer, out string message);
        OnResult?.Invoke(message);

        // 서빙 후에도 빌드가 남아있으면(실패) 컵을 드는 상태로 되돌립니다.
        if (BeverageBuildManager.Instance.IsBuildActive)
            ReturnToHeld();
    }

    void ReturnToHeld()
    {
        _onMachine = false;
        if (_rect != null)
            _rect.anchoredPosition = _heldHome;
    }

    void OnDisable()
    {
        _dragging = false;
    }
}
