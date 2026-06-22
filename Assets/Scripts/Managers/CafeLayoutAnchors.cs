using UnityEngine;

/// <summary>영업 화면 UI의 '배치 마커'입니다. 마커의 위치/크기에 맞춰 실제 게임 UI를 생성합니다.</summary>
[DefaultExecutionOrder(-110)]
public class CafeLayoutAnchors : MonoBehaviour
{
    public static CafeLayoutAnchors Instance { get; private set; }

    [Header("컵 위치 마커")]
    [Tooltip("컵을 집었을 때 놓이는 컵통 위치")] public RectTransform cupStack;
    [Tooltip("컵을 머신에서 내렸을 때(드는 상태) 위치")] public RectTransform cupHeld;
    [Tooltip("컵을 머신에 도킹했을 때 위치")] public RectTransform cupMachine;
    [Tooltip("컵 크기 마커(크기만 사용)")] public RectTransform cupSize;

    [Header("스테이션 마커")]
    public RectTransform machine;
    public RectTransform milkTool;
    public RectTransform iceTool;
    public RectTransform toppingTool;
    public RectTransform syrupTool;

    [Header("재료 변경 셀렉터 마커")]
    public RectTransform baseSelector;
    public RectTransform milkSelector;
    public RectTransform toppingSelector;
    public RectTransform syrupSelector;

    [Header("기타 마커")]
    public RectTransform banner;
    public RectTransform serveZone;
    public RectTransform queue;

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
