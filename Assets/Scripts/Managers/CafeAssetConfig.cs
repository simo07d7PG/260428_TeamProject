using UnityEngine;

/// <summary>
/// 스테이션/컵 스프라이트와 효과음을 Unity 인스펙터에서 드래그-드롭으로 지정하는 설정 컴포넌트입니다.
/// 빈 GameObject(예: Managers)에 추가하고 각 칸에 에셋을 끌어다 넣으면 자동 적용됩니다.
/// 지정하지 않은 칸은 Resources / 절차적 폴백을 사용합니다.
/// </summary>
[DefaultExecutionOrder(-110)]
public class CafeAssetConfig : MonoBehaviour
{
    public static CafeAssetConfig Instance { get; private set; }

    [Header("배경 / 컵")]
    [SerializeField] Sprite counter;
    [SerializeField] Sprite cup;

    [Header("스테이션")]
    [SerializeField] Sprite espressoMachine;
    [SerializeField] Sprite milk;
    [SerializeField] Sprite syrup;
    [SerializeField] Sprite topping;
    [SerializeField] Sprite ice;

    [Header("레이아웃")]
    [Tooltip("커피 머신 게이지(다이얼)의 위치. 머신 중심 기준 상대 좌표(px). 값을 바꾸고 다시 Play하면 위치가 반영됩니다.")]
    [SerializeField] Vector2 gaugeOffset = new Vector2(0f, 30f);

    /// <summary>커피 머신 게이지의 머신 기준 상대 위치(px).</summary>
    public Vector2 GaugeOffset => gaugeOffset;

    [Header("효과음")]
    [SerializeField] AudioClip cupTake;
    [SerializeField] AudioClip cupPlace;
    [SerializeField] AudioClip shotExtract;
    [SerializeField] AudioClip milkPour;
    [SerializeField] AudioClip syrupDrop;
    [SerializeField] AudioClip toppingAdd;
    [SerializeField] AudioClip iceAdd;
    [SerializeField] AudioClip lidClose;
    [SerializeField] AudioClip coin;
    [SerializeField] AudioClip serveFail;
    [SerializeField] AudioClip mergeSuccess;
    [SerializeField] AudioClip customerArrive;
    [SerializeField] AudioClip customerLeave;
    [SerializeField] AudioClip drinkComplete;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>스테이션/컵 스프라이트. 미지정이면 null(폴백 사용).</summary>
    public Sprite GetStationSprite(string name)
    {
        return name switch
        {
            "Counter" => counter,
            "Cup" => cup,
            "EspressoShot" => espressoMachine,
            "Milk" => milk,
            "SteamMilk" => milk,
            "Syrup" => syrup,
            "Topping" => topping,
            "Ice" => ice,
            _ => null
        };
    }

    /// <summary>효과음. 미지정이면 null(Resources/절차음 폴백).</summary>
    public AudioClip GetSfx(string key)
    {
        return key switch
        {
            "cup_take" => cupTake,
            "cup_place" => cupPlace,
            "shot_extract" => shotExtract,
            "milk_pour" => milkPour,
            "syrup_drop" => syrupDrop,
            "topping_add" => toppingAdd,
            "ice_add" => iceAdd,
            "lid_close" => lidClose,
            "coin" => coin,
            "serve_fail" => serveFail,
            "merge_success" => mergeSuccess,
            "customer_arrive" => customerArrive,
            "customer_leave" => customerLeave,
            "drink_complete" => drinkComplete,
            _ => null
        };
    }
}
