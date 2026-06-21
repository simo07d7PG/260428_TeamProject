using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하나의 스테이션 도구(에스프레소 머신, 밀크 피처, 시럽 병 등)를 표현하는 UI 조각입니다.
/// 배경 버튼 이미지, 라벨, 진행 게이지를 묶어 컨트롤러가 피드백을 갱신할 수 있게 합니다.
/// </summary>
public class BeverageStationUI : MonoBehaviour
{
    [SerializeField] StationType station;
    [SerializeField] Image background;
    [SerializeField] Image gaugeFill;
    [SerializeField] TextMeshProUGUI label;

    Color _baseColor = new(0.22f, 0.26f, 0.32f, 1f);

    public StationType Station => station;

    public void Bind(StationType stationType, Image backgroundImage, Image gauge, TextMeshProUGUI labelText)
    {
        station = stationType;
        background = backgroundImage;
        gaugeFill = gauge;
        label = labelText;

        if (background != null)
            _baseColor = background.color;

        SetGauge(0f);
    }

    static readonly Color GaugeNormal = new(0.30f, 0.55f, 0.85f, 0.45f);
    static readonly Color GaugeSweet = new(0.35f, 0.82f, 0.45f, 0.6f);

    public void SetGauge(float normalized)
    {
        if (gaugeFill == null)
            return;

        gaugeFill.fillAmount = Mathf.Clamp01(normalized);
        gaugeFill.enabled = normalized > 0.001f;
    }

    /// <summary>게이지 채움 + 최적 구간(sweetSpot)이면 초록 강조. 샷 타이밍 가이드용.</summary>
    public void SetGauge(float normalized, bool sweetSpot)
    {
        SetGauge(normalized);
        if (gaugeFill != null)
            gaugeFill.color = sweetSpot ? GaugeSweet : GaugeNormal;
    }

    public void SetLabel(string text)
    {
        if (label != null)
            label.text = UIFontUtility.Sanitize(text);
    }

    public void SetHighlight(bool on)
    {
        if (background == null)
            return;

        background.color = on
            ? new Color(0.30f, 0.52f, 0.78f, 1f)
            : _baseColor;
    }
}
