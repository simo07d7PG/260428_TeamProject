using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카페 카운터 위의 고정 스테이션 한 칸입니다. (에스프레소 머신, 우유, 시럽, 토핑, 리드, 얼음, 컵 통)
/// 컵이 이 영역 위로 오면 컨트롤러/컵 핸들러가 해당 동작을 적용합니다.
/// 배경 스프라이트는 Resources/Sprites/Stations/{StationType}에서 로드되며, 없으면 색으로 표시됩니다.
/// </summary>
public class CafeStationUI : MonoBehaviour
{
    [SerializeField] StationType station;
    [SerializeField] Image background;
    [SerializeField] Image gauge;
    [SerializeField] TextMeshProUGUI label;

    Color _baseColor = new(0.22f, 0.26f, 0.32f, 1f);

    public StationType Station => station;
    public RectTransform Rect => transform as RectTransform;

    static readonly Color GaugeNormal = new(0.30f, 0.55f, 0.85f, 0.5f);
    static readonly Color GaugeSweet = new(0.35f, 0.82f, 0.45f, 0.65f);

    public void Bind(StationType stationType, Image backgroundImage, Image gaugeImage, TextMeshProUGUI labelText, Color fallback)
    {
        station = stationType;
        background = backgroundImage;
        gauge = gaugeImage;
        label = labelText;
        _baseColor = fallback;
        SetGauge(0f);
    }

    /// <summary>화면 좌표가 이 스테이션 영역 위에 있는지.</summary>
    public bool ContainsScreen(Vector2 screenPos, Camera cam)
    {
        RectTransform rect = Rect;
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
    }

    public void SetGauge(float normalized, bool sweetSpot = false)
    {
        if (gauge == null)
            return;

        gauge.fillAmount = Mathf.Clamp01(normalized);
        gauge.enabled = normalized > 0.001f;
        gauge.color = sweetSpot ? GaugeSweet : GaugeNormal;
    }

    public void SetLabel(string text)
    {
        if (label != null)
            label.text = UIFontUtility.Sanitize(text);
    }

    public void SetHighlight(bool on)
    {
        if (background != null && background.sprite == null)
            background.color = on ? new Color(0.30f, 0.52f, 0.78f, 1f) : _baseColor;
        else if (background != null)
            background.color = on ? new Color(0.8f, 0.9f, 1f, 1f) : Color.white;
    }
}
