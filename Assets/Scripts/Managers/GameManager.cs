using UnityEngine;

/// <summary>
/// 게임 전체 상태(일차, 코인, 페이즈)를 관리하는 싱글톤 매니저입니다.
/// 씬의 Managers 오브젝트에 배치하며, DontDestroyOnLoad로 유지됩니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; set; } = GameState.Preparation;
    public int CurrentDay = 1;
    public int Coin;

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
        CurrentState = newState;
    }
}