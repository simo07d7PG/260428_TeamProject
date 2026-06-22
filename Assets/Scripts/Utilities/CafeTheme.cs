using UnityEngine;

/// <summary>
/// UI 색 팔레트의 단일 출처입니다. CafeAssetConfig에 색이 지정(알파 &gt; 0)되면 그 값을, 아니면 기본값을 씁니다.
/// 흩어져 중복되던 하드코드 색(인내심 3단계·패널·버튼·음료 레이어)을 한곳에 모아
/// 일관성과 인스펙터 오버라이드를 동시에 제공합니다.
/// </summary>
public static class CafeTheme
{
    // --- 기본 팔레트(코드 폴백) -------------------------------------------
    static readonly Color DefaultPanel = new Color(0.13f, 0.12f, 0.11f, 0.96f);
    static readonly Color DefaultPrimary = new Color(0.25f, 0.5f, 0.78f, 1f);
    static readonly Color DefaultSuccess = new Color(0.30f, 0.62f, 0.36f, 1f);
    static readonly Color DefaultDanger = new Color(0.6f, 0.3f, 0.3f, 1f);

    static readonly Color DefaultPatienceHigh = new Color(0.35f, 0.8f, 0.4f, 1f);
    static readonly Color DefaultPatienceMid = new Color(0.95f, 0.8f, 0.3f, 1f);
    static readonly Color DefaultPatienceLow = new Color(0.9f, 0.35f, 0.3f, 1f);

    static readonly Color DefaultEspresso = new Color(0.30f, 0.18f, 0.10f, 1f);
    static readonly Color DefaultMilk = new Color(0.96f, 0.93f, 0.84f, 1f);
    static readonly Color DefaultSyrup = new Color(0.55f, 0.33f, 0.10f, 1f);
    static readonly Color DefaultTopping = new Color(0.99f, 0.98f, 0.96f, 1f);
    static readonly Color DefaultIce = new Color(0.75f, 0.88f, 0.95f, 0.7f);

    static CafeAssetConfig Cfg => CafeAssetConfig.Instance;

    /// <summary>인스펙터 지정(알파 &gt; 0)이면 그 색, 아니면 폴백.</summary>
    static Color Pick(Color configured, Color fallback) => configured.a > 0f ? configured : fallback;

    public static Color Panel => Cfg != null ? Pick(Cfg.PanelColor, DefaultPanel) : DefaultPanel;
    public static Color PrimaryButton => Cfg != null ? Pick(Cfg.PrimaryButtonColor, DefaultPrimary) : DefaultPrimary;
    public static Color SuccessButton => Cfg != null ? Pick(Cfg.SuccessButtonColor, DefaultSuccess) : DefaultSuccess;
    public static Color DangerButton => Cfg != null ? Pick(Cfg.DangerButtonColor, DefaultDanger) : DefaultDanger;

    public static Color Espresso => Cfg != null ? Pick(Cfg.EspressoColor, DefaultEspresso) : DefaultEspresso;
    public static Color Milk => Cfg != null ? Pick(Cfg.MilkColor, DefaultMilk) : DefaultMilk;
    public static Color Syrup => Cfg != null ? Pick(Cfg.SyrupColor, DefaultSyrup) : DefaultSyrup;
    public static Color Topping => Cfg != null ? Pick(Cfg.ToppingColor, DefaultTopping) : DefaultTopping;
    public static Color Ice => Cfg != null ? Pick(Cfg.IceColor, DefaultIce) : DefaultIce;

    static Color PatienceHigh => Cfg != null ? Pick(Cfg.PatienceHighColor, DefaultPatienceHigh) : DefaultPatienceHigh;
    static Color PatienceMid => Cfg != null ? Pick(Cfg.PatienceMidColor, DefaultPatienceMid) : DefaultPatienceMid;
    static Color PatienceLow => Cfg != null ? Pick(Cfg.PatienceLowColor, DefaultPatienceLow) : DefaultPatienceLow;

    /// <summary>인내심 비율(0~1)에 따른 바 색. 0.5↑ 녹색, 0.25↑ 노랑, 그 이하 빨강.</summary>
    public static Color PatienceColor(float ratio)
    {
        if (ratio > 0.5f)
            return PatienceHigh;
        if (ratio > 0.25f)
            return PatienceMid;
        return PatienceLow;
    }
}
