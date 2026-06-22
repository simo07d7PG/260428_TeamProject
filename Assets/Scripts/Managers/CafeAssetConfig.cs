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

    [Header("음악(BGM) — 미지정 시 Resources/Bgm/MP_Background 폴백")]
    [SerializeField] AudioClip bgm;
    [SerializeField, Range(0f, 1f)] float bgmVolume = 0.45f;

    [Header("손님 캐릭터 — 미지정 시 Resources/Characters 폴백")]
    [SerializeField] Sprite[] customerBodies;
    [SerializeField] Sprite[] customerHeads;
    [SerializeField] Sprite faceHappy;
    [SerializeField] Sprite faceDefault;
    [SerializeField] Sprite faceAngry;

    [Header("배경 — 미지정 시 단색 폴백")]
    [SerializeField] Sprite serviceBackground;
    [SerializeField] Sprite mainMenuBackground;

    [Header("메뉴 / 재료 아이콘 — 미지정 시 SO.icon / 절차적 폴백")]
    [SerializeField] MenuIconEntry[] menuIcons;
    [SerializeField] IngredientIconEntry[] ingredientIcons;

    [Header("음료 레이어 색 — 알파 0이면 기본값 사용")]
    [SerializeField] Color espressoColor;
    [SerializeField] Color milkColor;
    [SerializeField] Color syrupColor;
    [SerializeField] Color toppingColor;
    [SerializeField] Color iceColor;

    [Header("UI 테마색 — 알파 0이면 기본값 사용")]
    [SerializeField] Color panelColor;
    [SerializeField] Color primaryButtonColor;
    [SerializeField] Color successButtonColor;
    [SerializeField] Color dangerButtonColor;
    [SerializeField] Color patienceHighColor;
    [SerializeField] Color patienceMidColor;
    [SerializeField] Color patienceLowColor;

    // --- 접근자 -------------------------------------------------------------
    public AudioClip Bgm => bgm;
    public float BgmVolume => bgmVolume;
    public Sprite ServiceBackground => serviceBackground;
    public Sprite MainMenuBackground => mainMenuBackground;

    public Sprite FaceHappy => faceHappy;
    public Sprite FaceDefault => faceDefault;
    public Sprite FaceAngry => faceAngry;
    public int CustomerVariantCount => Mathf.Max(
        customerBodies != null ? customerBodies.Length : 0,
        customerHeads != null ? customerHeads.Length : 0);

    public Color EspressoColor => espressoColor;
    public Color MilkColor => milkColor;
    public Color SyrupColor => syrupColor;
    public Color ToppingColor => toppingColor;
    public Color IceColor => iceColor;

    public Color PanelColor => panelColor;
    public Color PrimaryButtonColor => primaryButtonColor;
    public Color SuccessButtonColor => successButtonColor;
    public Color DangerButtonColor => dangerButtonColor;
    public Color PatienceHighColor => patienceHighColor;
    public Color PatienceMidColor => patienceMidColor;
    public Color PatienceLowColor => patienceLowColor;

    public Sprite GetCustomerBody(int index) => Pick(customerBodies, index);
    public Sprite GetCustomerHead(int index) => Pick(customerHeads, index);

    static Sprite Pick(Sprite[] arr, int index)
    {
        if (arr == null || arr.Length == 0)
            return null;
        int i = ((index % arr.Length) + arr.Length) % arr.Length;
        return arr[i];
    }

    public Sprite GetMenuIcon(string menuName)
    {
        if (menuIcons == null || string.IsNullOrEmpty(menuName))
            return null;
        foreach (MenuIconEntry e in menuIcons)
            if (e.icon != null && e.menuName == menuName)
                return e.icon;
        return null;
    }

    public Sprite GetIngredientIcon(IngredientType type)
    {
        if (ingredientIcons == null)
            return null;
        foreach (IngredientIconEntry e in ingredientIcons)
            if (e.icon != null && e.type == type)
                return e.icon;
        return null;
    }

    [System.Serializable]
    public struct MenuIconEntry
    {
        public string menuName;
        public Sprite icon;
    }

    [System.Serializable]
    public struct IngredientIconEntry
    {
        public IngredientType type;
        public Sprite icon;
    }

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
