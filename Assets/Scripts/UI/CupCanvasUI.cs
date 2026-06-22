using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>제작 중인 음료의 컵과 레이어를 실시간 렌더링합니다.</summary>
public class CupCanvasUI : MonoBehaviour
{
    [SerializeField] RectTransform liquidArea;
    [SerializeField] RectTransform decorArea;
    [SerializeField] Image espressoFill;
    [SerializeField] Image milkFill;
    [SerializeField] Image lidImage;
    [SerializeField] TextMeshProUGUI emptyHint;

    readonly List<Image> _dropPool = new();
    int _activeDrops;

    public RectTransform DecorArea => decorArea;

    public void Bind(
        RectTransform liquid,
        RectTransform decor,
        Image espresso,
        Image milk,
        Image lid,
        TextMeshProUGUI hint)
    {
        liquidArea = liquid;
        decorArea = decor;
        espressoFill = espresso;
        milkFill = milk;
        lidImage = lid;
        emptyHint = hint;
    }

    public void Render(BeverageBuildSnapshot snapshot)
    {
        if (snapshot == null || liquidArea == null)
            return;

        float area = liquidArea.rect.height;
        if (area <= 0f)
            area = liquidArea.sizeDelta.y;

        float espressoFraction = snapshot.ShotCount > 0
            ? Mathf.Clamp(0.20f + 0.12f * (snapshot.ShotCount - 1), 0f, 0.5f)
            : 0f;
        float milkFraction = Mathf.Clamp01(snapshot.MilkAmount) * 0.62f;

        ApplyFill(espressoFill, area * espressoFraction, 0f, CafeTheme.Espresso, espressoFraction > 0f);
        ApplyFill(milkFill, area * milkFraction, area * espressoFraction, CafeTheme.Milk, milkFraction > 0f);

        _activeDrops = 0;
        RenderPoints(snapshot.GetLayer(StationType.Syrup), CafeTheme.Syrup, 14f);
        RenderPoints(snapshot.GetLayer(StationType.Topping), CafeTheme.Topping, 30f);
        HideUnusedDrops();

        if (lidImage != null)
            lidImage.enabled = snapshot.HasLid;

        if (emptyHint != null)
            emptyHint.gameObject.SetActive(snapshot.IsEmpty);
    }

    static void ApplyFill(Image fill, float height, float bottomOffset, Color color, bool visible)
    {
        if (fill == null)
            return;

        fill.enabled = visible;
        if (!visible)
            return;

        fill.color = color;
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = new Vector2(0f, bottomOffset);
    }

    void RenderPoints(BeverageLayer layer, Color color, float size)
    {
        if (layer == null || layer.positions == null || decorArea == null)
            return;

        Vector2 areaSize = decorArea.rect.size;

        foreach (Vector2 norm in layer.positions)
        {
            Image dot = GetDrop(_activeDrops++);
            dot.color = color;
            RectTransform rect = dot.rectTransform;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(
                Mathf.Clamp01(norm.x) * areaSize.x,
                Mathf.Clamp01(norm.y) * areaSize.y);
            dot.enabled = true;
            dot.gameObject.SetActive(true);
        }
    }

    Image GetDrop(int index)
    {
        while (_dropPool.Count <= index)
        {
            GameObject dotObject = new GameObject("Drop", typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(decorArea, false);
            RectTransform rect = dotObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = dotObject.GetComponent<Image>();
            image.raycastTarget = false;
            _dropPool.Add(image);
        }

        return _dropPool[index];
    }

    void HideUnusedDrops()
    {
        for (int i = _activeDrops; i < _dropPool.Count; i++)
        {
            if (_dropPool[i] != null)
                _dropPool[i].gameObject.SetActive(false);
        }
    }

    /// <summary>화면 좌표가 컵 영역 위에 있는지 검사합니다. (도구 드래그 히트테스트)</summary>
    public bool IsOverCup(Vector2 screenPosition, Camera eventCamera)
    {
        return decorArea != null
            && RectTransformUtility.RectangleContainsScreenPoint(decorArea, screenPosition, eventCamera);
    }

    /// <summary>화면 좌표를 컵(데코 영역) 정규화 좌표(0~1)로 변환합니다.</summary>
    public bool TryGetNormalizedPoint(Vector2 screenPosition, Camera eventCamera, out Vector2 normalized)
    {
        normalized = Vector2.zero;
        if (decorArea == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                decorArea, screenPosition, eventCamera, out Vector2 local))
            return false;

        Vector2 size = decorArea.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            return false;

        normalized = new Vector2(
            Mathf.Clamp01(local.x / size.x + decorArea.pivot.x),
            Mathf.Clamp01(local.y / size.y + decorArea.pivot.y));
        return true;
    }
}
