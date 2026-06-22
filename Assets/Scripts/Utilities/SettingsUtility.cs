using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>사운드/그래픽 설정을 PlayerPrefs에 저장하고 적용하는 유틸리티입니다.</summary>
public static class SettingsUtility
{
    const string KeyVolume = "opt_master_volume";
    const string KeyFullscreen = "opt_fullscreen";
    const string KeyResW = "opt_res_w";
    const string KeyResH = "opt_res_h";
    const string KeyMsaa = "opt_msaa";

    public static readonly int[] MsaaOptions = { 1, 2, 4, 8 };

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyVolume, 1f);
        set
        {
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyVolume, v);
            AudioListener.volume = v;
            PlayerPrefs.Save();
        }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int MsaaSamples
    {
        get => PlayerPrefs.GetInt(KeyMsaa, 1);
        set
        {
            int s = Mathf.Max(1, value);
            PlayerPrefs.SetInt(KeyMsaa, s);
            ApplyMsaa(s);
            PlayerPrefs.Save();
        }
    }

    public static void ApplyResolution(int width, int height, bool fullscreen)
    {
        if (width <= 0 || height <= 0)
            return;

        PlayerPrefs.SetInt(KeyResW, width);
        PlayerPrefs.SetInt(KeyResH, height);
        PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Screen.SetResolution(width, height,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    static void ApplyMsaa(int samples)
    {
        int s = samples <= 1 ? 0 : samples;
        QualitySettings.antiAliasing = s;

        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null)
            return;

        PropertyInfo prop = pipeline.GetType().GetProperty("msaaSampleCount");
        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
            prop.SetValue(pipeline, samples <= 1 ? 1 : samples);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStartup()
    {
        AudioListener.volume = MasterVolume;
        ApplyMsaa(MsaaSamples);

        if (PlayerPrefs.HasKey(KeyResW) && PlayerPrefs.HasKey(KeyResH))
        {
            int w = PlayerPrefs.GetInt(KeyResW);
            int h = PlayerPrefs.GetInt(KeyResH);
            bool full = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            if (w > 0 && h > 0)
                Screen.SetResolution(w, h,
                    full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
