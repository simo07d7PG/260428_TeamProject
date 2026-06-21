/// <summary>
/// GPGP식 음료 제작에서 사용하는 작업대(스테이션) 종류입니다.
/// 컵 준비 → 에스프레소 → 스팀 밀크 → 시럽 → 토핑 → (얼음) → 리드 → 서빙 순으로 조작합니다.
/// </summary>
public enum StationType
{
    Cup,
    EspressoShot,
    SteamMilk,
    Syrup,
    Topping,
    Ice,
    Lid,
    Serve
}
