using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 코인 획득·머지 성공·음료 완성 등에 대한 떠다니는 텍스트/파티클 피드백을 코드로 생성합니다.
/// 에셋 없이 런타임 합성하며, 입력을 가리지 않는 최상단 오버레이에 표시합니다.
/// </summary>
public class FeedbackUIController : MonoBehaviour
{
    RectTransform _overlay;
    bool _subscribed;

    public static void ConfigureHostTransform(RectTransform host)
    {
        if (host == null)
            return;

        host.anchorMin = Vector2.zero;
        host.anchorMax = Vector2.one;
        host.pivot = new Vector2(0.5f, 0.5f);
        host.anchoredPosition = Vector2.zero;
        host.sizeDelta = Vector2.zero;
        host.localScale = Vector3.one;
    }

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        BuildOverlay();
    }

    void Start()
    {
        Subscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void BuildOverlay()
    {
        GameObject overlayObject = UIFactoryUtility.CreateUIObject("FeedbackOverlay", transform);
        _overlay = overlayObject.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_overlay);
        // 그래픽 컴포넌트가 없으므로 레이캐스트를 막지 않습니다.
    }

    void Subscribe()
    {
        if (_subscribed)
            return;

        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnServed += HandleServed;
        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnMergeCompleted += HandleMerge;
        if (BeverageBuildManager.Instance != null)
            BeverageBuildManager.Instance.OnBuildCompleted += HandleDrinkComplete;

        _subscribed = ServiceManager.Instance != null
            || PreparationManager.Instance != null
            || BeverageBuildManager.Instance != null;
    }

    void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnServed -= HandleServed;
        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnMergeCompleted -= HandleMerge;
        if (BeverageBuildManager.Instance != null)
            BeverageBuildManager.Instance.OnBuildCompleted -= HandleDrinkComplete;

        _subscribed = false;
    }

    // --- 이벤트 핸들러 ---

    void HandleServed(Customer customer, ServingScoreBreakdown score, int payout)
    {
        if (payout > 0)
        {
            SpawnFloatingText(new Vector2(0f, 40f), $"+{payout}", new Color(1f, 0.85f, 0.3f), 42f);
            SpawnBurst(new Vector2(0f, 40f), new Color(1f, 0.85f, 0.3f), 10);
        }
        else
        {
            SpawnFloatingText(new Vector2(0f, 40f), "환불", new Color(0.92f, 0.42f, 0.36f), 34f);
        }
    }

    void HandleMerge(MergeResult result)
    {
        // 성공 텍스트는 MergeUIController가 이미 표시하므로 여기선 스파클만.
        if (result.resultType == MergeResultType.Success)
            SpawnBurst(new Vector2(0f, 0f), new Color(1f, 0.92f, 0.5f), 12);
    }

    void HandleDrinkComplete(BeverageBuildSnapshot snapshot)
    {
        SpawnBurst(new Vector2(0f, 40f), new Color(0.6f, 0.85f, 1f), 8);
    }

    // --- 스폰 ---

    void SpawnFloatingText(Vector2 anchoredPos, string text, Color color, float fontSize)
    {
        if (_overlay == null)
            return;

        TextMeshProUGUI label = UIFactoryUtility.CreateLabel(_overlay, "FloatText", text, fontSize);
        label.color = color;
        label.fontStyle = FontStyles.Bold;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos + new Vector2(Random.Range(-30f, 30f), 0f);
        rect.sizeDelta = new Vector2(320f, 64f);

        StartCoroutine(RiseAndFade(label, rect));
    }

    IEnumerator RiseAndFade(TextMeshProUGUI label, RectTransform rect)
    {
        const float duration = 1.0f;
        float t = 0f;
        Vector2 start = rect.anchoredPosition;
        Color baseColor = label.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            rect.anchoredPosition = start + new Vector2(0f, 80f * k);
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
            yield return null;
        }

        if (label != null)
            Destroy(label.gameObject);
    }

    void SpawnBurst(Vector2 anchoredPos, Color color, int count)
    {
        if (_overlay == null)
            return;

        for (int i = 0; i < count; i++)
        {
            Image spark = UIFactoryUtility.CreateImage(_overlay, "Spark", color);
            spark.raycastTarget = false;

            RectTransform rect = spark.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(9f, 9f);

            float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(60f, 135f);
            StartCoroutine(ScatterAndFade(spark, rect, velocity));
        }
    }

    IEnumerator ScatterAndFade(Image spark, RectTransform rect, Vector2 velocity)
    {
        const float duration = 0.6f;
        float t = 0f;
        Vector2 start = rect.anchoredPosition;
        Color baseColor = spark.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            rect.anchoredPosition = start + velocity * k - new Vector2(0f, 24f * k * k); // 약한 중력
            spark.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
            rect.localScale = Vector3.one * (1f - 0.5f * k);
            yield return null;
        }

        if (spark != null)
            Destroy(spark.gameObject);
    }
}
