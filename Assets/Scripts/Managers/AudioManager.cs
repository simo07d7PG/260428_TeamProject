using System.Collections.Generic;
using UnityEngine;

/// <summary>효과음을 재생하는 싱글톤 매니저입니다. 클립은 Resources/Audio/{key} 에서 선택적으로 로드합니다.</summary>
[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    readonly Dictionary<string, AudioClip> _cache = new();
    AudioSource _source;
    AudioSource _bgmSource;
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

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
    }

    void Start()
    {
        Subscribe();
        PlayBgm();
    }

    void PlayBgm()
    {
        if (_bgmSource == null)
            return;

        AudioClip clip = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.Bgm : null;
        if (clip == null)
            clip = Resources.Load<AudioClip>("Bgm/MP_Background");
        if (clip == null)
            return;

        _bgmSource.clip = clip;
        _bgmSource.volume = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.BgmVolume : 0.45f;
        if (!_bgmSource.isPlaying)
            _bgmSource.Play();
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
        AudioClip configured = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GetSfx(key) : null;
        if (configured != null)
            return configured;

        if (_cache.TryGetValue(key, out AudioClip cached))
            return cached;

        AudioClip clip = Resources.Load<AudioClip>($"Audio/{key}");
        if (clip == null)
        {
            string bgmFile = MapToBgmFile(key);
            if (bgmFile != null)
                clip = Resources.Load<AudioClip>($"Bgm/{bgmFile}");
        }
        if (clip == null) clip = ProceduralAudioUtility.Get(key);
        _cache[key] = clip;
        return clip;
    }

    static string MapToBgmFile(string key)
    {
        return key switch
        {
            "shot_extract" => "CoffeeMachineSound",
            "coin" => "SuccessSound",
            "merge_success" => "SuccessSound",
            "milk_pour" => "WaterSound",
            "cup_take" => "ButtonClickSound",
            "cup_place" => "ButtonClickSound",
            "lid_close" => "ButtonClickSound",
            "ui_click" => "ButtonClickSound",
            _ => null
        };
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
