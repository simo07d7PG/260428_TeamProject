using UnityEngine;

/// <summary>손님 주문과 완성된 음료를 대조해 점수를 산출합니다.</summary>
public static class ServingEvaluator
{
    // 주문과 '다른 음료'면 완성도가 높아도 지급하지 않도록 임계를 높게 둡니다(종류 일치가 1차 관문).
    public const float CorrectMenuThreshold = 0.72f;
    public const float ExactScoreThreshold = 0.82f;

    public static ServingScoreBreakdown Evaluate(CustomerOrder order, BeverageBuildSnapshot snapshot, float patienceRatio)
    {
        if (order == null || order.menu == null || snapshot == null)
            return ServingScoreBreakdown.Mismatch("평가할 주문이 없습니다.");

        float orderMatch = MenuCatalog.ScoreIdentity(order.menu, snapshot);
        bool correct = orderMatch >= CorrectMenuThreshold;

        ServingScoreBreakdown breakdown = new ServingScoreBreakdown
        {
            OrderMatch = orderMatch,
            AmountAccuracy = MenuCatalog.ScoreAmounts(order.menu, snapshot),
            ToppingPlacement = EvaluateTopping(order.menu, snapshot),
            PatienceRemaining = Mathf.Clamp01(patienceRatio),
            IsCorrectMenu = correct
        };
        breakdown.Recalculate();

        if (!correct)
        {
            breakdown.PayoutMultiplier = 0f;
            breakdown.Summary = $"주문과 다른 음료입니다 ({snapshot.EstimatedMenuName})";
            return breakdown;
        }

        if (breakdown.TotalScore >= ExactScoreThreshold)
        {
            breakdown.PayoutMultiplier = ServingScoreBreakdown.ExactMatchMultiplier;
            breakdown.Summary = $"완벽한 {order.MenuName}!";
        }
        else
        {
            breakdown.PayoutMultiplier = ServingScoreBreakdown.PartialMatchMultiplier;
            breakdown.Summary = $"{order.MenuName} (아쉬운 완성도)";
        }

        return breakdown;
    }

    static float EvaluateTopping(MenuDefinition menu, BeverageBuildSnapshot snapshot)
    {
        if (menu.toppingCount <= 0)
            return snapshot.ToppingCount == 0 ? 1f : 0.6f;

        if (snapshot.ToppingCount == 0)
            return 0f;

        float countScore = Mathf.Clamp01(1f - Mathf.Abs(menu.toppingCount - snapshot.ToppingCount) * 0.4f);
        float spreadScore = SpreadScore(snapshot.GetLayer(StationType.Topping));
        return Mathf.Clamp01(countScore * 0.7f + spreadScore * 0.3f);
    }

    static float SpreadScore(BeverageLayer layer)
    {
        if (layer == null || layer.positions == null || layer.positions.Count < 2)
            return 0.7f;

        Vector2 centroid = Vector2.zero;
        foreach (Vector2 point in layer.positions)
            centroid += point;
        centroid /= layer.positions.Count;

        float avgDistance = 0f;
        foreach (Vector2 point in layer.positions)
            avgDistance += Vector2.Distance(point, centroid);
        avgDistance /= layer.positions.Count;

        return Mathf.Clamp01(avgDistance / 0.25f);
    }
}
