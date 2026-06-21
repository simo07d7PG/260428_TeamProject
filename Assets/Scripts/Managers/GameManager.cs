using System;
using UnityEngine;

/// <summary>
/// 게임 전체 상태(일차, 코인, 페이즈)를 관리하는 싱글톤 매니저입니다.
/// 씬의 Managers 오브젝트에 배치하며, DontDestroyOnLoad로 유지됩니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 진행")]
    public GameState CurrentState = GameState.Preparation;
    public int CurrentDay = 1;
    public int Coin;

    [Header("저장")]
    [Tooltip("켜면 시작 시 저장 파일을 불러옵니다. (개발 중에는 꺼 두면 Inspector 값 유지)")]
    [SerializeField] bool loadOnStart;

    public event Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ValidateComponents();

        if (loadOnStart)
            SaveLoadUtility.LoadInto(this);
    }

    void OnApplicationQuit()
    {
        SaveLoadUtility.Save(this);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void ValidateComponents()
    {
        if (!TryGetComponent(out DataManager _))
            Debug.LogError("[GameManager] 같은 오브젝트에 DataManager 컴포넌트를 추가해 주세요.");
    }

    /// <summary>
    /// 게임 상태를 변경합니다. 4분 세션 내 페이즈 전환에 사용됩니다.
    /// </summary>
    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
    }

    /// <summary>준비 → 영업으로 전환합니다.</summary>
    public void StartService()
    {
        if (CurrentState == GameState.Preparation)
            SetState(GameState.Service);
    }

    /// <summary>영업 → 정산으로 전환합니다.</summary>
    public void EndService()
    {
        if (CurrentState == GameState.Service)
            SetState(GameState.Closing);
    }
}