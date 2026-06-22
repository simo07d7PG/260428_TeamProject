using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>스테이션/컵 스프라이트를 Resources에서 로드합니다. 없으면 null을 반환해 색 폴백을 쓰게 합니다.</summary>
public static class CafeSpriteUtility
{
    static readonly Dictionary<string, Sprite> _cache = new();

    public static Sprite Station(string name)
    {
        Sprite configured = CafeAssetConfig.Instance != null ? CafeAssetConfig.Instance.GetStationSprite(name) : null;
        if (configured != null)
            return configured;

        foreach (string path in ResourcePaths(name))
        {
            Sprite sprite = Load(path);
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    static IEnumerable<string> ResourcePaths(string name)
    {
        yield return $"Sprites/Stations/{name}";

        switch (name)
        {
            case "Cup":
                yield return "Ingredients/cup";
                break;
            case "EspressoShot":
                yield return "Coffee/CoffeeMachine0";
                yield return "Coffee/CoffeeMachine1";
                break;
            case "Milk":
            case "SteamMilk":
                yield return "Ingredients/milk";
                break;
            case "Syrup":
                yield return "Ingredients/syrup";
                break;
            case "Topping":
                yield return "Ingredients/cream";
                yield return "Ingredients/chocolatechips";
                break;
        }
    }

    public static Sprite Cup(string name) => Load($"Sprites/Cup/{name}");

    static Sprite Load(string path)
    {
        if (_cache.TryGetValue(path, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>(path);
        _cache[path] = sprite;
        return sprite;
    }

    public static void ApplyStation(Image image, string name, Color fallback)
    {
        if (image == null)
            return;

        Sprite sprite = Station(name);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
        }
        else
        {
            image.sprite = null;
            image.color = fallback;
        }
    }
}
