using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// MFA 模拟器主入口 — 管理 UI、场景切换、全局配置
/// 使用方法：挂到一个空 GameObject 上，按 Play
/// </summary>
public class MFASimulator : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    // Colors
    // ═══════════════════════════════════════════════════
    public static readonly Color BgColor = new Color(0.04f, 0.04f, 0.18f);
    public static readonly Color MainMagnetColor = new Color(0.2f, 0.4f, 0.8f);
    public static readonly Color DistractorColor = new Color(0.8f, 0.2f, 0.2f);
    public static readonly Color FilingColor = new Color(0.8f, 0.67f, 0.27f);
    public static readonly Color ThresholdColor = new Color(1f, 0.53f, 0f);
    public static readonly Color UIBgColor = new Color(0.06f, 0.06f, 0.22f, 0.9f);

    // ═══════════════════════════════════════════════════
    // References
    // ═══════════════════════════════════════════════════
    private Canvas canvas;
    private GameObject sidePanel;
    private Text titleText;
    private Text descText;
    private Text infoText;
    private List<GameObject> sliderObjects = new List<GameObject>();
    private List<Button> sceneButtons = new List<Button>();
    private MonoBehaviour activeScene;
    private bool screenshotMode = false;

    // Current scene's slider callbacks (so scenes can create sliders)
    public delegate void SliderCallback(float value);

    void Start()
    {
        SetupCamera();
        CreateUI();

        // Quantitative analysis: always available across all scenes
        if (GetComponent<FieldDataAnalyzer>() == null)
            gameObject.AddComponent<FieldDataAnalyzer>();

        SwitchScene(1);
    }

    // ═══════════════════════════════════════════════════
    // Camera Setup
    // ═══════════════════════════════════════════════════
    void SetupCamera()
    {
        Camera.main.backgroundColor = BgColor;
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 6f;
        Camera.main.transform.position = new Vector3(0, 0, -10);
    }

    // ═══════════════════════════════════════════════════
    // UI Creation (fully programmatic)
    // ═══════════════════════════════════════════════════
    void CreateUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        CreateTopBar();
        CreateSidePanel();
        CreateScreenshotButton();
    }

    void CreateTopBar()
    {
        var bar = CreatePanel(canvas.transform, "TopBar",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -40), new Vector2(0, 0),
            UIBgColor);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5;
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;

        string[] labels = {
            "1: Comparison", "2: Attention Life",
            "3: Multi-Task", "4: Curvature"
        };

        for (int i = 0; i < labels.Length; i++)
        {
            int sceneIdx = i + 1;
            var btn = CreateButton(bar.transform, labels[i], () => SwitchScene(sceneIdx));
            sceneButtons.Add(btn);
        }
    }

    void CreateSidePanel()
    {
        sidePanel = CreatePanel(canvas.transform, "SidePanel",
            new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(-240, 40), new Vector2(0, -40),
            UIBgColor);

        var vlg = sidePanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        titleText = CreateText(sidePanel.transform, "Scene Title", 18, FontStyle.Bold);
        var titleLE = titleText.gameObject.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 30;

        // Description
        descText = CreateText(sidePanel.transform, "", 12, FontStyle.Normal);
        descText.alignment = TextAnchor.UpperLeft;
        var descLE = descText.gameObject.AddComponent<LayoutElement>();
        descLE.preferredHeight = 60;

        // Info (for real-time values)
        infoText = CreateText(sidePanel.transform, "", 13, FontStyle.Normal);
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.color = new Color(0.7f, 0.9f, 1f);
        var infoLE = infoText.gameObject.AddComponent<LayoutElement>();
        infoLE.preferredHeight = 80;
    }

    void CreateScreenshotButton()
    {
        var btnGO = new GameObject("ScreenshotBtn");
        btnGO.transform.SetParent(canvas.transform, false);

        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(50, 25);
        rt.sizeDelta = new Vector2(80, 30);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.35f, 0.8f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(ToggleScreenshotMode);

        var txt = CreateText(btnGO.transform, "Screenshot", 11, FontStyle.Normal);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.sizeDelta = Vector2.zero;
    }

    // ═══════════════════════════════════════════════════
    // Scene Switching
    // ═══════════════════════════════════════════════════
    public void SwitchScene(int sceneIndex)
    {
        // Remove current scene
        if (activeScene != null)
        {
            if (activeScene is ISceneController sc) sc.Cleanup();
            Destroy(activeScene);
        }
        ClearSliders();

        // Highlight active button
        for (int i = 0; i < sceneButtons.Count; i++)
        {
            var colors = sceneButtons[i].colors;
            colors.normalColor = (i == sceneIndex - 1)
                ? new Color(0.3f, 0.3f, 0.7f)
                : new Color(0.15f, 0.15f, 0.35f);
            sceneButtons[i].colors = colors;
        }

        // Create new scene
        switch (sceneIndex)
        {
            case 1: activeScene = gameObject.AddComponent<ModelComparisonScene>(); break;
            case 2: activeScene = gameObject.AddComponent<AttentionLifeScene>(); break;
            case 3: activeScene = gameObject.AddComponent<MultiTaskScene>(); break;
            case 4: activeScene = gameObject.AddComponent<CurvatureScene>(); break;
        }
    }

    // ═══════════════════════════════════════════════════
    // Public UI Helpers (called by scene controllers)
    // ═══════════════════════════════════════════════════
    public void SetTitle(string title) => titleText.text = title;
    public void SetDescription(string desc) => descText.text = desc;
    public void SetInfo(string info) => infoText.text = info;

    public Slider AddSlider(string label, float min, float max, float initial, SliderCallback callback)
    {
        // Label
        var labelText = CreateText(sidePanel.transform, label, 12, FontStyle.Normal);
        labelText.alignment = TextAnchor.MiddleLeft;
        var labelLE = labelText.gameObject.AddComponent<LayoutElement>();
        labelLE.preferredHeight = 18;
        sliderObjects.Add(labelText.gameObject);

        // Slider container
        var sliderGO = new GameObject("Slider_" + label);
        sliderGO.transform.SetParent(sidePanel.transform, false);
        var sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredHeight = 20;
        sliderObjects.Add(sliderGO);

        var sliderRT = sliderGO.AddComponent<RectTransform>();

        // Background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.35f);
        bgRT.anchorMax = new Vector2(1, 0.65f);
        bgRT.sizeDelta = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.4f);

        // Fill area
        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.35f);
        fillAreaRT.anchorMax = new Vector2(1, 0.65f);
        fillAreaRT.sizeDelta = Vector2.zero;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.5f, 0.9f);

        // Handle
        var handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        var handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.sizeDelta = new Vector2(-10, 0);

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(16, 16);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;

        // Slider component
        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = initial;
        slider.onValueChanged.AddListener((v) => {
            callback(v);
            labelText.text = $"{label}: {v:F1}";
        });

        labelText.text = $"{label}: {initial:F1}";
        return slider;
    }

    void ClearSliders()
    {
        foreach (var go in sliderObjects)
            if (go != null) Destroy(go);
        sliderObjects.Clear();
    }

    void ToggleScreenshotMode()
    {
        screenshotMode = !screenshotMode;
        if (canvas != null)
        {
            // Hide/show all UI except the toggle button
            var topBar = canvas.transform.Find("TopBar");
            if (topBar != null) topBar.gameObject.SetActive(!screenshotMode);
            if (sidePanel != null) sidePanel.SetActive(!screenshotMode);
        }
    }

    // ═══════════════════════════════════════════════════
    // UI Factory Utilities
    // ═══════════════════════════════════════════════════
    GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        var img = go.AddComponent<Image>();
        img.color = color;

        return go;
    }

    Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.35f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.55f);
        colors.pressedColor = new Color(0.35f, 0.35f, 0.65f);
        btn.colors = colors;
        btn.onClick.AddListener(action);

        var txt = CreateText(go.transform, label, 13, FontStyle.Normal);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.sizeDelta = Vector2.zero;

        return btn;
    }

    Text CreateText(Transform parent, string content, int size, FontStyle style)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);

        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;

        return txt;
    }

    /// <summary>
    /// 获取 MFASimulator 实例（供场景脚本使用）
    /// </summary>
    public static MFASimulator Instance
    {
        get { return FindObjectOfType<MFASimulator>(); }
    }
}

/// <summary>
/// 场景控制器接口
/// </summary>
public interface ISceneController
{
    void Cleanup();
}
