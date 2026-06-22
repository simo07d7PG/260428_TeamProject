using System.Collections.Generic;
using UnityEngine;

/// <summary>제작 중이거나 완성된 음료의 전체 구성을 담는 스냅샷입니다.</summary>
public class BeverageBuildSnapshot
{
    readonly List<BeverageLayer> _layers = new();

    public IReadOnlyList<BeverageLayer> Layers => _layers;
    public bool IsComplete { get; set; }

    public string EstimatedMenuName { get; set; } = "빈 컵";

    public MenuDefinition MatchedMenu { get; set; }

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

    public float ShotQuality
    {
        get
        {
            BeverageLayer layer = GetLayer(StationType.EspressoShot);
            return layer != null ? Mathf.Clamp01(layer.amount) : 0f;
        }
    }

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
