using UnityEngine;

/// <summary>UI용 절차적 도형 스프라이트(원판/링)를 캐시해 제공합니다.</summary>
public static class UIShapeUtility
{
    static Sprite _disc;
    static Sprite _ring;

    public static Sprite Disc()
    {
        if (_disc != null)
            return _disc;

        const int size = 128;
        float r = size * 0.5f;
        Vector2 c = new Vector2(r - 0.5f, r - 0.5f);
        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(r - d);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        _disc = Build(px, size, "UIDisc");
        return _disc;
    }

    public static Sprite Ring()
    {
        if (_ring != null)
            return _ring;

        const int size = 128;
        float outer = size * 0.5f;
        float inner = outer - 7f;
        Vector2 c = new Vector2(outer - 0.5f, outer - 0.5f);
        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
        }

        _ring = Build(px, size, "UIRing");
        return _ring;
    }

    static Sprite Build(Color[] pixels, int size, string name)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = name
        };
        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        return sprite;
    }
}
