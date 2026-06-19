using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PreparationManager 하위에 Merge UI 계층을 자동 생성합니다.
/// </summary>
public static class PreparationUIViewFactory
{
    public static Canvas Build(Transform managerRoot)
    {
        Transform existingCanvas = ManagerUtility.FindDeepChild(managerRoot, "PreparationCanvas");
        if (existingCanvas != null && existingCanvas.TryGetComponent(out Canvas existing))
            return existing;

        GameObject canvasObject = new GameObject(
            "PreparationCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(managerRoot, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject gridPanel = ManagerUtility.CreatePanel(
            canvasObject.transform,
            "MergeGridPanel",
            new Vector2(0.5f, 0.55f),
            new Vector2(360f, 360f));
        ManagerUtility.GetOrAddComponent<MergeGridUI>(gridPanel);

        GameObject basicInventoryPanel = ManagerUtility.CreatePanel(
            canvasObject.transform,
            "BasicInventoryPanel",
            new Vector2(0.2f, 0.2f),
            new Vector2(420f, 120f));
        HorizontalLayoutGroup basicLayout = ManagerUtility.GetOrAddComponent<HorizontalLayoutGroup>(basicInventoryPanel);
        basicLayout.spacing = 8f;
        basicLayout.childAlignment = TextAnchor.MiddleLeft;

        GameObject advancedInventoryPanel = ManagerUtility.CreatePanel(
            canvasObject.transform,
            "AdvancedInventoryPanel",
            new Vector2(0.55f, 0.2f),
            new Vector2(420f, 120f));
        HorizontalLayoutGroup advancedLayout = ManagerUtility.GetOrAddComponent<HorizontalLayoutGroup>(advancedInventoryPanel);
        advancedLayout.spacing = 8f;
        advancedLayout.childAlignment = TextAnchor.MiddleLeft;

        GameObject infoPanel = ManagerUtility.CreatePanel(
            canvasObject.transform,
            "MergeInfoPanel",
            new Vector2(0.5f, 0.88f),
            new Vector2(640f, 80f));
        ManagerUtility.CreateText(infoPanel.transform, "DisposalCostText", "쓰레기 폐기 비용: 0 Coin", 20, TextAnchor.MiddleCenter);

        Text feedbackText = ManagerUtility.CreateText(infoPanel.transform, "FeedbackText", string.Empty, 22, TextAnchor.MiddleCenter);
        feedbackText.gameObject.SetActive(false);

        ManagerUtility.GetOrAddComponent<MergeUIController>(infoPanel);

        return canvas;
    }
}