using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>각 스테이션 아래의 '재료 변경' 칸입니다.</summary>
public class MaterialSelectorUI : MonoBehaviour
{
    IngredientType _type;
    TextMeshProUGUI _label;
    readonly List<IngredientSO> _options = new();
    int _index;

    public void Bind(IngredientType type, TextMeshProUGUI label, Button left, Button right)
    {
        _type = type;
        _label = label;

        if (left != null)
            left.onClick.AddListener(() => Cycle(-1));
        if (right != null)
            right.onClick.AddListener(() => Cycle(1));
    }

    void Start()
    {
        BuildOptions();
        ApplySelection();
    }

    void BuildOptions()
    {
        _options.Clear();
        if (DataManager.Instance != null)
        {
            foreach (IngredientSO ingredient in DataManager.Instance.GetIngredientsByType(_type))
            {
                if (ingredient != null)
                    _options.Add(ingredient);
            }
        }

        IngredientSO selected = BeverageBuildManager.Instance != null
            ? BeverageBuildManager.Instance.GetSelectedIngredient(_type)
            : null;
        _index = selected != null ? _options.IndexOf(selected) : 0;
        if (_index < 0)
            _index = 0;
    }

    void Cycle(int dir)
    {
        if (_options.Count == 0)
            return;

        _index = (_index + dir + _options.Count) % _options.Count;
        ApplySelection();
    }

    void ApplySelection()
    {
        if (_options.Count == 0)
        {
            if (_label != null)
                _label.text = UIFontUtility.Sanitize("없음");
            return;
        }

        IngredientSO ingredient = _options[Mathf.Clamp(_index, 0, _options.Count - 1)];
        BeverageBuildManager.Instance?.SetSelectedIngredient(_type, ingredient);
        Refresh();
    }

    /// <summary>보유 수량 표시를 갱신합니다. (컨트롤러가 매 갱신 시 호출)</summary>
    public void Refresh()
    {
        if (_label == null || _options.Count == 0)
            return;

        IngredientSO ingredient = _options[Mathf.Clamp(_index, 0, _options.Count - 1)];
        int count = BeverageBuildManager.Instance != null ? BeverageBuildManager.Instance.CountAvailable(ingredient) : 0;
        _label.text = UIFontUtility.Sanitize($"{ingredient.ingredientName}\n{count}개 남음");
    }
}
