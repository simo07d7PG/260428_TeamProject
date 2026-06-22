using UnityEngine;

/// <summary>스크립트로 생성되는 영업 화면 UI의 위치·크기를 유니티 인스펙터에서 조정하는 설정 컴포넌트입니다.</summary>
[DefaultExecutionOrder(-110)]
public class CafeLayoutConfig : MonoBehaviour
{
    public static CafeLayoutConfig Instance { get; private set; }

    static readonly Vector2 D_CupStackHome = new Vector2(-600f, 150f);
    static readonly Vector2 D_CupHeldHome = new Vector2(0f, -210f);
    static readonly Vector2 D_CupMachinePos = new Vector2(-400f, -74f);
    static readonly Vector2 D_CupSize = new Vector2(150f, 205f);
    static readonly Vector2 D_MachinePos = new Vector2(-400f, 88f);
    static readonly Vector2 D_MachineSize = new Vector2(200f, 210f);
    static readonly Vector2 D_MilkToolPos = new Vector2(-180f, 66f);
    static readonly Vector2 D_IceToolPos = new Vector2(40f, 66f);
    static readonly Vector2 D_ToppingToolPos = new Vector2(260f, 66f);
    static readonly Vector2 D_SyrupToolPos = new Vector2(480f, 66f);
    static readonly Vector2 D_ToolSize = new Vector2(116f, 130f);
    static readonly Vector2 D_BaseSelectorPos = new Vector2(-600f, -48f);
    static readonly Vector2 D_MilkSelectorPos = new Vector2(-180f, -48f);
    static readonly Vector2 D_ToppingSelectorPos = new Vector2(260f, -48f);
    static readonly Vector2 D_SyrupSelectorPos = new Vector2(480f, -48f);
    static readonly Vector2 D_SelectorSize = new Vector2(190f, 64f);
    static readonly Vector2 D_BannerPos = new Vector2(0f, -212f);
    static readonly Vector2 D_BannerSize = new Vector2(700f, 118f);
    static readonly Vector2 D_ServeZonePos = new Vector2(-36f, -40f);
    static readonly Vector2 D_ServeZoneSize = new Vector2(180f, 320f);

    [Header("컵")]
    public Vector2 cupStackHome = D_CupStackHome;
    public Vector2 cupHeldHome = D_CupHeldHome;
    public Vector2 cupMachinePos = D_CupMachinePos;
    public Vector2 cupSize = D_CupSize;

    [Header("커피 머신")]
    public Vector2 machinePos = D_MachinePos;
    public Vector2 machineSize = D_MachineSize;

    [Header("재료 도구")]
    public Vector2 milkToolPos = D_MilkToolPos;
    public Vector2 iceToolPos = D_IceToolPos;
    public Vector2 toppingToolPos = D_ToppingToolPos;
    public Vector2 syrupToolPos = D_SyrupToolPos;
    public Vector2 toolSize = D_ToolSize;

    [Header("재료 변경 셀렉터")]
    public Vector2 baseSelectorPos = D_BaseSelectorPos;
    public Vector2 milkSelectorPos = D_MilkSelectorPos;
    public Vector2 toppingSelectorPos = D_ToppingSelectorPos;
    public Vector2 syrupSelectorPos = D_SyrupSelectorPos;
    public Vector2 selectorSize = D_SelectorSize;

    [Header("주문 배너 / 서빙존")]
    public Vector2 bannerPos = D_BannerPos;
    public Vector2 bannerSize = D_BannerSize;
    public Vector2 serveZonePos = D_ServeZonePos;
    public Vector2 serveZoneSize = D_ServeZoneSize;

    [Header("손님 대기열 (상단 가장자리에서 내려오는 거리)")]
    public float queueTopOffset = 64f;

    public static Vector2 CupStackHome => Instance != null ? Instance.cupStackHome : D_CupStackHome;
    public static Vector2 CupHeldHome => Instance != null ? Instance.cupHeldHome : D_CupHeldHome;
    public static Vector2 CupMachinePos => Instance != null ? Instance.cupMachinePos : D_CupMachinePos;
    public static Vector2 CupSize => Instance != null ? Instance.cupSize : D_CupSize;
    public static Vector2 MachinePos => Instance != null ? Instance.machinePos : D_MachinePos;
    public static Vector2 MachineSize => Instance != null ? Instance.machineSize : D_MachineSize;
    public static Vector2 MilkToolPos => Instance != null ? Instance.milkToolPos : D_MilkToolPos;
    public static Vector2 IceToolPos => Instance != null ? Instance.iceToolPos : D_IceToolPos;
    public static Vector2 ToppingToolPos => Instance != null ? Instance.toppingToolPos : D_ToppingToolPos;
    public static Vector2 SyrupToolPos => Instance != null ? Instance.syrupToolPos : D_SyrupToolPos;
    public static Vector2 ToolSize => Instance != null ? Instance.toolSize : D_ToolSize;
    public static Vector2 BaseSelectorPos => Instance != null ? Instance.baseSelectorPos : D_BaseSelectorPos;
    public static Vector2 MilkSelectorPos => Instance != null ? Instance.milkSelectorPos : D_MilkSelectorPos;
    public static Vector2 ToppingSelectorPos => Instance != null ? Instance.toppingSelectorPos : D_ToppingSelectorPos;
    public static Vector2 SyrupSelectorPos => Instance != null ? Instance.syrupSelectorPos : D_SyrupSelectorPos;
    public static Vector2 SelectorSize => Instance != null ? Instance.selectorSize : D_SelectorSize;
    public static Vector2 BannerPos => Instance != null ? Instance.bannerPos : D_BannerPos;
    public static Vector2 BannerSize => Instance != null ? Instance.bannerSize : D_BannerSize;
    public static Vector2 ServeZonePos => Instance != null ? Instance.serveZonePos : D_ServeZonePos;
    public static Vector2 ServeZoneSize => Instance != null ? Instance.serveZoneSize : D_ServeZoneSize;
    public static float QueueTopOffset => Instance != null ? Instance.queueTopOffset : 64f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
