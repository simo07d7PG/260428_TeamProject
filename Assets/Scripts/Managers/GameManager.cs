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
    [Tooltip("켜면 시작 시 저장 파일을 불러옵니다. 메인 메뉴를 쓰면 꺼 두세요.")]
    [SerializeField] bool loadOnStart;
    [Tooltip("새로하기 시작 코인")]
    [SerializeField] int newGameCoin = 500;

    bool _gameStarted;

    public bool HasSave => SaveLoadUtility.HasSave();

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

    void Start()
    {
        // 메뉴 씬에서 전달된 시작 의도를 적용합니다.
        switch (GameBootstrap.PendingMode)
        {
            case GameBootstrap.StartMode.NewGame:
                StartNewGame();
                break;
            case GameBootstrap.StartMode.Continue:
                ContinueGame();
                break;
            default:
                // 직접 MainScene 실행 등 진입 의도가 없을 때, 씬에 잘못 직렬화된 상태(예: Service)로
                // 부팅되어 발주 등 '준비 단계 전용' 기능이 막히지 않도록 항상 준비 단계로 정규화합니다.
                if (CurrentState != GameState.Preparation)
                    SetState(GameState.Preparation);
                break;
        }

        GameBootstrap.PendingMode = GameBootstrap.StartMode.None;
    }

    void OnApplicationQuit()
    {
        // 게임을 시작하지 않고 메뉴에서 종료하면 저장을 덮어쓰지 않습니다.
        if (_gameStarted)
            SaveLoadUtility.Save(this);
    }

    /// <summary>새로하기: 저장 삭제 후 1일차/시작 코인으로 초기화합니다.</summary>
    public void StartNewGame()
    {
        _gameStarted = true;
        SaveLoadUtility.Delete();
        CurrentDay = 1;
        Coin = newGameCoin;
        PreparationManager.Instance?.ResetForNewDay();

        if (CurrentState != GameState.Preparation)
            SetState(GameState.Preparation);
    }

    /// <summary>이어하기: 저장된 코인/일차를 불러와 해당 일차의 준비 단계로 시작합니다.</summary>
    public void ContinueGame()
    {
        _gameStarted = true;
        SaveLoadUtility.LoadInto(this);
        PreparationManager.Instance?.ResetForNewDay();

        if (CurrentState != GameState.Preparation)
            SetState(GameState.Preparation);
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