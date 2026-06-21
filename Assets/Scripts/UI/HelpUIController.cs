using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "?" 버튼으로 토글하는 도움말 오버레이입니다. 조작법과 주문 암호 범례를 안내합니다.
/// 게임 상태와 무관하게 항상 접근 가능하며 기본은 숨김입니다.
/// </summary>
public class HelpUIController : MonoBehaviour
{
    const string HelpText =
        "<b>[ 카페 경영 도움말 ]</b>\n\n" +
        "<b>▶ 준비 단계</b>\n" +
        "- 같은 재료 2개를 겹쳐 고급 재료로 머지\n" +
        "- 오른쪽 발주 패널에서 재료 구매 (하루 1회)\n" +
        "- 상단 '영업 시작'으로 영업 진입\n\n" +
        "<b>▶ 영업 단계 (직접 제작)</b>\n" +
        "- 손님 카드를 눌러 주문 받기\n" +
        "- 에스프레소: 버튼을 길게 눌러 추출 (1~1.8초 최적)\n" +
        "- 밀크: 피처를 누른 채 부어 게이지 조절\n" +
        "- 시럽: 병을 탭 (최대 3회)\n" +
        "- 토핑: '토핑' 켠 뒤 컵을 탭\n" +
        "- '완성' 후 컵을 손님 카드로 드래그해 서빙\n\n" +
        "<b>▶ 주문 암호 예시</b>\n" +
        "- \"그냥 커피\" > 아메리카노\n" +
        "- \"우유 많이\" > 라떼\n" +
        "- \"달달하게 / 초코\" > 시럽 듬뿍 (모카)\n" +
        "- \"머리 위에 구름\" > 휘핑 토핑\n" +
        "- \"흑당 듬뿍\" > 시럽 3회\n" +
        "- \"크림 올린 커피\" > 아인슈페너 (우유 없이 휘핑)\n\n" +
        "<b>▶ 팁</b>\n" +
        "- 인내심 바가 빨개지기 전에 서빙 (잔여량이 수령액 ±35%)\n" +
        "- 고급(Lv2) 재료는 조작 허용 오차 보너스\n" +
        "- 남은 재료/쓰레기는 폐기 비용 발생";

    RectTransform _panel;
    Button _toggleButton;
    bool _open;

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);
        BuildUI();
        UIFontUtility.ApplyToHierarchy(transform);
        SetOpen(false);
    }

    void BuildUI()
    {
        // "?" 토글 버튼 — 좌측 상단
        _toggleButton = UIFactoryUtility.CreateButton(
            transform as RectTransform, "HelpToggle", "?", new Color(0.22f, 0.26f, 0.32f, 0.95f));
        RectTransform toggleRect = _toggleButton.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(12f, -64f);
        toggleRect.sizeDelta = new Vector2(40f, 40f);
        TextMeshProUGUI toggleLabel = _toggleButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (toggleLabel != null)
        {
            toggleLabel.fontSize = 24f;
            toggleLabel.fontStyle = FontStyles.Bold;
        }
        _toggleButton.onClick.AddListener(Toggle);

        // 도움말 패널 (어두운 배경 + 중앙 패널)
        GameObject dimObject = UIFactoryUtility.CreateUIObject("HelpDim", transform, typeof(Image), typeof(Button));
        _panel = dimObject.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_panel);
        dimObject.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.78f);
        // 배경 클릭 시 닫기
        dimObject.GetComponent<Button>().onClick.AddListener(() => SetOpen(false));

        GameObject panelObject = UIFactoryUtility.CreateUIObject("HelpPanel", _panel, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(620f, 680f);
        panelObject.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 1f);

        TextMeshProUGUI body = UIFactoryUtility.CreateLabel(panelRect, "HelpBody", HelpText, 18f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.enableWordWrapping = true;
        body.richText = true;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(28f, 70f);
        bodyRect.offsetMax = new Vector2(-28f, -24f);

        Button closeButton = UIFactoryUtility.CreateButton(
            panelRect, "CloseButton", "닫기", new Color(0.25f, 0.45f, 0.75f, 1f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 16f);
        closeRect.sizeDelta = new Vector2(200f, 44f);
        closeButton.onClick.AddListener(() => SetOpen(false));
    }

    void Toggle() => SetOpen(!_open);

    void SetOpen(bool open)
    {
        _open = open;
        if (_panel != null)
            _panel.gameObject.SetActive(open);
    }
}
