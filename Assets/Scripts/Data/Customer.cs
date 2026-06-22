using UnityEngine;

/// <summary>한 명의 손님과 그의 주문, 인내심 상태를 담는 런타임 데이터입니다.</summary>
public class Customer
{
    public int Id;
    public CustomerOrder Order;
    public float MaxPatience = 50f;
    public float CurrentPatience = 50f;
    public CustomerState State = CustomerState.Waiting;

    public int CharacterIndex;

    public float PatienceRatio => MaxPatience > 0f ? Mathf.Clamp01(CurrentPatience / MaxPatience) : 0f;

    public bool IsActive => State == CustomerState.Waiting || State == CustomerState.Building;

    public CustomerMood Mood
    {
        get
        {
            if (State == CustomerState.Served)
                return CustomerMood.Happy;
            if (State == CustomerState.Left)
                return CustomerMood.Angry;
            if (PatienceRatio <= 0.25f)
                return CustomerMood.Angry;
            if (PatienceRatio >= 0.7f)
                return CustomerMood.Happy;
            return CustomerMood.Neutral;
        }
    }

    public Customer(int id, CustomerOrder order, float maxPatience)
    {
        Id = id;
        Order = order;
        MaxPatience = Mathf.Max(1f, maxPatience);
        CurrentPatience = MaxPatience;
        State = CustomerState.Waiting;
    }

    public bool Tick(float deltaSeconds)
    {
        if (!IsActive)
            return false;

        CurrentPatience = Mathf.Max(0f, CurrentPatience - deltaSeconds);
        return CurrentPatience <= 0f;
    }
}

/// <summary>손님 표정 상태.</summary>
public enum CustomerMood
{
    Happy,
    Neutral,
    Angry
}
