using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 등에 붙어 클릭 시 공통 효과음("ui_click")을 재생합니다.
/// Button.onClick 리스너가 교체(RemoveAllListeners)돼도 영향을 받지 않습니다.
/// 비활성(interactable=false) 상태에서는 소리를 내지 않습니다.
/// </summary>
public class UIClickSound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable)
            return;

        AudioManager.PlaySfx("ui_click");
    }
}
