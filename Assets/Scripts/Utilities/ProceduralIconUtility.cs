using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메뉴/재료 아이콘을 코드로 합성해 캐시하는 절차적 아이콘 유틸리티입니다.
/// 64x64 RGBA32 텍스처에 픽셀을 직접 그려 Sprite로 반환합니다.
/// 스프라이트 에셋이 없을 때의 폴백 아이콘으로 사용합니다.
/// </summary>
public static class ProceduralIconUtility
{
    const int Size = 64;
    const float PixelsPerUnit = 100f;

    static readonly Dictionary<string, Sprite> _menuCache = new Dictionary<string, Sprite>();
    static readonly Dictionary<int, Sprite> _ingredientCache = new Dictionary<int, Sprite>();

    // 팔레트 (0~1 RGBA)
    static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);
    static readonly Color Espresso = new Color(0.30f, 0.18f, 0.09f, 1f); // 짙은 갈색
    static readonly Color Cream = new Color(0.96f, 0.91f, 0.78f, 1f); // 크림색
    static readonly Color SyrupDot = new Color(0.55f, 0.32f, 0.10f, 1f); // 갈색 점
    static readonly Color Whip = new Color(1f, 1f, 1f, 1f); // 흰 휘핑
    static readonly Color IceTint = new Color(0.62f, 0.80f, 0.92f, 1f); // 옅은 하늘색
    static readonly Color CupOutline = new Color(0.20f, 0.14f, 0.10f, 1f); // 컵 테두리

    static readonly Color Amber = new Color(0.85f, 0.62f, 0.20f, 1f); // 호박색
    static readonly Color Pink = new Color(0.98f, 0.78f, 0.83f, 1f); // 연분홍
    static readonly Color Tan = new Color(0.78f, 0.58f, 0.36f, 1f); // 탄색

    /// <summary>
    /// 메뉴 구성에 맞춰 컵 실루엣 안에 색 밴드를 쌓은 아이콘을 반환합니다.
    /// menu.menuName 을 캐시 키로 사용합니다.
    /// </summary>
    public static Sprite GetMenuIcon(MenuDefinition menu)
    {
        if (menu == null)
            return null;

        string key = string.IsNullOrEmpty(menu.menuName) ? "__menu__" : menu.menuName;
        if (_menuCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Sprite sprite = CreateSprite(BuildMenuPixels(menu), key + "_Icon");
        _menuCache[key] = sprite;
        return sprite;
    }

    /// <summary>
    /// 재료 종류별 색의 둥근 칩 아이콘을 반환합니다.
    /// ingredient 인스턴스 ID를 캐시 키로 사용합니다.
    /// </summary>
    public static Sprite GetIngredientIcon(IngredientSO ingredient)
    {
        if (ingredient == null)
            return null;

        int key = ingredient.GetInstanceID();
        if (_ingredientCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        string name = string.IsNullOrEmpty(ingredient.ingredientName) ? "Ingredient" : ingredient.ingredientName;
        Sprite sprite = CreateSprite(BuildIngredientPixels(ingredient.type), name + "_Icon");
        _ingredientCache[key] = sprite;
        return sprite;
    }

    // ---------------------------------------------------------------------
    // 픽셀 생성
    // ---------------------------------------------------------------------

    static Color[] BuildMenuPixels(MenuDefinition menu)
    {
        Color[] pixels = NewTransparent();

        // 컵 영역: 위로 살짝 좁아지는 사다리꼴 실루엣
        const int cupBottom = 8;
        const int cupTop = 56;
        const float bottomHalf = 18f;
        const float topHalf = 22f;
        const float centerX = (Size - 1) * 0.5f;
        float cupHeight = cupTop - cupBottom;

        // 내용물 높이 계산 (컵 내부를 채우는 비율)
        int shots = Mathf.Max(0, menu.requiredShots);
        float milk = Mathf.Clamp01(menu.milkAmount);

        // 에스프레소: 샷 수에 비례(0.18~0.55), 밀크가 많으면 비중 축소
        float espressoFrac = shots <= 0 ? 0f : Mathf.Clamp(0.18f + shots * 0.16f, 0.18f, 0.6f);
        // 밀크: milkAmount 비율
        float milkFrac = milk * 0.6f;

        // 컵 안쪽 채움 높이(픽셀)
        float fillTop = cupBottom + cupHeight * 0.92f;
        float espressoTopY = cupBottom + cupHeight * espressoFrac;
        float milkTopY = espressoTopY + cupHeight * milkFrac;
        if (milkTopY > fillTop)
            milkTopY = fillTop;

        bool ice = menu.requiresIce;

        for (int y = cupBottom; y < cupTop; y++)
        {
            float t = (y - cupBottom) / cupHeight;
            float half = Mathf.Lerp(bottomHalf, topHalf, t);

            for (int x = 0; x < Size; x++)
            {
                float dx = x - centerX;
                float adx = Mathf.Abs(dx);

                if (adx > half + 0.5f)
                    continue;

                bool isOutline = adx > half - 1.5f;

                if (isOutline)
                {
                    SetPx(pixels, x, y, CupOutline);
                    continue;
                }

                // 내용물 색 결정
                Color content;
                if (y < espressoTopY)
                    content = Espresso;
                else if (y < milkTopY)
                    content = Cream;
                else
                    continue; // 컵 윗부분 빈 공간

                if (ice)
                    content = Color.Lerp(content, IceTint, 0.28f);

                SetPx(pixels, x, y, content);
            }
        }

        // 시럽 점: 밀크/에스프레소 경계 부근에 작은 갈색 점들
        if (menu.syrupCount > 0)
        {
            int dots = Mathf.Clamp(menu.syrupCount, 1, 4);
            int dotY = Mathf.RoundToInt(Mathf.Clamp(espressoTopY + 3f, cupBottom + 2, cupTop - 4));
            float spread = 9f;
            for (int i = 0; i < dots; i++)
            {
                float offset = dots == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(dots - 1));
                int dotX = Mathf.RoundToInt(centerX + offset);
                DrawDisc(pixels, dotX, dotY, 2.2f, SyrupDot, requireOpaque: true);
            }
        }

        // 휘핑 캡: 상단에 흰 반구
        if (menu.toppingCount > 0)
        {
            float capY = Mathf.Min(milkTopY, fillTop);
            int capCenterY = Mathf.RoundToInt(Mathf.Clamp(capY, cupBottom + 6, cupTop - 2));
            DrawCap(pixels, Mathf.RoundToInt(centerX), capCenterY, topHalf - 4f, Whip);
        }

        return pixels;
    }

    static Color[] BuildIngredientPixels(IngredientType type)
    {
        Color[] pixels = NewTransparent();

        Color fill = type switch
        {
            IngredientType.Base => Espresso,
            IngredientType.Milk => Cream,
            IngredientType.Syrup => Amber,
            IngredientType.Topping => Pink,
            IngredientType.DessertBase => Tan,
            _ => Cream
        };

        Color border = Color.Lerp(fill, Color.black, 0.25f);
        border.a = 0.55f; // 옅은 테두리

        const float centerX = (Size - 1) * 0.5f;
        const float centerY = (Size - 1) * 0.5f;
        const float radius = 26f;
        const float borderWidth = 2.5f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radius + 0.5f)
                    continue;

                if (dist > radius - borderWidth)
                {
                    SetPx(pixels, x, y, border);
                }
                else
                {
                    // 둥근 칩에 약간의 입체감(상단 밝게)
                    float shade = Mathf.Clamp01(0.5f - dy / (radius * 2f));
                    Color c = Color.Lerp(fill, Color.white, shade * 0.18f);
                    c.a = 1f;
                    SetPx(pixels, x, y, c);
                }
            }
        }

        return pixels;
    }

    // ---------------------------------------------------------------------
    // 드로잉 헬퍼
    // ---------------------------------------------------------------------

    static void DrawDisc(Color[] pixels, int cx, int cy, float radius, Color color, bool requireOpaque)
    {
        int r = Mathf.CeilToInt(radius);
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= Size || y < 0 || y >= Size)
                    continue;

                float dx = x - cx;
                float dy = y - cy;
                if (dx * dx + dy * dy > radius * radius)
                    continue;

                // 컵 내부(이미 그려진 픽셀) 위에만 점을 찍어 밖으로 새지 않게 함
                if (requireOpaque && pixels[y * Size + x].a < 0.99f)
                    continue;

                SetPx(pixels, x, y, color);
            }
        }
    }

    static void DrawCap(Color[] pixels, int cx, int cy, float halfWidth, Color color)
    {
        int r = Mathf.CeilToInt(halfWidth);
        for (int y = cy; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= Size || y < 0 || y >= Size)
                    continue;

                float dx = (x - cx) / halfWidth;
                float dy = (y - cy) / halfWidth;
                if (dx * dx + dy * dy > 1f)
                    continue;

                SetPx(pixels, x, y, color);
            }
        }
    }

    // ---------------------------------------------------------------------
    // 텍스처/스프라이트 유틸
    // ---------------------------------------------------------------------

    static Color[] NewTransparent()
    {
        Color[] pixels = new Color[Size * Size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Transparent;
        return pixels;
    }

    static void SetPx(Color[] pixels, int x, int y, Color color)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            return;
        pixels[y * Size + x] = color;
    }

    static Sprite CreateSprite(Color[] pixels, string name)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = name
        };

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        sprite.name = name;
        return sprite;
    }
}
