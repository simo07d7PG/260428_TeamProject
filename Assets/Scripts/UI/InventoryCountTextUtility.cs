using TMPro;
using UnityEngine;

/// <summary>인벤토리 아이템 개수 텍스트를 생성·배치합니다.</summary>
public static class InventoryCountTextUtility
{
    const string CountChildName = "Count";
    public static TextMeshProUGUI EnsureCountText(Transform itemRoot, TextMeshProUGUI existing)
    {
        TextMeshProUGUI countText = existing;

        if (countText == null)
        {
            Transform countTransform = itemRoot.Find(CountChildName);
            if (countTransform != null)
                countText = countTransform.GetComponent<TextMeshProUGUI>();
        }

        if (countText == null)
        {
            GameObject countObject = new GameObject(CountChildName, typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(itemRoot, false);
            countObject.transform.SetAsLastSibling();
            countText = countObject.GetComponent<TextMeshProUGUI>();
        }

        Configure(countText);
        return countText;
    }

    static void Configure(TextMeshProUGUI countText)
    {
        RectTransform countRect = countText.rectTransform;
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-2f, 2f);
        countRect.sizeDelta = new Vector2(34f, 24f);

        UIFontUtility.Apply(countText);
        countText.fontSize = 16f;
        countText.fontStyle = FontStyles.Bold;
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.color = Color.white;
        countText.outlineWidth = 0.25f;
        countText.outlineColor = Color.black;
        countText.raycastTarget = false;
        countText.enableAutoSizing = false;
        countText.gameObject.SetActive(true);
    }
}