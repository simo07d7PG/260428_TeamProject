using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>코인 획득·머지 성공·음료 완성 등에 대한 떠다니는 텍스트/파티클 피드백을 코드로 생성합니다.</summary>
public class FeedbackUIController : MonoBehaviour
{
    RectTransform _overlay;
    bool _subscribed;

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);

        RectTransform existing = transform.Find("FeedbackOverlay") as RectTransform;
        if (existing != null)
            BindOverlay(existing);
        else
            BuildOverlay();
    }

    /// <summary>에디터에서 씬에 오버레이 컨테이너를 굽기 위한 진입점.</summary>
    public void EditorBuild()
    {
        ConfigureHostTransform(transform as RectTransform);
        if (transform.Find("FeedbackOverlay") == null)
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

    void BindOverlay(RectTransform overlay)
    {
        _overlay = overlay;
    }

    void BuildOverlay()
    {
        GameObject overlayObject = UIFactoryUtility.CreateUIObject("FeedbackOverlay", transform);
        _overlay = overlayObject.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_overlay);
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
        {
            BeverageBuildManager.Instance.OnBuildCompleted += HandleDrinkComplete;
            BeverageBuildManager.Instance.OnShotPulled += HandleShotPulled;
        }

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
        {
            BeverageBuildManager.Instance.OnBuildCompleted -= HandleDrinkComplete;
            BeverageBuildManager.Instance.OnShotPulled -= HandleShotPulled;
        }

        _subscribed = false;
    }

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
        if (result.resultType == MergeResultType.Success)
            SpawnBurst(new Vector2(0f, 0f), new Color(1f, 0.92f, 0.5f), 12);
    }

    void HandleDrinkComplete(BeverageBuildSnapshot snapshot)
    {
        SpawnBurst(new Vector2(0f, 40f), new Color(0.6f, 0.85f, 1f), 8);
    }

    void HandleShotPulled()
    {
        SpawnSteam(new Vector2(0f, 90f), 4);
    }

    void SpawnSteam(Vector2 anchoredPos, int count)
    {
        if (_overlay == null)
            return;

        for (int i = 0; i < count; i++)
        {
            Image wisp = UIFactoryUtility.CreateImage(_overlay, "Steam", new Color(1f, 1f, 1f, 0.5f));
            wisp.raycastTarget = false;

            RectTransform rect = wisp.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos + new Vector2(Random.Range(-22f, 22f), Random.Range(-6f, 6f));
            rect.sizeDelta = new Vector2(14f, 14f);

            StartCoroutine(RiseSteam(wisp, rect, Random.Range(-14f, 14f)));
        }
    }

    System.Collections.IEnumerator RiseSteam(Image wisp, RectTransform rect, float drift)
    {
        float duration = Random.Range(0.9f, 1.3f);
        float t = 0f;
        Vector2 start = rect.anchoredPosition;
        Color baseColor = wisp.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            rect.anchoredPosition = start + new Vector2(drift * k, 120f * k);
            wisp.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - k));
            rect.localScale = Vector3.one * (1f + 1.2f * k);
            yield return null;
        }

        if (wisp != null)
            Destroy(wisp.gameObject);
    }

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
            rect.anchoredPosition = start + velocity * k - new Vector2(0f, 24f * k * k);
            spark.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
            rect.localScale = Vector3.one * (1f - 0.5f * k);
            yield return null;
        }

        if (spark != null)
            Destroy(spark.gameObject);
    }
}
