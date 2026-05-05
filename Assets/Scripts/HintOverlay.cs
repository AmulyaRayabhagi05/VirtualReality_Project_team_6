using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HintOverlay : MonoBehaviour
{
    [TextArea(1, 3)]
    public string message = "Find white tablet stands for help";

    public int fontSize = 34;
    public Vector2 padding = new Vector2(24f, 20f);

    Canvas _canvas;
    TextMeshProUGUI _text;

    void Awake()
    {
        EnsureUI();
        SetMessage(message);
    }

    public void SetMessage(string msg)
    {
        message = msg;
        if (_text != null)
        {
            _text.text = message;
        }
    }

    void EnsureUI()
    {
        if (_canvas != null && _text != null)
        {
            return;
        }

        var canvasGO = new GameObject("HintOverlayCanvas");
        canvasGO.transform.SetParent(transform, false);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 6000;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("HintText");
        textGO.transform.SetParent(canvasGO.transform, false);

        _text = textGO.AddComponent<TextMeshProUGUI>();
        _text.fontSize = fontSize;
        _text.color = new Color(1f, 1f, 1f, 0.95f);
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.enableWordWrapping = true;

        // Ensure TMP renders by using the default font asset from Resources if available.
        var tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (tmpFont != null)
        {
            _text.font = tmpFont;
        }

        // Subtle background for readability (optional).
        var bgGO = new GameObject("HintBg");
        bgGO.transform.SetParent(textGO.transform, false);
        bgGO.transform.SetAsFirstSibling();
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);

        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(-12f, -10f);
        bgRT.offsetMax = new Vector2(12f, 10f);

        var rt = _text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(padding.x, -padding.y);
        rt.sizeDelta = new Vector2(900f, 180f);
    }
}

