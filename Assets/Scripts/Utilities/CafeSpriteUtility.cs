using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이션/컵 스프라이트를 Resources에서 로드합니다. 없으면 null을 반환해 색 폴백을 쓰게 합니다.
/// 사용자가 다음 경로에 스프라이트를 넣으면 자동 적용됩니다:
///   Resources/Sprites/Stations/{name}  (예: EspressoShot, SteamMilk, Syrup, Topping, Lid, Ice, Cup, Counter)
///   Resources/Sprites/Cup/{name}        (예: cup, espresso, milk, lid)
/// </summary>
public static class CafeSpriteUtility
{
    static readonly Dictionary<string, Sprite> _cache = new();

    public static Sprite Station(string name)
    {
        // 인스펙터(CafeAssetConfig) 지정이 최우선, 없으면 Resources.
        Sprite configured = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GetStationSprite(name) : null;
        return configured != null ? configured : Load($"Sprites/Stations/{name}");
    }

    public static Sprite Cup(string name) => Load($"Sprites/Cup/{name}");

    static Sprite Load(string path)
    {
        if (_cache.TryGetValue(path, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>(path);
        _cache[path] = sprite; // null도 캐시(반복 로드 방지)
        return sprite;
    }

    /// <summary>스프라이트가 있으면 Image에 적용하고 흰색으로, 없으면 색 폴백을 유지합니다.</summary>
    public static void ApplyStation(Image image, string name, Color fallback)
    {
        if (image == null)
            return;

        Sprite sprite = Station(name);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true; // 입력 이미지의 가로세로 비를 유지(왜곡 방지)
            image.type = Image.Type.Simple;
        }
        else
        {
            image.sprite = null;
            image.color = fallback;
        }
    }
}
