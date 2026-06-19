using UnityEngine;

/// <summary>
/// 게임 전체 상태(일차, 코인, 페이즈)를 관리하는 싱글톤 매니저입니다.
/// 씬 전환 시에도 유지되며, 하루 세션의 핵심 루프를 조율합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; set; } = GameState.Preparation;
    public int CurrentDay = 1;
    public int Coin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GameManager>() != null)
            return;

        GameObject managersRoot = new GameObject("[Managers]");
        managersRoot.AddComponent<GameManager>();
        managersRoot.AddComponent<DataManager>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        if (Instance == null)
            return;

        Instance.EnsureManagersForCurrentState();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ManagerUtility.GetOrAddComponent<DataManager>(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 게임 상태를 변경하고 해당 페이즈 Manager를 자동 장착합니다.
    /// </summary>
    public void SetState(GameState newState)
    {
        CurrentState = newState;
        EnsureManagersForCurrentState();
    }

    void EnsureManagersForCurrentState()
    {
        switch (CurrentState)
        {
            case GameState.Preparation:
                PreparationManager.EnsureExists();
                break;
        }
    }
}