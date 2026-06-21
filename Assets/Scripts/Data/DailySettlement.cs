using System.Collections.Generic;

/// <summary>
/// 하루 영업의 정산 결과입니다.
/// </summary>
public class DailySettlement
{
    public int Day;
    public int Revenue;
    public int GarbageCost;
    public int LeftoverCost;
    public int ServedCount;
    public int LeftCount;
    public int MergeCount;
    public int CoinAfter;
    public string Grade = string.Empty;
    public readonly List<string> UpcomingUnlocks = new();
    public string UpcomingTrendName = string.Empty;

    public int DisposalCost => GarbageCost + LeftoverCost;
    public int NetProfit => Revenue - DisposalCost;
}
