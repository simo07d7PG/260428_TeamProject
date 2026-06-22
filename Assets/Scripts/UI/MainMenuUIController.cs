using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>'MainMenu' 씬의 메인 화면을 구성하고 시작 의도/씬 로드를 처리합니다.</summary>
public class MainMenuUIController : MonoBehaviour
{
    [Tooltip("로드할 게임 씬 이름 (Build Settings에 포함되어야 함)")]
    [SerializeField] string gameSceneName = "MainScene";

    RectTransform _root;
    RectTransform _menuRoot;
    RectTransform _optionsPanel;
    RectTransform _confirmPanel;
    Button _continueButton;

    TextMeshProUGUI _volumeValue;
    TextMeshProUGUI _fullscreenValue;
    TextMeshProUGUI _resValue;
    TextMeshProUGUI _aaValue;
    readonly List<Vector2Int> _resolutions = new();
    int _resIndex;
    int _aaIndex;
    bool _fullscreen;

    void Awake()
    {
        RectTransform canvasRect = EnsureCanvas();

        BuildResolutions();
        _fullscreen = SettingsUtility.Fullscreen;
        _aaIndex = Mathf.Max(0, System.Array.IndexOf(SettingsUtility.MsaaOptions, SettingsUtility.MsaaSamples));

        RectTransform existing = canvasRect.Find("MainMenuUI") as RectTransform;
        if (existing != null)
        {
            BindMenu(existing);
        }
        else
        {
            BuildMenuUI(canvasRect);
            UIFontUtility.ApplyToHierarchy(_root);
        }

        _optionsPanel.gameObject.SetActive(false);
        _confirmPanel.gameObject.SetActive(false);
        RefreshContinueButton();
        RefreshOptionLabels();
    }

    /// <summary>에디터에서 씬에 메인 메뉴를 굽기 위한 진입점.</summary>
    public void EditorBuild()
    {
        RectTransform canvasRect = EnsureCanvas();
        BuildResolutions();
        _fullscreen = SettingsUtility.Fullscreen;
        _aaIndex = Mathf.Max(0, System.Array.IndexOf(SettingsUtility.MsaaOptions, SettingsUtility.MsaaSamples));

        if (canvasRect.Find("MainMenuUI") == null)
            BuildMenuUI(canvasRect);
        UIFontUtility.ApplyToHierarchy(_root);
    }

    void BuildMenuUI(RectTransform canvasRect)
    {
        GameObject rootObj = UIFactoryUtility.CreateUIObject("MainMenuUI", canvasRect);
        _root = rootObj.GetComponent<RectTransform>();
        UIFactoryUtility.StretchHost(_root);

        BuildMenu();
        BuildOptions();
        BuildConfirm();
    }

    void BindMenu(RectTransform root)
    {
        _root = root;
        _menuRoot = root.Find("MainMenuRoot") as RectTransform;
        if (_menuRoot != null)
        {
            BindButton(_menuRoot.Find("Btn_이어하기") as RectTransform, OnContinue, out _continueButton);
            BindButton(_menuRoot.Find("Btn_새로하기") as RectTransform, OnNewGame, out _);
            BindButton(_menuRoot.Find("Btn_옵션") as RectTransform, OnOpenOptions, out _);
            BindButton(_menuRoot.Find("Btn_종료") as RectTransform, OnQuit, out _);

            _confirmPanel = _menuRoot.Find("ConfirmDim") as RectTransform;
            _optionsPanel = _menuRoot.Find("OptionsDim") as RectTransform;
        }

        if (_confirmPanel != null)
        {
            RectTransform panel = _confirmPanel.Find("ConfirmPanel") as RectTransform;
            BindButton(panel?.Find("Yes") as RectTransform, () => { _confirmPanel.gameObject.SetActive(false); StartFresh(); }, out _);
            BindButton(panel?.Find("No") as RectTransform, () => _confirmPanel.gameObject.SetActive(false), out _);
        }

        if (_optionsPanel != null)
        {
            BindButton(_optionsPanel as RectTransform, () => _optionsPanel.gameObject.SetActive(false), out _);
            RectTransform panel = _optionsPanel.Find("OptionsPanel") as RectTransform;
            _volumeValue = BindRow(panel, "Row_사운드", () => AdjustVolume(-0.1f), () => AdjustVolume(0.1f));
            _fullscreenValue = BindRow(panel, "Row_전체화면", ToggleFullscreen, ToggleFullscreen);
            _resValue = BindRow(panel, "Row_해상도", () => CycleResolution(-1), () => CycleResolution(1));
            _aaValue = BindRow(panel, "Row_안티에일리어싱", () => CycleAa(-1), () => CycleAa(1));

            BindButton(panel?.Find("Apply") as RectTransform, ApplyDisplay, out _);
            BindButton(panel?.Find("Close") as RectTransform, () => _optionsPanel.gameObject.SetActive(false), out _);
        }
    }

    static void BindButton(RectTransform rect, UnityEngine.Events.UnityAction action, out Button button)
    {
        button = rect != null ? rect.GetComponent<Button>() : null;
        if (button != null)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }

    TextMeshProUGUI BindRow(RectTransform panel, string rowName, UnityEngine.Events.UnityAction onLeft, UnityEngine.Events.UnityAction onRight)
    {
        RectTransform row = panel?.Find(rowName) as RectTransform;
        if (row == null)
            return null;
        BindButton(row.Find("Left") as RectTransform, onLeft, out _);
        BindButton(row.Find("Right") as RectTransform, onRight, out _);
        return row.Find("Value")?.GetComponent<TextMeshProUGUI>();
    }

    RectTransform EnsureCanvas()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        return canvas.transform as RectTransform;
    }

    void BuildMenu()
    {
        GameObject root = UIFactoryUtility.CreateUIObject("MainMenuRoot", _root, typeof(Image));
        _menuRoot = root.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_menuRoot);
        root.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.07f, 1f);

        TextMeshProUGUI title = UIFactoryUtility.CreateLabel(_menuRoot, "Title", "카페 경영\n시뮬레이션", 52f);
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 200f);
        titleRect.sizeDelta = new Vector2(760f, 180f);
        title.color = new Color(1f, 0.92f, 0.78f, 1f);

        _continueButton = CreateMenuButton("이어하기", new Vector2(0f, 40f), new Color(0.30f, 0.62f, 0.36f, 1f), OnContinue);
        CreateMenuButton("새로하기", new Vector2(0f, -32f), new Color(0.25f, 0.5f, 0.78f, 1f), OnNewGame);
        CreateMenuButton("옵션", new Vector2(0f, -104f), new Color(0.30f, 0.34f, 0.40f, 1f), OnOpenOptions);
        CreateMenuButton("종료", new Vector2(0f, -176f), new Color(0.5f, 0.3f, 0.3f, 1f), OnQuit);
    }

    Button CreateMenuButton(string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction action)
    {
        Button button = UIFactoryUtility.CreateButton(_menuRoot, $"Btn_{label}", label, color);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300f, 60f);
        TextMeshProUGUI lbl = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (lbl != null)
            lbl.fontSize = 24f;
        button.onClick.AddListener(action);
        return button;
    }

    void OnContinue()
    {
        if (!SaveLoadUtility.HasSave())
            return;

        GameBootstrap.PendingMode = GameBootstrap.StartMode.Continue;
        LoadGameScene();
    }

    void OnNewGame()
    {
        if (SaveLoadUtility.HasSave())
        {
            _confirmPanel.gameObject.SetActive(true);
            return;
        }

        StartFresh();
    }

    void StartFresh()
    {
        GameBootstrap.PendingMode = GameBootstrap.StartMode.NewGame;
        LoadGameScene();
    }

    void LoadGameScene()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError($"[MainMenu] '{gameSceneName}' 씬을 찾을 수 없습니다. Build Settings에 추가하고 이름을 확인하세요.");
    }

    void OnOpenOptions()
    {
        RefreshOptionLabels();
        _optionsPanel.gameObject.SetActive(true);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RefreshContinueButton()
    {
        if (_continueButton != null)
            _continueButton.interactable = SaveLoadUtility.HasSave();
    }

    void BuildConfirm()
    {
        GameObject dim = UIFactoryUtility.CreateUIObject("ConfirmDim", _menuRoot, typeof(Image));
        _confirmPanel = dim.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_confirmPanel);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        GameObject panelObj = UIFactoryUtility.CreateUIObject("ConfirmPanel", _confirmPanel, typeof(Image));
        RectTransform panel = panelObj.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(460f, 220f);
        panelObj.GetComponent<Image>().color = new Color(0.14f, 0.13f, 0.16f, 1f);

        TextMeshProUGUI msg = UIFactoryUtility.CreateLabel(panel, "Msg", "새로 시작하면 저장된 진행이 사라집니다.\n계속할까요?", 18f);
        RectTransform msgRect = msg.rectTransform;
        msgRect.anchorMin = new Vector2(0f, 1f);
        msgRect.anchorMax = new Vector2(1f, 1f);
        msgRect.pivot = new Vector2(0.5f, 1f);
        msgRect.offsetMin = new Vector2(20f, -120f);
        msgRect.offsetMax = new Vector2(-20f, -24f);

        Button yes = UIFactoryUtility.CreateButton(panel, "Yes", "새로 시작", new Color(0.5f, 0.3f, 0.3f, 1f));
        PlaceConfirmButton(yes.GetComponent<RectTransform>(), -110f);
        yes.onClick.AddListener(() => { _confirmPanel.gameObject.SetActive(false); StartFresh(); });

        Button no = UIFactoryUtility.CreateButton(panel, "No", "취소", new Color(0.3f, 0.34f, 0.4f, 1f));
        PlaceConfirmButton(no.GetComponent<RectTransform>(), 110f);
        no.onClick.AddListener(() => _confirmPanel.gameObject.SetActive(false));
    }

    static void PlaceConfirmButton(RectTransform rect, float xOffset)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(xOffset, 18f);
        rect.sizeDelta = new Vector2(190f, 48f);
    }

    void BuildOptions()
    {
        GameObject dim = UIFactoryUtility.CreateUIObject("OptionsDim", _menuRoot, typeof(Image), typeof(Button));
        _optionsPanel = dim.GetComponent<RectTransform>();
        UIFactoryUtility.StretchFull(_optionsPanel);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        dim.GetComponent<Button>().onClick.AddListener(() => _optionsPanel.gameObject.SetActive(false));

        GameObject panelObj = UIFactoryUtility.CreateUIObject("OptionsPanel", _optionsPanel, typeof(Image));
        RectTransform panel = panelObj.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(560f, 540f);
        panelObj.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.17f, 1f);

        TextMeshProUGUI title = UIFactoryUtility.CreateLabel(panel, "OptTitle", "옵션", 28f);
        title.fontStyle = FontStyles.Bold;
        RectTransform tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.offsetMin = new Vector2(0f, -56f); tr.offsetMax = new Vector2(0f, -16f);

        _volumeValue = CreateAdjustRow(panel, -80f, "사운드", () => AdjustVolume(-0.1f), () => AdjustVolume(0.1f));
        _fullscreenValue = CreateAdjustRow(panel, -136f, "전체화면", ToggleFullscreen, ToggleFullscreen);
        _resValue = CreateAdjustRow(panel, -192f, "해상도", () => CycleResolution(-1), () => CycleResolution(1));
        _aaValue = CreateAdjustRow(panel, -248f, "안티에일리어싱", () => CycleAa(-1), () => CycleAa(1));

        Button apply = UIFactoryUtility.CreateButton(panel, "Apply", "해상도/전체화면 적용", new Color(0.30f, 0.62f, 0.36f, 1f));
        RectTransform applyRect = apply.GetComponent<RectTransform>();
        applyRect.anchorMin = applyRect.anchorMax = new Vector2(0.5f, 0f); applyRect.pivot = new Vector2(0.5f, 0f);
        applyRect.anchoredPosition = new Vector2(0f, 76f);
        applyRect.sizeDelta = new Vector2(360f, 46f);
        apply.onClick.AddListener(ApplyDisplay);

        Button close = UIFactoryUtility.CreateButton(panel, "Close", "닫기", new Color(0.25f, 0.45f, 0.75f, 1f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f); closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 20f);
        closeRect.sizeDelta = new Vector2(220f, 46f);
        close.onClick.AddListener(() => _optionsPanel.gameObject.SetActive(false));
    }

    TextMeshProUGUI CreateAdjustRow(RectTransform panel, float topY, string title,
        UnityEngine.Events.UnityAction onLeft, UnityEngine.Events.UnityAction onRight)
    {
        GameObject rowObj = UIFactoryUtility.CreateUIObject($"Row_{title}", panel);
        RectTransform row = rowObj.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(1f, 1f); row.pivot = new Vector2(0.5f, 1f);
        row.offsetMin = new Vector2(24f, topY - 48f); row.offsetMax = new Vector2(-24f, topY);

        TextMeshProUGUI titleLabel = UIFactoryUtility.CreateLabel(row, "Title", title, 18f);
        titleLabel.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f); titleRect.anchorMax = new Vector2(0f, 1f); titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero; titleRect.sizeDelta = new Vector2(200f, 0f);

        Button right = UIFactoryUtility.CreateButton(row, "Right", ">", new Color(0.28f, 0.32f, 0.38f, 1f));
        PinRight(right.GetComponent<RectTransform>(), 0f, 42f);
        right.onClick.AddListener(onRight);

        TextMeshProUGUI value = UIFactoryUtility.CreateLabel(row, "Value", "-", 18f);
        RectTransform valueRect = value.rectTransform;
        valueRect.anchorMin = valueRect.anchorMax = new Vector2(1f, 0.5f); valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(-50f, 0f); valueRect.sizeDelta = new Vector2(150f, 40f);

        Button left = UIFactoryUtility.CreateButton(row, "Left", "<", new Color(0.28f, 0.32f, 0.38f, 1f));
        PinRight(left.GetComponent<RectTransform>(), 208f, 42f);
        left.onClick.AddListener(onLeft);

        return value;
    }

    static void PinRight(RectTransform rect, float insetFromRight, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f); rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-insetFromRight, 0f);
        rect.sizeDelta = new Vector2(size, size);
    }

    void AdjustVolume(float delta)
    {
        SettingsUtility.MasterVolume = SettingsUtility.MasterVolume + delta;
        RefreshOptionLabels();
    }

    void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        SettingsUtility.Fullscreen = _fullscreen;
        RefreshOptionLabels();
    }

    void CycleResolution(int dir)
    {
        if (_resolutions.Count == 0)
            return;

        _resIndex = (_resIndex + dir + _resolutions.Count) % _resolutions.Count;
        RefreshOptionLabels();
    }

    void CycleAa(int dir)
    {
        int n = SettingsUtility.MsaaOptions.Length;
        _aaIndex = (_aaIndex + dir + n) % n;
        SettingsUtility.MsaaSamples = SettingsUtility.MsaaOptions[_aaIndex];
        RefreshOptionLabels();
    }

    void ApplyDisplay()
    {
        if (_resolutions.Count == 0)
            return;

        Vector2Int res = _resolutions[Mathf.Clamp(_resIndex, 0, _resolutions.Count - 1)];
        SettingsUtility.ApplyResolution(res.x, res.y, _fullscreen);
    }

    void RefreshOptionLabels()
    {
        if (_volumeValue != null)
            _volumeValue.text = $"{Mathf.RoundToInt(SettingsUtility.MasterVolume * 100f)}%";
        if (_fullscreenValue != null)
            _fullscreenValue.text = _fullscreen ? "켜짐" : "꺼짐";
        if (_resValue != null && _resolutions.Count > 0)
        {
            Vector2Int r = _resolutions[Mathf.Clamp(_resIndex, 0, _resolutions.Count - 1)];
            _resValue.text = $"{r.x} x {r.y}";
        }
        if (_aaValue != null)
        {
            int samples = SettingsUtility.MsaaOptions[Mathf.Clamp(_aaIndex, 0, SettingsUtility.MsaaOptions.Length - 1)];
            _aaValue.text = samples <= 1 ? "끄기" : $"{samples}x";
        }
    }

    void BuildResolutions()
    {
        _resolutions.Clear();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int v = new Vector2Int(r.width, r.height);
            if (!_resolutions.Contains(v))
                _resolutions.Add(v);
        }

        if (_resolutions.Count == 0)
            _resolutions.Add(new Vector2Int(Screen.width, Screen.height));

        _resIndex = _resolutions.FindIndex(v => v.x == Screen.width && v.y == Screen.height);
        if (_resIndex < 0)
            _resIndex = _resolutions.Count - 1;
    }
}
