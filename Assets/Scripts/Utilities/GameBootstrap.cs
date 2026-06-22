/// <summary>메뉴 씬에서 게임 씬으로 시작 방식을 전달하는 정적 상태입니다.</summary>
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
