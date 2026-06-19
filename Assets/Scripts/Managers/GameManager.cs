using UnityEngine;

/// <summary>
/// 게임 전체 상태(일차, 코인, 페이즈)를 관리하는 싱글톤 매니저입니다.
/// 씬 전환 시에도 유지되며, 하루 세션의 핵심 루프를 조율합니다.
/// </summary>
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

        var managerObject = new GameObject(nameof(GameManager));
        managerObject.AddComponent<GameManager>();
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
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 게임 상태를 변경합니다. 4분 세션 내 페이즈 전환에 사용됩니다.
    /// </summary>
    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }
}