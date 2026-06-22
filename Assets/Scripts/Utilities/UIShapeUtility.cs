using UnityEngine;

/// <summary>
/// UI용 절차적 도형 스프라이트(원판 등)를 캐시해 제공합니다.
/// 기본 UI Image는 사각형이라 Radial360으로 채워도 모서리가 각져 보입니다.
/// 진짜 둥근 게이지가 필요할 때 이 원판 스프라이트를 Image.sprite로 지정하면
/// 채움(Filled/Radial360)이 원형 부채꼴로 잘립니다. 색은 Image.color로 지정합니다.
/// </summary>
public static class UIShapeUtility
{
    static Sprite _disc;
    static Sprite _ring;

    /// <summary>안티에일리어싱된 흰색 원판(중심 피벗).</summary>
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
                float a = Mathf.Clamp01(r - d); // 가장자리 1px 부드럽게
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        _disc = Build(px, size, "UIDisc");
        return _disc;
    }

    /// <summary>가는 흰색 링(원형 테두리). 다이얼 외곽선 용도.</summary>
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
