using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음을 재생하는 싱글톤 매니저입니다. 클립은 Resources/Audio/{key} 에서 선택적으로 로드합니다.
/// 에셋이 없으면 조용히 무시하며(에러 없음), 기존 매니저 이벤트에 구독해 자동 재생합니다.
/// 사용자가 Resources/Audio 에 다음 이름의 클립을 넣으면 소리가 납니다:
/// merge_success, coin, serve_fail, customer_arrive, customer_leave, drink_complete
/// </summary>
[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    readonly Dictionary<string, AudioClip> _cache = new();
    AudioSource _source;
    bool _subscribed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _source = ManagerUtility.GetOrAddComponent<AudioSource>(gameObject);
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
    }

    void Start()
    {
        Subscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    public static void PlaySfx(string key, float volume = 1f)
    {
        Instance?.Play(key, volume);
    }

    public void Play(string key, float volume = 1f)
    {
        if (_source == null || string.IsNullOrEmpty(key))
            return;

        AudioClip clip = Resolve(key);
        if (clip != null)
            _source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    AudioClip Resolve(string key)
    {
        // 인스펙터(CafeAssetConfig) 지정이 최우선.
        AudioClip configured = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GetSfx(key) : null;
        if (configured != null)
            return configured;

        if (_cache.TryGetValue(key, out AudioClip cached))
            return cached;

        AudioClip clip = Resources.Load<AudioClip>($"Audio/{key}");
        if (clip == null) clip = ProceduralAudioUtility.Get(key);
        _cache[key] = clip;
        return clip;
    }

    void Subscribe()
    {
        if (_subscribed)
            return;

        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnMergeCompleted += HandleMerge;
        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnServed += HandleServed;
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnCustomerArrived += HandleArrived;
            CustomerManager.Instance.OnCustomerLeft += HandleLeft;
        }
        if (BeverageBuildManager.Instance != null)
            BeverageBuildManager.Instance.OnBuildCompleted += HandleDrinkComplete;

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (PreparationManager.Instance != null)
            PreparationManager.Instance.OnMergeCompleted -= HandleMerge;
        if (ServiceManager.Instance != null)
            ServiceManager.Instance.OnServed -= HandleServed;
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnCustomerArrived -= HandleArrived;
            CustomerManager.Instance.OnCustomerLeft -= HandleLeft;
        }
        if (BeverageBuildManager.Instance != null)
            BeverageBuildManager.Instance.OnBuildCompleted -= HandleDrinkComplete;

        _subscribed = false;
    }

    void HandleMerge(MergeResult result)
    {
        if (result.resultType == MergeResultType.Success)
            Play("merge_success");
    }

    void HandleServed(Customer customer, ServingScoreBreakdown score, int payout)
    {
        Play(payout > 0 ? "coin" : "serve_fail");
    }

    void HandleArrived(Customer customer) => Play("customer_arrive");

    void HandleLeft(Customer customer) => Play("customer_leave");

    void HandleDrinkComplete(BeverageBuildSnapshot snapshot) => Play("drink_complete");
}
