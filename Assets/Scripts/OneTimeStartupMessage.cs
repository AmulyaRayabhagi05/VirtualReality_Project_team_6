using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OneTimeStartupMessage : MonoBehaviour
{
    // "Once per app run" flag (resets when the app is restarted).
    static bool s_shownThisRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSessionFlags()
    {
        // Ensures this resets even if domain reload is disabled in Editor.
        s_shownThisRun = false;
    }

    [TextArea(2, 6)]
    public string message = "Find the white tablet stands for help.\nPress x to close info.";

    // Keep this open until the player closes it (js2 on controller or Z on PC).
    public bool allowManualClose = true;

    CanvasGroup _group;

    void Start()
    {
        if (s_shownThisRun)
        {
            return;
        }

        BuildUI();
        Show();
        s_shownThisRun = true;
    }

    void Update()
    {
        if (!allowManualClose || _group == null || _group.alpha < 0.5f)
        {
            return;
        }

        // PC: Z to close. Controller: js2 down-edge to close.
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetButtonDown("js2"))
        {
            Hide();
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("StartupMessageCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6500;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        _group = canvasGO.AddComponent<CanvasGroup>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.68f);

        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1100f, 220f);
        panelRT.anchoredPosition = new Vector2(0f, 260f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(panelGO.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 40;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.98f);
        tmp.enableWordWrapping = true;

        var tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (tmpFont != null)
        {
            tmp.font = tmpFont;
        }

        var rt = tmp.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(30f, 24f);
        rt.offsetMax = new Vector2(-30f, -24f);
    }

    void Show()
    {
        if (_group == null) return;
        _group.alpha = 1f;
        _group.interactable = true;
        _group.blocksRaycasts = true;
    }

    void Hide()
    {
        if (_group == null) return;
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }
}
