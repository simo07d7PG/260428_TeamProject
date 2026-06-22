using UnityEngine;

/// <summary>서빙 평가 결과의 세부 점수입니다.</summary>
public class ServingScoreBreakdown
{
    public const float OrderMatchWeight = 0.40f;
    public const float AmountAccuracyWeight = 0.30f;
    public const float ToppingPlacementWeight = 0.15f;
    public const float PatienceWeight = 0.15f;

    public const float ExactMatchMultiplier = 1.25f;
    public const float PartialMatchMultiplier = 0.7f;

    public float OrderMatch;

    public float AmountAccuracy;

    public float ToppingPlacement;

    public float PatienceRemaining;

    public float TotalScore;

    public float PayoutMultiplier;

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
