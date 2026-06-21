using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 활성 트렌드를 상단 배너로 표시하고 TrendManager와 연동합니다.
/// </summary>
public class TrendBannerUIController : MonoBehaviour
{
    RectTransform _banner;
    TextMeshProUGUI _text;

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        EnsureManagers();
        BuildBanner();
        UIFontUtility.ApplyToHierarchy(transform);
    }

    void OnEnable()
    {
        if (TrendManager.Instance != null)
            TrendManager.Instance.OnTrendChanged += Refresh;

        Refresh();
    }

    void OnDisable()
    {
        if (TrendManager.Instance != null)
            TrendManager.Instance.OnTrendChanged -= Refresh;
    }

    void EnsureManagers()
    {
        if (PreparationManager.Instance != null && TrendManager.Instance == null)
            ManagerUtility.GetOrAddComponent<TrendManager>(PreparationManager.Instance.gameObject);
    }

    void BuildBanner()
    {
        GameObject bannerObject = UIFactoryUtility.CreateUIObject("TrendBanner", transform, typeof(Image));
        _banner = bannerObject.GetComponent<RectTransform>();
        _banner.anchorMin = new Vector2(0.5f, 1f);
        _banner.anchorMax = new Vector2(0.5f, 1f);
        _banner.pivot = new Vector2(0.5f, 1f);
        _banner.anchoredPosition = new Vector2(0f, -62f);
        _banner.sizeDelta = new Vector2(480f, 34f);
        bannerObject.GetComponent<Image>().color = new Color(0.42f, 0.28f, 0.55f, 0.92f);

        _text = UIFactoryUtility.CreateLabel(_banner, "TrendText", string.Empty, 16f);
        UIFactoryUtility.StretchFull(_text.rectTransform);
        _text.alignment = TextAlignmentOptions.Center;
    }

    void Refresh()
    {
        if (_banner == null)
            return;

        TrendSO active = TrendManager.Instance != null ? TrendManager.Instance.ActiveTrend : null;
        if (active == null)
        {
            _banner.gameObject.SetActive(false);
            return;
        }

        _banner.gameObject.SetActive(true);
        if (_text != null)
            _text.text = UIFontUtility.Sanitize($"오늘의 트렌드: {active.trendName} (x{active.multiplier:0.0})");
    }
}
