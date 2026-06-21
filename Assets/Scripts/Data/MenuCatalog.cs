using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메뉴 매칭에 사용하는 정규화된 메뉴 정의입니다.
/// 하드코드 폴백과 MenuRecipeSO 에셋을 같은 형태로 다룹니다.
/// </summary>
public class MenuDefinition
{
    public string menuName;
    public Sprite icon;
    public int requiredShots = 1;
    public float milkAmount = 0f;
    public float milkTolerance = 0.2f;
    public int syrupCount = 0;
    public int toppingCount = 0;
    public bool requiresIce = false;
    public int basePrice = 40;
    public int unlockDay = 1;
    public string[] orderPhrases = System.Array.Empty<string>();
    public string[] specialTags = System.Array.Empty<string>();
    public StationType[] forbiddenStations = System.Array.Empty<StationType>();
    public MenuRecipeSO source;

    public string RandomPhrase()
    {
        if (orderPhrases == null || orderPhrases.Length == 0)
            return menuName;

        int index = Random.Range(0, orderPhrases.Length);
        return string.IsNullOrEmpty(orderPhrases[index]) ? menuName : orderPhrases[index];
    }
}

/// <summary>
/// 음료 스냅샷을 메뉴와 대조하는 매칭 엔진이자, 하드코드 폴백 메뉴 카탈로그입니다.
/// MenuRecipeSO 에셋이 있으면 함께 사용하고, 없으면 폴백만으로 동작합니다.
/// </summary>
public static class MenuCatalog
{
    public const float MatchThreshold = 0.55f;
    const float Lv2ToleranceBonus = 0.1f;

    static List<MenuDefinition> _fallback;

    static List<MenuDefinition> Fallback => _fallback ??= BuildFallback();

    static List<MenuDefinition> BuildFallback()
    {
        return new List<MenuDefinition>
        {
            new MenuDefinition
            {
                menuName = "아메리카노", requiredShots = 1, milkAmount = 0f, milkTolerance = 0.12f,
                syrupCount = 0, toppingCount = 0, basePrice = 40, unlockDay = 1,
                forbiddenStations = new[] { StationType.SteamMilk },
                orderPhrases = new[] { "그냥 커피 한 잔이요", "진한 아메리카노", "물 탄 커피로 주세요" }
            },
            new MenuDefinition
            {
                menuName = "카페라떼", requiredShots = 1, milkAmount = 0.72f, milkTolerance = 0.16f,
                syrupCount = 0, toppingCount = 0, basePrice = 55, unlockDay = 1,
                orderPhrases = new[] { "우유 많이 넣어주세요", "부드러운 걸로", "라떼 한 잔" }
            },
            new MenuDefinition
            {
                menuName = "바닐라라떼", requiredShots = 1, milkAmount = 0.66f, milkTolerance = 0.16f,
                syrupCount = 1, toppingCount = 0, basePrice = 60, unlockDay = 1,
                orderPhrases = new[] { "달달한 우유 커피", "바닐라 라떼요", "살짝 달게" }
            },
            new MenuDefinition
            {
                menuName = "카페모카", requiredShots = 1, milkAmount = 0.55f, milkTolerance = 0.18f,
                syrupCount = 2, toppingCount = 0, basePrice = 65, unlockDay = 1,
                orderPhrases = new[] { "달달하게 모카로", "초코 시럽 듬뿍", "아주 달게요" }
            },
            new MenuDefinition
            {
                menuName = "휘핑 카페모카", requiredShots = 1, milkAmount = 0.5f, milkTolerance = 0.2f,
                syrupCount = 2, toppingCount = 1, basePrice = 78, unlockDay = 2,
                specialTags = new[] { "휘핑" },
                orderPhrases = new[] { "머리 위에 구름 얹어줘요", "휘핑 올려서", "크림 잔뜩" }
            }
        };
    }

    /// <summary>현재 일차에 해금된 전체 메뉴(폴백 + 에셋)를 반환합니다.</summary>
    public static List<MenuDefinition> BuildCatalog(int currentDay)
    {
        List<MenuDefinition> result = new List<MenuDefinition>();
        HashSet<string> names = new HashSet<string>();

        if (DataManager.Instance != null)
        {
            foreach (MenuRecipeSO asset in DataManager.Instance.Menus)
            {
                if (asset == null || asset.unlockDay > currentDay)
                    continue;

                MenuDefinition def = FromAsset(asset);
                if (def != null && names.Add(def.menuName))
                    result.Add(def);
            }
        }

        foreach (MenuDefinition def in Fallback)
        {
            if (def.unlockDay <= currentDay && names.Add(def.menuName))
                result.Add(def);
        }

        return result;
    }

    public static MenuDefinition FromAsset(MenuRecipeSO asset)
    {
        if (asset == null)
            return null;

        MenuDefinition def = new MenuDefinition
        {
            menuName = string.IsNullOrEmpty(asset.menuName) ? asset.name : asset.menuName,
            icon = asset.icon,
            basePrice = asset.basePrice,
            unlockDay = asset.unlockDay,
            orderPhrases = asset.orderPhrases ?? System.Array.Empty<string>(),
            specialTags = asset.specialTags ?? System.Array.Empty<string>(),
            forbiddenStations = asset.forbiddenStations ?? System.Array.Empty<StationType>(),
            source = asset
        };

        ApplySpecs(def, asset.requiredLayers);
        return def;
    }

    static void ApplySpecs(MenuDefinition def, MenuLayerSpec[] specs)
    {
        if (specs == null)
            return;

        bool hasMilkSpec = false;
        bool hasShotSpec = false;

        foreach (MenuLayerSpec spec in specs)
        {
            if (spec == null)
                continue;

            switch (spec.station)
            {
                case StationType.EspressoShot:
                    def.requiredShots = Mathf.Max(1, spec.targetCount);
                    hasShotSpec = true;
                    break;
                case StationType.SteamMilk:
                    def.milkAmount = spec.targetAmount;
                    def.milkTolerance = spec.amountTolerance;
                    hasMilkSpec = true;
                    break;
                case StationType.Syrup:
                    def.syrupCount = Mathf.Max(1, spec.targetCount);
                    break;
                case StationType.Topping:
                    def.toppingCount = Mathf.Max(1, spec.targetCount);
                    break;
                case StationType.Ice:
                    def.requiresIce = true;
                    break;
            }
        }

        if (!hasShotSpec)
            def.requiredShots = 1;
        if (!hasMilkSpec)
            def.milkAmount = 0f;
    }

    /// <summary>스냅샷에 가장 가까운 메뉴를 찾습니다. 신뢰도가 임계 미만이면 null.</summary>
    public static MenuDefinition Match(BeverageBuildSnapshot snapshot, int currentDay, out float confidence)
    {
        confidence = 0f;
        if (snapshot == null || snapshot.IsEmpty)
            return null;

        MenuDefinition best = null;
        float bestScore = 0f;

        foreach (MenuDefinition def in BuildCatalog(currentDay))
        {
            float score = ScoreIdentity(def, snapshot);
            if (score > bestScore)
            {
                bestScore = score;
                best = def;
            }
        }

        confidence = bestScore;
        return bestScore >= MatchThreshold ? best : null;
    }

    /// <summary>메뉴 "종류" 일치도(0~1). 샷/밀크/시럽/토핑 구성이 메뉴와 얼마나 닮았는지.</summary>
    public static float ScoreIdentity(MenuDefinition def, BeverageBuildSnapshot snapshot)
    {
        if (def == null || snapshot == null)
            return 0f;

        float shotScore = ScoreShots(def, snapshot);
        float milkScore = ScoreMilk(def, snapshot, false);
        float syrupScore = ScoreCount(def.syrupCount, snapshot.SyrupCount);
        float toppingScore = ScoreCount(def.toppingCount, snapshot.ToppingCount);

        float identity = shotScore * 0.30f + milkScore * 0.35f + syrupScore * 0.20f + toppingScore * 0.15f;
        identity *= ForbiddenPenalty(def, snapshot);
        return Mathf.Clamp01(identity);
    }

    /// <summary>수량 정확도(0~1). Lv2 재료 사용 시 허용 오차에 보너스를 줍니다.</summary>
    public static float ScoreAmounts(MenuDefinition def, BeverageBuildSnapshot snapshot)
    {
        if (def == null || snapshot == null)
            return 0f;

        bool lv2 = snapshot.HighestLevelUsed >= 2;
        float milkScore = ScoreMilk(def, snapshot, lv2);
        float shotQuality = snapshot.ShotCount > 0 ? snapshot.ShotQuality : (def.requiredShots <= 0 ? 1f : 0f);
        float syrupScore = ScoreCount(def.syrupCount, snapshot.SyrupCount);
        float toppingScore = ScoreCount(def.toppingCount, snapshot.ToppingCount);

        return Mathf.Clamp01(milkScore * 0.4f + shotQuality * 0.3f + syrupScore * 0.2f + toppingScore * 0.1f);
    }

    static float ScoreShots(MenuDefinition def, BeverageBuildSnapshot snapshot)
    {
        if (def.requiredShots <= 0)
            return snapshot.ShotCount == 0 ? 1f : 0.35f;

        if (snapshot.ShotCount >= def.requiredShots)
            return 1f;

        return snapshot.ShotCount > 0 ? 0.5f : 0f;
    }

    static float ScoreMilk(MenuDefinition def, BeverageBuildSnapshot snapshot, bool lv2Bonus)
    {
        float tolerance = def.milkTolerance + (lv2Bonus ? Lv2ToleranceBonus : 0f);
        float diff = Mathf.Abs(snapshot.MilkAmount - def.milkAmount);

        if (diff <= tolerance)
            return 1f;

        return Mathf.Clamp01(1f - (diff - tolerance) / 0.5f);
    }

    static float ScoreCount(int target, int actual)
    {
        if (target == actual)
            return 1f;

        int diff = Mathf.Abs(target - actual);
        return Mathf.Clamp01(1f - diff * 0.4f);
    }

    static float ForbiddenPenalty(MenuDefinition def, BeverageBuildSnapshot snapshot)
    {
        if (def.forbiddenStations == null)
            return 1f;

        foreach (StationType station in def.forbiddenStations)
        {
            if (station == StationType.SteamMilk && snapshot.MilkAmount > 0.2f)
                return 0.35f;
            if (snapshot.HasStation(station))
                return 0.35f;
        }

        return 1f;
    }
}
