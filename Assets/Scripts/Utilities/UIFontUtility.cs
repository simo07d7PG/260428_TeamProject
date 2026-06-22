using TMPro;
using UnityEngine;

/// <summary>런타임 UI 텍스트에 한글 지원 TMP 폰트를 일관되게 적용합니다.</summary>
public static class UIFontUtility
{
    const string PrimaryFontPath = "Fonts/Pretendard-Medium SDF";
    const string FallbackFontPath = "Fonts/SunBatang-Medium SDF";

    static TMP_FontAsset _cachedFont;
    static bool _charactersWarmed;

    static readonly string WarmupCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        "%+-./:()x " +
        "가각간감값갑갓강갖같개거게경고공과관교구그금나내너는능단당대더도되동된등라런로료를만매먼명모무문미및발법변보복본부불비사산상새서선설성세소송수순시실아안않액야어없에연영예오요우원유으은을이인일자작잘재전정제조종주준중지차처리초최추출치카컵코콘크통투특판패펼폐피필하한합해행현확후흡희";

    public static TMP_FontAsset GetUIFont()
    {
        if (_cachedFont != null)
            return _cachedFont;

        _cachedFont = Resources.Load<TMP_FontAsset>(PrimaryFontPath);
        if (_cachedFont == null)
            _cachedFont = Resources.Load<TMP_FontAsset>(FallbackFontPath);

        if (_cachedFont == null)
        {
            TMP_FontAsset[] fonts = Resources.LoadAll<TMP_FontAsset>("Fonts");
            foreach (TMP_FontAsset font in fonts)
            {
                if (font == null)
                    continue;

                if (font.name.Contains("Pretendard") || font.name.Contains("SunBatang"))
                {
                    _cachedFont = font;
                    break;
                }
            }
        }

        if (_cachedFont == null && TMP_Settings.defaultFontAsset != null)
            _cachedFont = TMP_Settings.defaultFontAsset;

        if (_cachedFont == null)
            Debug.LogWarning("[UIFontUtility] 한글 UI 폰트를 찾지 못했습니다. Resources/Fonts 경로를 확인해 주세요.");

        WarmupCharactersIfNeeded(_cachedFont);
        return _cachedFont;
    }

    public static void Apply(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = GetUIFont();
        if (font == null)
            return;

        text.font = font;
        if (font.material != null)
            text.fontSharedMaterial = font.material;

        text.ForceMeshUpdate();
    }

    public static void ApplyToHierarchy(Transform root)
    {
        if (root == null)
            return;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text is TextMeshProUGUI ugui)
                Apply(ugui);
        }
    }

    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace('\u00d7', 'x')
            .Replace('\u2014', '-')
            .Replace('\u00b7', '|')
            .Replace('\u2192', '>');
    }

    static void WarmupCharactersIfNeeded(TMP_FontAsset font)
    {
        if (font == null || _charactersWarmed)
            return;

        font.TryAddCharacters(WarmupCharacters, out _);
        _charactersWarmed = true;
    }
}