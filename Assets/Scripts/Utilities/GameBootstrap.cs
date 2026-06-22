/// <summary>
/// 메뉴 씬 → 게임 씬으로 "어떻게 시작할지"를 전달하는 정적 상태입니다.
/// 씬 로드 후 GameManager가 읽어 적용한 뒤 None으로 되돌립니다.
/// </summary>
public static class GameBootstrap
{
    public enum StartMode
    {
        None,
        NewGame,
        Continue
    }

    public static StartMode PendingMode = StartMode.None;
}
