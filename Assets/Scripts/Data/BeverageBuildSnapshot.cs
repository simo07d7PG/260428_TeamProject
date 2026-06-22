using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작 중이거나 완성된 음료의 전체 구성을 담는 스냅샷입니다.
/// 레이어 목록과, 평가·메뉴 매칭에 쓰는 요약 값을 함께 제공합니다.
/// </summary>
public class BeverageBuildSnapshot
{
    readonly List<BeverageLayer> _layers = new();

    public IReadOnlyList<BeverageLayer> Layers => _layers;
    public bool IsComplete { get; set; }

    /// <summary>완성 시 추정/확정된 메뉴 이름. ("라떼" / "알 수 없는 음료")</summary>
    public string EstimatedMenuName { get; set; } = "빈 컵";

    /// <summary>매칭된 메뉴 정의(폴백 포함). null이면 미상.</summary>
    public MenuDefinition MatchedMenu { get; set; }

    /// <summary>0~1. 추정 메뉴와의 일치 신뢰도.</summary>
    public float MatchConfidence { get; set; }

    public BeverageLayer GetLayer(StationType station)
    {
        foreach (BeverageLayer layer in _layers)
        {
            if (layer != null && layer.station == station)
                return layer;
        }

        return null;
    }

    public BeverageLayer GetOrAddLayer(StationType station, IngredientSO ingredient, int level)
    {
        BeverageLayer layer = GetLayer(station);
        if (layer == null)
        {
            layer = BeverageLayer.Create(station, ingredient, level);
            _layers.Add(layer);
        }
        else
        {
            if (ingredient != null)
                layer.ingredient = ingredient;
            layer.level = Mathf.Max(layer.level, level);
        }

        return layer;
    }

    public bool HasStation(StationType station) => GetLayer(station) != null;

    public int ShotCount
    {
        get
        {
            BeverageLayer layer = GetLayer(StationType.EspressoShot);
            return layer != null ? Mathf.Max(layer.count, 0) : 0;
        }
    }

    /// <summary>샷 추출 타이밍 품질 평균(0~1).</summary>
    public float ShotQuality
    {
        get
        {
            BeverageLayer layer = GetLayer(StationType.EspressoShot);
            return layer != null ? Mathf.Clamp01(layer.amount) : 0f;
        }
    }

    /// <summary>밀크 게이지(0~1).</summary>
    public float MilkAmount
    {
        get
        {
            BeverageLayer layer = GetLayer(StationType.SteamMilk);
            return layer != null ? Mathf.Clamp01(layer.amount) : 0f;
        }
    }

    public int SyrupCount
    {
        get
        {
            BeverageLayer layer = GetLayer(StationType.Syrup);
            return layer != null ? layer.count : 0;
        }
    }

    public IngredientSO SyrupIngredient => GetLayer(StationType.Syrup)?.ingredient;

    public int ToppingCount
    {
        get
        {
            BeverageLayer layer = GetLayer(StationType.Topping);
            return layer != null ? layer.count : 0;
        }
    }

    public IngredientSO ToppingIngredient => GetLayer(StationType.Topping)?.ingredient;

    public bool HasIce => HasStation(StationType.Ice);

    public bool HasLid => HasStation(StationType.Lid);

    public bool IsEmpty => _layers.Count == 0;

    public int HighestLevelUsed
    {
        get
        {
            int highest = 1;
            foreach (BeverageLayer layer in _layers)
            {
                if (layer != null && layer.level > highest)
                    highest = layer.level;
            }

            return highest;
        }
    }

    /// <summary>Lv2 이상(머지로 만든 프리미엄) 재료를 사용한 레이어 수.</summary>
    public int PremiumIngredientCount
    {
        get
        {
            int count = 0;
            foreach (BeverageLayer layer in _layers)
            {
                if (layer != null && layer.level >= 2)
                    count++;
            }

            return count;
        }
    }

    public void Clear()
    {
        _layers.Clear();
        IsComplete = false;
        EstimatedMenuName = "빈 컵";
        MatchedMenu = null;
        MatchConfidence = 0f;
    }
}
