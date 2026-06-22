using System.Collections.Generic;
using UnityEngine;

/// <summary>메뉴/재료 아이콘을 코드로 합성해 캐시하는 절차적 아이콘 유틸리티입니다.</summary>
public static class ProceduralIconUtility
{
    const int Size = 64;
    const float PixelsPerUnit = 100f;

    static readonly Dictionary<string, Sprite> _menuCache = new Dictionary<string, Sprite>();
    static readonly Dictionary<int, Sprite> _ingredientCache = new Dictionary<int, Sprite>();

    static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);
    static readonly Color Espresso = new Color(0.30f, 0.18f, 0.09f, 1f);
    static readonly Color Cream = new Color(0.96f, 0.91f, 0.78f, 1f);
    static readonly Color SyrupDot = new Color(0.55f, 0.32f, 0.10f, 1f);
    static readonly Color Whip = new Color(1f, 1f, 1f, 1f);
    static readonly Color IceTint = new Color(0.62f, 0.80f, 0.92f, 1f);
    static readonly Color CupOutline = new Color(0.20f, 0.14f, 0.10f, 1f);

    static readonly Color Amber = new Color(0.85f, 0.62f, 0.20f, 1f);
    static readonly Color Pink = new Color(0.98f, 0.78f, 0.83f, 1f);
    static readonly Color Tan = new Color(0.78f, 0.58f, 0.36f, 1f);

    public static Sprite GetMenuIcon(MenuDefinition menu)
    {
        if (menu == null)
            return null;

        if (CafeAssetConfig.Instance != null)
        {
            Sprite injected = CafeAssetConfig.Instance.GetMenuIcon(menu.menuName);
            if (injected != null)
                return injected;
        }

        string key = string.IsNullOrEmpty(menu.menuName) ? "__menu__" : menu.menuName;
        if (_menuCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Sprite sprite = CreateSprite(BuildMenuPixels(menu), key + "_Icon");
        _menuCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetIngredientIcon(IngredientSO ingredient)
    {
        if (ingredient == null)
            return null;

        if (CafeAssetConfig.Instance != null)
        {
            Sprite injected = CafeAssetConfig.Instance.GetIngredientIcon(ingredient.type);
            if (injected != null)
                return injected;
        }

        int key = ingredient.GetInstanceID();
        if (_ingredientCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        string name = string.IsNullOrEmpty(ingredient.ingredientName) ? "Ingredient" : ingredient.ingredientName;
        Sprite sprite = CreateSprite(BuildIngredientPixels(ingredient.type), name + "_Icon");
        _ingredientCache[key] = sprite;
        return sprite;
    }

    static Color[] BuildMenuPixels(MenuDefinition menu)
    {
        Color[] pixels = NewTransparent();

        const int cupBottom = 8;
        const int cupTop = 56;
        const float bottomHalf = 18f;
        const float topHalf = 22f;
        const float centerX = (Size - 1) * 0.5f;
        float cupHeight = cupTop - cupBottom;

        int shots = Mathf.Max(0, menu.requiredShots);
        float milk = Mathf.Clamp01(menu.milkAmount);

        float espressoFrac = shots <= 0 ? 0f : Mathf.Clamp(0.18f + shots * 0.16f, 0.18f, 0.6f);
        float milkFrac = milk * 0.6f;

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

                Color content;
                if (y < espressoTopY)
                    content = Espresso;
                else if (y < milkTopY)
                    content = Cream;
                else
                    continue;

                if (ice)
                    content = Color.Lerp(content, IceTint, 0.28f);

                SetPx(pixels, x, y, content);
            }
        }

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
        border.a = 0.55f;

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
                    float shade = Mathf.Clamp01(0.5f - dy / (radius * 2f));
                    Color c = Color.Lerp(fill, Color.white, shade * 0.18f);
                    c.a = 1f;
                    SetPx(pixels, x, y, c);
                }
            }
        }

        return pixels;
    }

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
