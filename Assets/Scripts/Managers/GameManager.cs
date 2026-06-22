using System;
using UnityEngine;

/// <summary>게임 전체 상태(일차, 코인, 페이즈)를 관리하는 싱글톤 매니저입니다.</summary>
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
        switch (GameBootstrap.PendingMode)
        {
            case GameBootstrap.StartMode.NewGame:
                StartNewGame();
                break;
            case GameBootstrap.StartMode.Continue:
                ContinueGame();
                break;
            default:
                // 진입 의도 없이 부팅 시 잘못 직렬화된 상태로 시작하지 않도록 준비 단계로 정규화.
                if (CurrentState != GameState.Preparation)
                    SetState(GameState.Preparation);
                break;
        }

        GameBootstrap.PendingMode = GameBootstrap.StartMode.None;
    }

    void OnApplicationQuit()
    {
        if (_gameStarted)
            SaveLoadUtility.Save(this);
    }

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

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
    }

    public void StartService()
    {
        if (CurrentState == GameState.Preparation)
            SetState(GameState.Service);
    }

    public void EndService()
    {
        if (CurrentState == GameState.Service)
            SetState(GameState.Closing);
    }
}