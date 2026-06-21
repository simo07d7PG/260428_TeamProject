using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 컵 위에 쌓이는 한 겹의 구성 요소입니다. (에스프레소, 밀크, 시럽 방울, 토핑 등)
/// 컵 캔버스 렌더링과 서빙 평가에 함께 사용됩니다.
/// </summary>
[Serializable]
public class BeverageLayer
{
    public StationType station;
    public IngredientSO ingredient;
    public int level = 1;

    /// <summary>밀크 게이지·샷 추출 품질 등 연속 값(0~1). 시럽/토핑은 사용하지 않습니다.</summary>
    [Range(0f, 1f)] public float amount;

    /// <summary>샷 횟수, 시럽 방울 수, 토핑 개수 등 이산 값.</summary>
    public int count;

    /// <summary>시럽 방울·토핑이 찍힌 컵 위 정규화 좌표(0~1 기준). 렌더링용.</summary>
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
