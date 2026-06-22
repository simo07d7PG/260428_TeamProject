using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>발주 패널을 화면 오른쪽 끝에서 슬라이드로 열고 닫습니다.</summary>
public class SupplyPanelDrawer : MonoBehaviour
{
    const string ExpandedLabel = ">";
    const string CollapsedLabel = "<";

    [SerializeField] RectTransform panelBody;
    [SerializeField] RectTransform toggleRect;
    [SerializeField] Button toggleButton;
    [SerializeField] TextMeshProUGUI toggleLabel;
    [SerializeField] float panelWidth = 300f;
    [SerializeField] float slideDuration = 0.18f;

    bool _isExpanded = true;
    Coroutine _slideRoutine;
    float _expandedToggleX;
    float _collapsedToggleInset;

    public bool IsExpanded => _isExpanded;

    public void Initialize(
        RectTransform body,
        Button toggle,
        TextMeshProUGUI label,
        float width,
        float expandedToggleX,
        float collapsedToggleInset = 0f,
        bool startExpanded = true)
    {
        panelBody = body;
        toggleButton = toggle;
        toggleRect = toggle != null ? toggle.GetComponent<RectTransform>() : null;
        toggleLabel = label;
        panelWidth = width;
        _expandedToggleX = expandedToggleX;
        _collapsedToggleInset = collapsedToggleInset;

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(Toggle);
        }

        SetExpanded(startExpanded, false);
    }

    public void Toggle()
    {
        SetExpanded(!_isExpanded, true);
    }

    public void SetExpanded(bool expanded, bool animate)
    {
        _isExpanded = expanded;
        UpdateToggleLabel();

        if (panelBody == null)
            return;

        float panelTargetX = expanded ? -_collapsedToggleInset : panelWidth;
        float toggleTargetX = expanded ? _expandedToggleX : -_collapsedToggleInset;

        if (expanded && panelBody != null)
            panelBody.gameObject.SetActive(true);

        if (_slideRoutine != null)
            StopCoroutine(_slideRoutine);

        if (!animate || slideDuration <= 0f)
        {
            ApplyPositions(panelTargetX, toggleTargetX);
            return;
        }

        _slideRoutine = StartCoroutine(SlidePanel(panelTargetX, toggleTargetX));
    }

    void ApplyPositions(float panelTargetX, float toggleTargetX)
    {
        if (panelBody != null)
        {
            Vector2 panelPosition = panelBody.anchoredPosition;
            panelPosition.x = panelTargetX;
            panelBody.anchoredPosition = panelPosition;
            panelBody.gameObject.SetActive(panelTargetX <= -_collapsedToggleInset + 0.01f);
        }

        if (toggleRect != null)
        {
            Vector2 togglePosition = toggleRect.anchoredPosition;
            togglePosition.x = toggleTargetX;
            toggleRect.anchoredPosition = togglePosition;
            toggleRect.gameObject.SetActive(true);
        }
    }

    IEnumerator SlidePanel(float panelTargetX, float toggleTargetX)
    {
        Vector2 panelStart = panelBody != null ? panelBody.anchoredPosition : Vector2.zero;
        Vector2 panelEnd = new Vector2(panelTargetX, panelStart.y);
        Vector2 toggleStart = toggleRect != null ? toggleRect.anchoredPosition : Vector2.zero;
        Vector2 toggleEnd = new Vector2(toggleTargetX, toggleStart.y);
        float elapsed = 0f;

        if (panelTargetX <= -_collapsedToggleInset + 0.01f && panelBody != null)
            panelBody.gameObject.SetActive(true);

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = t * t * (3f - 2f * t);

            if (panelBody != null && panelBody.gameObject.activeSelf)
                panelBody.anchoredPosition = Vector2.Lerp(panelStart, panelEnd, t);

            if (toggleRect != null)
                toggleRect.anchoredPosition = Vector2.Lerp(toggleStart, toggleEnd, t);

            yield return null;
        }

        ApplyPositions(panelTargetX, toggleTargetX);
        _slideRoutine = null;
    }

    void UpdateToggleLabel()
    {
        if (toggleLabel == null)
            return;

        toggleLabel.text = _isExpanded ? ExpandedLabel : CollapsedLabel;
    }
}