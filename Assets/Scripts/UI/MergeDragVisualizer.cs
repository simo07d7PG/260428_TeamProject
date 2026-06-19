using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Merge UI 드래그 중 커서를 따라다니는 고스트 아이콘을 표시합니다.
/// </summary>
public static class MergeDragVisualizer
{
    const float GhostScale = 1.1f;
    const float SourceAlpha = 0.35f;

    static RectTransform _ghostRect;
    static Image _ghostImage;
    static Canvas _canvas;
    static CanvasGroup _sourceCanvasGroup;

    public static void Begin(Sprite sprite, PointerEventData eventData, Vector2 size, CanvasGroup sourceCanvasGroup)
    {
        End();

        if (eventData == null)
            return;

        _canvas = eventData.pointerPressRaycast.gameObject != null
            ? eventData.pointerPressRaycast.gameObject.GetComponentInParent<Canvas>()
            : null;

        if (_canvas == null)
            return;

        _sourceCanvasGroup = sourceCanvasGroup;
        if (_sourceCanvasGroup != null)
        {
            _sourceCanvasGroup.alpha = SourceAlpha;
            _sourceCanvasGroup.blocksRaycasts = false;
        }

        GameObject ghostObject = new GameObject("MergeDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ghostObject.transform.SetParent(_canvas.transform, false);
        ghostObject.transform.SetAsLastSibling();

        _ghostRect = ghostObject.GetComponent<RectTransform>();
        _ghostRect.sizeDelta = size;
        _ghostRect.localScale = Vector3.one * GhostScale;

        _ghostImage = ghostObject.GetComponent<Image>();
        _ghostImage.sprite = sprite;
        _ghostImage.color = sprite != null
            ? Color.white
            : new Color(0.75f, 0.75f, 0.75f, 0.85f);
        _ghostImage.raycastTarget = false;
        _ghostImage.preserveAspect = true;

        UpdatePosition(eventData);
    }

    public static void UpdatePosition(PointerEventData eventData)
    {
        if (_ghostRect == null || _canvas == null || eventData == null)
            return;

        RectTransform canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            _ghostRect.localPosition = localPoint;
        }
    }

    public static void End()
    {
        if (_sourceCanvasGroup != null)
        {
            _sourceCanvasGroup.alpha = 1f;
            _sourceCanvasGroup.blocksRaycasts = true;
            _sourceCanvasGroup = null;
        }

        if (_ghostRect != null)
        {
            Object.Destroy(_ghostRect.gameObject);
            _ghostRect = null;
            _ghostImage = null;
        }

        _canvas = null;
    }
}