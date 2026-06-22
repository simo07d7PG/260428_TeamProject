using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>인벤토리 패널에 가로 스크롤 뷰포트를 런타임 구성합니다.</summary>
public static class InventoryScrollUtility
{
    const string ViewportName = "ScrollViewport";
    const string ContentName = "ScrollContent";

    public static Transform EnsureSetup(Transform panelRoot, float itemSize, float spacing)
    {
        if (panelRoot == null)
            return null;

        Transform existingContent = panelRoot.Find($"{ViewportName}/{ContentName}");
        if (existingContent != null)
            return existingContent;

        if (panelRoot.TryGetComponent(out RectMask2D panelMask))
            Object.Destroy(panelMask);

        if (panelRoot.TryGetComponent(out HorizontalLayoutGroup panelLayout))
            Object.Destroy(panelLayout);

        ScrollRect scrollRect = ManagerUtility.GetOrAddComponent<ScrollRect>(panelRoot.gameObject);
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.scrollSensitivity = 25f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;

        RectTransform viewportRect = CreateViewport(panelRoot);
        RectTransform contentRect = CreateContent(viewportRect, itemSize, spacing);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        ManagerUtility.GetOrAddComponent<InventoryHorizontalScroll>(viewportRect.gameObject)
            .Initialize(scrollRect);

        return contentRect;
    }

    static RectTransform CreateViewport(Transform panelRoot)
    {
        GameObject viewportGo = new GameObject(
            ViewportName,
            typeof(RectTransform),
            typeof(RectMask2D),
            typeof(Image));

        RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportGo.transform.SetParent(panelRoot, false);
        StretchFull(viewportRect);

        Image viewportImage = viewportGo.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        return viewportRect;
    }

    static RectTransform CreateContent(RectTransform viewportRect, float itemSize, float spacing)
    {
        GameObject contentGo = new GameObject(
            ContentName,
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(ContentSizeFitter));

        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentGo.transform.SetParent(viewportRect, false);

        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, itemSize);

        HorizontalLayoutGroup layout = contentGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.padding = new RectOffset(4, 4, 0, 0);

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        return contentRect;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}

/// <summary>마우스 휠로 인벤토리를 좌우 스크롤합니다.</summary>
public class InventoryHorizontalScroll : MonoBehaviour, IScrollHandler
{
    ScrollRect _scrollRect;

    public void Initialize(ScrollRect scrollRect)
    {
        _scrollRect = scrollRect;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_scrollRect == null || _scrollRect.content == null)
            return;

        float scrollableWidth = _scrollRect.content.rect.width - _scrollRect.viewport.rect.width;
        if (scrollableWidth <= 0f)
            return;

        Vector2 position = _scrollRect.content.anchoredPosition;
        position.x -= eventData.scrollDelta.y * _scrollRect.scrollSensitivity;
        position.x = Mathf.Clamp(position.x, -scrollableWidth, 0f);
        _scrollRect.content.anchoredPosition = position;
    }
}