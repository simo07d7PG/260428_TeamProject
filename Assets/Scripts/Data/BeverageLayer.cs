using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>컵 위에 쌓이는 한 겹의 구성 요소입니다. (에스프레소, 밀크, 시럽 방울, 토핑 등)</summary>
[Serializable]
public class BeverageLayer
{
    public StationType station;
    public IngredientSO ingredient;
    public int level = 1;

    [Range(0f, 1f)] public float amount;

    public int count;

    public List<Vector2> positions = new();

    public static BeverageLayer Create(StationType station, IngredientSO ingredient, int level)
    {
        return new BeverageLayer
        {
            station = station,
            ingredient = ingredient,
            level = Mathf.Max(1, level)
        };
    }

    public void AddPoint(Vector2 normalizedPosition)
    {
        positions ??= new List<Vector2>();
        positions.Add(normalizedPosition);
        count = positions.Count;
    }

    public BeverageLayer Clone()
    {
        return new BeverageLayer
        {
            station = station,
            ingredient = ingredient,
            level = level,
            amount = amount,
            count = count,
            positions = positions != null ? new List<Vector2>(positions) : new List<Vector2>()
        };
    }
}
