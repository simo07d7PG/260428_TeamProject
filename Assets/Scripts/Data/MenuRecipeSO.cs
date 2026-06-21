using System;
using UnityEngine;

/// <summary>
/// 메뉴 한 종류가 요구하는 단일 스테이션 구성 조건입니다.
/// 밀크는 amount(게이지), 시럽/토핑은 count를 사용합니다.
/// </summary>
[Serializable]
public class MenuLayerSpec
{
    public StationType station;

    [Tooltip("밀크 게이지 목표(0~1). EspressoShot은 샷 추출 품질 목표로도 사용합니다.")]
    [Range(0f, 1f)] public float targetAmount = 0f;

    [Tooltip("허용 오차(±). Lv2 재료 사용 시 보너스로 더 넉넉해집니다.")]
    [Range(0f, 1f)] public float amountTolerance = 0.2f;

    [Tooltip("샷 횟수·시럽 방울·토핑 개수 목표.")]
    public int targetCount = 1;
}

/// <summary>
/// GPGP식 음료 메뉴의 완성 조건 데이터입니다. MergeRecipeSO와는 별개입니다.
/// 에셋이 없어도 MenuCatalog의 하드코드 폴백으로 게임이 동작합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewMenuRecipe", menuName = "CafeSim/Menu Recipe")]
public class MenuRecipeSO : ScriptableObject
{
    [Header("기본 정보")]
    public string menuName;
    public Sprite icon;
    public int basePrice = 40;
    public int unlockDay = 1;

    [Header("주문 대사 (암호형, 여러 개면 무작위)")]
    [TextArea] public string[] orderPhrases;

    [Header("필수 구성")]
    public MenuLayerSpec[] requiredLayers;

    [Header("선택 구성 (있으면 가점)")]
    public MenuLayerSpec[] optionalLayers;

    [Header("금지 스테이션 (들어가면 감점/불일치)")]
    public StationType[] forbiddenStations;

    [Header("특수 태그 (예: 아이스, 휘핑)")]
    public string[] specialTags;
}
