using UnityEngine;

/// <summary>
/// 서빙 평가 결과의 세부 점수입니다.
/// 총점 = 주문 일치 40% + 양 정확도 30% + 토핑 배치 15% + 인내심 잔여 15%
/// </summary>
public class ServingScoreBreakdown
{
    public const float OrderMatchWeight = 0.40f;
    public const float AmountAccuracyWeight = 0.30f;
    public const float ToppingPlacementWeight = 0.15f;
    public const float PatienceWeight = 0.15f;

    public const float ExactMatchMultiplier = 1.25f;
    public const float PartialMatchMultiplier = 0.7f;

    /// <summary>주문한 메뉴와 만든 음료의 종류 일치도(0~1).</summary>
    public float OrderMatch;

    /// <summary>밀크 양·샷·시럽·토핑 수량 정확도(0~1).</summary>
    public float AmountAccuracy;

    /// <summary>토핑 배치 균형(0~1).</summary>
    public float ToppingPlacement;

    /// <summary>전달 시점 인내심 잔여 비율(0~1).</summary>
    public float PatienceRemaining;

    /// <summary>가중 합산 총점(0~1).</summary>
    public float TotalScore;

    /// <summary>완전 일치 x1.25 / 부분 x0.7 / 불일치 0(환불).</summary>
    public float PayoutMultiplier;

    /// <summary>주문 메뉴 종류가 맞는지.</summary>
    public bool IsCorrectMenu;

    public string Summary = string.Empty;

    public static ServingScoreBreakdown Mismatch(string summary)
    {
        return new ServingScoreBreakdown
        {
            OrderMatch = 0f,
            TotalScore = 0f,
            PayoutMultiplier = 0f,
            IsCorrectMenu = false,
            Summary = summary
        };
    }

    public void Recalculate()
    {
        TotalScore = Mathf.Clamp01(
            OrderMatch * OrderMatchWeight +
            AmountAccuracy * AmountAccuracyWeight +
            ToppingPlacement * ToppingPlacementWeight +
            PatienceRemaining * PatienceWeight);
    }
}
