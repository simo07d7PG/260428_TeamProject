using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "레시피" 버튼으로 토글하는 레시피 북입니다.
/// 현재 일차에 해금된 메뉴(구성·가격)와 머지 레시피(입력→출력)를 동적으로 보여줍니다.
/// </summary>
public class RecipeBookUIController : MonoBehaviour
{
    RectTransform _panel;
    TextMeshProUGUI _body;
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
        dimObject.GetComponent<Button>().onClick.AddListener(() => SetOpen(false));

        GameObject panelObject = UIFactoryUtility.CreateUIObject("RecipePanel", _panel, typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 700f);
        panelObject.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 1f);

        _body = UIFactoryUtility.CreateLabel(panelRect, "RecipeBody", string.Empty, 18f);
        _body.alignment = TextAlignmentOptions.TopLeft;
        _body.enableWordWrapping = true;
        _body.richText = true;
        RectTransform bodyRect = _body.rectTransform;
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

        if (open && _body != null)
            _body.text = UIFontUtility.Sanitize(BuildContent());
    }

    string BuildContent()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<b>[ 아는 레시피 ]</b>");
        sb.AppendLine();

        List<MenuDefinition> menus = MenuCatalog.BuildCatalog(day);
        sb.AppendLine($"<b>▶ 메뉴 ({menus.Count}종)</b>");
        foreach (MenuDefinition menu in menus)
        {
            if (menu != null)
                sb.AppendLine($"- {menu.menuName}: {Composition(menu)}  ({menu.basePrice} Coin)");
        }
        sb.AppendLine();

        sb.AppendLine("<b>▶ 머지 레시피 (해금)</b>");
        bool any = false;
        if (DataManager.Instance != null)
        {
            foreach (MergeRecipeSO recipe in DataManager.Instance.GetUnlockedRecipes(day))
            {
                if (recipe == null || recipe.outputIngredient == null)
                    continue;

                string inputName = recipe.inputIngredients != null && recipe.inputIngredients.Length > 0
                    && recipe.inputIngredients[0] != null
                    ? recipe.inputIngredients[0].ingredientName
                    : "재료";
                sb.AppendLine($"- {inputName} x2 > {recipe.outputIngredient.ingredientName} (Lv{recipe.outputLevel})");
                any = true;
            }
        }
        if (!any)
            sb.AppendLine("- (아직 해금된 머지 레시피가 없습니다)");

        return sb.ToString();
    }

    static string Composition(MenuDefinition menu)
    {
        List<string> parts = new List<string>();
        if (menu.requiredShots > 0)
            parts.Add($"샷{menu.requiredShots}");
        if (menu.milkAmount > 0.05f)
            parts.Add(menu.milkAmount >= 0.6f ? "밀크많이" : "밀크");
        if (menu.syrupCount > 0)
            parts.Add($"시럽{menu.syrupCount}");
        if (menu.toppingCount > 0)
            parts.Add("토핑");
        if (menu.requiresIce)
            parts.Add("아이스");

        return parts.Count > 0 ? string.Join(" / ", parts) : "-";
    }
}
