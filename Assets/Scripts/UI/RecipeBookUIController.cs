using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>"레시피" 버튼으로 토글하는 레시피 북입니다.</summary>
public class RecipeBookUIController : MonoBehaviour
{
    class RecipePage
    {
        public string title;
        public Sprite icon;
        public List<string> steps;
        public string footer;
    }

    RectTransform _panel;

    Image _icon;
    TextMeshProUGUI _title;
    TextMeshProUGUI _steps;
    TextMeshProUGUI _footer;
    TextMeshProUGUI _indicator;
    Button _prevButton;
    Button _nextButton;

    List<RecipePage> _pages = new List<RecipePage>();
    int _index;
    bool _open;

    public static void ConfigureHostTransform(RectTransform host) => UIFactoryUtility.StretchHost(host);

    void Awake()
    {
        ConfigureHostTransform(transform as RectTransform);

        RectTransform existing = FindDeepChild(transform, "RecipePanel") as RectTransform;
        if (existing != null)
            Bind(existing);
        else
            Build();

        UIFontUtility.ApplyToHierarchy(transform);
        SetOpen(false);
    }

    /// <summary>에디터에서 씬에 레시피 북을 굽기 위한 진입점.</summary>
    public void EditorBuild()
    {
        ConfigureHostTransform(transform as RectTransform);
        if (FindDeepChild(transform, "RecipePanel") == null)
            Build();
        UIFontUtility.ApplyToHierarchy(transform);
    }

    void Build()
    {
        Button toggleButton = UIFactoryUtility.CreateButton(
            transform as RectTransform, "RecipeToggle", "레시피", new Color(0.22f, 0.30f, 0.26f, 0.95f));
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(60f, -64f);
        toggleRect.sizeDelta = new Vector2(76f, 40f);
        TextMeshProUGUI toggleLabel = toggleButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (toggleLabel != null)
            toggleLabel.fontSize = 16f;
        toggleButton.onClick.AddListener(Toggle);

        GameObject dimObject = UIFactoryUtility.CreateUIObject("RecipeDim", transform, typeof(Image), typeof(Button));
        _panel = dimObject.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_panel);
        dimObject.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.78f);
        dimObject.GetComponent<Button>().onClick.AddListener(CloseFromUI);

        GameObject panelObject = UIFactoryUtility.CreateUIObject("RecipePanel", _panel, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 700f);
        panelObject.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 1f);

        BuildPageContent(panelRect);
        BuildNavigation(panelRect);

        Button closeButton = UIFactoryUtility.CreateButton(
            panelRect, "CloseButton", "닫기", new Color(0.25f, 0.45f, 0.75f, 1f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 16f);
        closeRect.sizeDelta = new Vector2(200f, 44f);
        closeButton.onClick.AddListener(CloseFromUI);
    }

    void Bind(RectTransform panelRect)
    {
        _panel = panelRect.parent as RectTransform;

        _icon = panelRect.Find("RecipeIcon")?.GetComponent<Image>();
        _title = panelRect.Find("RecipeTitle")?.GetComponent<TextMeshProUGUI>();
        _steps = panelRect.Find("RecipeSteps")?.GetComponent<TextMeshProUGUI>();
        _footer = panelRect.Find("RecipeFooter")?.GetComponent<TextMeshProUGUI>();
        _indicator = panelRect.Find("PageIndicator")?.GetComponent<TextMeshProUGUI>();
        _prevButton = panelRect.Find("PrevButton")?.GetComponent<Button>();
        _nextButton = panelRect.Find("NextButton")?.GetComponent<Button>();

        Button toggleButton = transform.Find("RecipeToggle")?.GetComponent<Button>();
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(Toggle);
            toggleButton.onClick.AddListener(Toggle);
        }

        if (_prevButton != null)
        {
            _prevButton.onClick.RemoveListener(Prev);
            _prevButton.onClick.AddListener(Prev);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(Next);
            _nextButton.onClick.AddListener(Next);
        }

        Button dimButton = _panel != null ? _panel.GetComponent<Button>() : null;
        if (dimButton != null)
        {
            dimButton.onClick.RemoveListener(CloseFromUI);
            dimButton.onClick.AddListener(CloseFromUI);
        }

        Button closeButton = panelRect.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseFromUI);
            closeButton.onClick.AddListener(CloseFromUI);
        }
    }

    void CloseFromUI() => SetOpen(false);

    static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
                return child;
            Transform found = FindDeepChild(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    void BuildPageContent(RectTransform panelRect)
    {
        _icon = UIFactoryUtility.CreateImage(panelRect, "RecipeIcon", Color.white);
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
        RectTransform iconRect = _icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -40f);
        iconRect.sizeDelta = new Vector2(180f, 180f);

        _title = UIFactoryUtility.CreateLabel(panelRect, "RecipeTitle", string.Empty, 30f);
        _title.alignment = TextAlignmentOptions.Center;
        _title.fontStyle = FontStyles.Bold;
        _title.overflowMode = TextOverflowModes.Overflow;
        RectTransform titleRect = _title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(28f, 0f);
        titleRect.offsetMax = new Vector2(-28f, 0f);
        titleRect.anchoredPosition = new Vector2(0f, -236f);
        titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, 48f);

        _steps = UIFactoryUtility.CreateLabel(panelRect, "RecipeSteps", string.Empty, 20f);
        _steps.alignment = TextAlignmentOptions.TopLeft;
        _steps.enableWordWrapping = true;
        _steps.richText = true;
        _steps.overflowMode = TextOverflowModes.Overflow;
        RectTransform stepsRect = _steps.rectTransform;
        stepsRect.anchorMin = new Vector2(0f, 0f);
        stepsRect.anchorMax = new Vector2(1f, 1f);
        stepsRect.pivot = new Vector2(0.5f, 0.5f);
        stepsRect.offsetMin = new Vector2(40f, 150f);
        stepsRect.offsetMax = new Vector2(-40f, -300f);

        _footer = UIFactoryUtility.CreateLabel(panelRect, "RecipeFooter", string.Empty, 17f);
        _footer.alignment = TextAlignmentOptions.Top;
        _footer.enableWordWrapping = true;
        _footer.richText = true;
        _footer.overflowMode = TextOverflowModes.Overflow;
        RectTransform footerRect = _footer.rectTransform;
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = new Vector2(28f, 110f);
        footerRect.offsetMax = new Vector2(-28f, 0f);
        footerRect.sizeDelta = new Vector2(footerRect.sizeDelta.x, 70f);
    }

    void BuildNavigation(RectTransform panelRect)
    {
        _prevButton = UIFactoryUtility.CreateButton(
            panelRect, "PrevButton", "◀ 이전", new Color(0.30f, 0.34f, 0.42f, 1f));
        RectTransform prevRect = _prevButton.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0f, 0f);
        prevRect.anchorMax = new Vector2(0f, 0f);
        prevRect.pivot = new Vector2(0f, 0f);
        prevRect.anchoredPosition = new Vector2(28f, 74f);
        prevRect.sizeDelta = new Vector2(150f, 44f);
        _prevButton.onClick.AddListener(Prev);

        _nextButton = UIFactoryUtility.CreateButton(
            panelRect, "NextButton", "다음 ▶", new Color(0.30f, 0.34f, 0.42f, 1f));
        RectTransform nextRect = _nextButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(1f, 0f);
        nextRect.anchorMax = new Vector2(1f, 0f);
        nextRect.pivot = new Vector2(1f, 0f);
        nextRect.anchoredPosition = new Vector2(-28f, 74f);
        nextRect.sizeDelta = new Vector2(150f, 44f);
        _nextButton.onClick.AddListener(Next);

        _indicator = UIFactoryUtility.CreateLabel(panelRect, "PageIndicator", string.Empty, 18f);
        _indicator.alignment = TextAlignmentOptions.Center;
        RectTransform indicatorRect = _indicator.rectTransform;
        indicatorRect.anchorMin = new Vector2(0.5f, 0f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0f);
        indicatorRect.pivot = new Vector2(0.5f, 0f);
        indicatorRect.anchoredPosition = new Vector2(0f, 84f);
        indicatorRect.sizeDelta = new Vector2(160f, 30f);
    }

    void Toggle() => SetOpen(!_open);

    void SetOpen(bool open)
    {
        _open = open;
        if (_panel != null)
            _panel.gameObject.SetActive(open);

        if (open)
        {
            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
            _pages = BuildPages(day);
            GoTo(0);
        }
    }

    void Prev() => GoTo(_index - 1);

    void Next() => GoTo(_index + 1);

    void GoTo(int index)
    {
        if (_pages == null || _pages.Count == 0)
        {
            _index = 0;
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }
            if (_title != null)
                _title.text = UIFontUtility.Sanitize("표시할 레시피가 없습니다");
            if (_steps != null)
                _steps.text = string.Empty;
            if (_footer != null)
                _footer.text = string.Empty;
            if (_indicator != null)
                _indicator.text = UIFontUtility.Sanitize("0 / 0");
            if (_prevButton != null)
                _prevButton.interactable = false;
            if (_nextButton != null)
                _nextButton.interactable = false;
            return;
        }

        _index = Mathf.Clamp(index, 0, _pages.Count - 1);
        RecipePage page = _pages[_index];

        if (_icon != null)
        {
            _icon.enabled = page.icon != null;
            _icon.sprite = page.icon;
        }
        if (_title != null)
            _title.text = UIFontUtility.Sanitize(page.title);
        if (_steps != null)
            _steps.text = UIFontUtility.Sanitize(BuildStepsText(page.steps));
        if (_footer != null)
            _footer.text = UIFontUtility.Sanitize(page.footer);
        if (_indicator != null)
            _indicator.text = UIFontUtility.Sanitize($"{_index + 1} / {_pages.Count}");

        if (_prevButton != null)
            _prevButton.interactable = _index > 0;
        if (_nextButton != null)
            _nextButton.interactable = _index < _pages.Count - 1;
    }

    List<RecipePage> BuildPages(int day)
    {
        List<RecipePage> pages = new List<RecipePage>();

        List<MenuDefinition> menus = MenuCatalog.BuildCatalog(day);
        foreach (MenuDefinition def in menus)
        {
            if (def == null)
                continue;

            pages.Add(new RecipePage
            {
                title = def.menuName,
                icon = def.icon,
                steps = BuildSteps(def),
                footer = BuildMenuFooter(def)
            });
        }

        if (DataManager.Instance != null)
        {
            foreach (MergeRecipeSO recipe in DataManager.Instance.GetUnlockedRecipes(day))
            {
                if (recipe == null || recipe.outputIngredient == null)
                    continue;

                pages.Add(BuildMergePage(recipe));
            }
        }

        return pages;
    }

    List<string> BuildSteps(MenuDefinition def)
    {
        List<string> raw = new List<string>();

        raw.Add("컵을 준비한다");

        if (def.requiredShots > 0)
            raw.Add($"에스프레소 {def.requiredShots}샷을 추출한다");

        if (def.milkAmount > 0.05f)
            raw.Add($"스팀밀크를 {(def.milkAmount >= 0.6f ? "가득" : "적당히")} 채운다");

        if (def.syrupCount > 0)
            raw.Add($"시럽을 {def.syrupCount}회 넣는다");

        if (def.toppingCount > 0)
            raw.Add(HasTag(def.specialTags, "휘핑") ? "휘핑 크림을 올린다" : "토핑을 올린다");

        if (def.requiresIce)
            raw.Add("얼음을 넣는다");

        raw.Add("리드를 덮어 서빙한다");

        List<string> numbered = new List<string>(raw.Count);
        for (int i = 0; i < raw.Count; i++)
            numbered.Add($"{i + 1}. {raw[i]}");

        return numbered;
    }

    string BuildMenuFooter(MenuDefinition def)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"<b>가격</b>  {def.basePrice} Coin");

        if (def.orderPhrases != null && def.orderPhrases.Length > 0
            && !string.IsNullOrEmpty(def.orderPhrases[0]))
        {
            sb.AppendLine();
            sb.Append($"<b>주문 예시</b>  \"{def.orderPhrases[0]}\"");
        }

        return sb.ToString();
    }

    RecipePage BuildMergePage(MergeRecipeSO recipe)
    {
        string inputName = recipe.inputIngredients != null && recipe.inputIngredients.Length > 0
            && recipe.inputIngredients[0] != null
            ? recipe.inputIngredients[0].ingredientName
            : "재료";
        string outputName = recipe.outputIngredient.ingredientName;

        List<string> steps = new List<string>
        {
            $"1. 동일한 {inputName} 2개를 준비한다",
            $"2. 두 재료를 합친다 → {outputName} Lv{recipe.outputLevel}"
        };

        string footer = string.IsNullOrEmpty(recipe.effectDescription)
            ? "<b>재료 합성</b>"
            : $"<b>효과</b>  {recipe.effectDescription}";

        return new RecipePage
        {
            title = $"재료 합성: {outputName}",
            icon = ProceduralIconUtility.GetIngredientIcon(recipe.outputIngredient),
            steps = steps,
            footer = footer
        };
    }

    static string BuildStepsText(List<string> steps)
    {
        if (steps == null || steps.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>[ 제조법 ]</b>");
        for (int i = 0; i < steps.Count; i++)
            sb.AppendLine(steps[i]);
        return sb.ToString();
    }

    static bool HasTag(string[] tags, string tag)
    {
        if (tags == null)
            return false;

        foreach (string t in tags)
        {
            if (t == tag)
                return true;
        }
        return false;
    }
}
