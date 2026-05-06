using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StandInfoPanelManager : MonoBehaviour
{
    public static StandInfoPanelManager instance;

    [Header("Placement")]
    public float panelWidth = 1400f;
    public float panelHeight = 800f;

    [Header("UI (runtime created if null)")]
    public Canvas canvas;
    public CanvasGroup canvasGroup;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Button closeButton;

    public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0.5f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUI();
        Hide();
    }

    void EnsureUI()
    {
        if (canvas != null && canvasGroup != null && titleText != null && bodyText != null && closeButton != null)
        {
            return;
        }

        // Root canvas
        var canvasGO = new GameObject("StandInfoPanelCanvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Scale so pixel units map to metres (900px → 0.9 m wide)
        canvasGO.transform.localScale = Vector3.one * 0.001f;

        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // Background panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.78f);

        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);
        panelRT.anchoredPosition = Vector2.zero;

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 64;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.color = Color.white;

        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(-220f, 70f);
        titleRT.anchoredPosition = new Vector2(0f, -22f);
        titleRT.offsetMin = new Vector2(30f, titleRT.offsetMin.y);
        titleRT.offsetMax = new Vector2(-190f, titleRT.offsetMax.y);

        // Body text
        var bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(panelGO.transform, false);
        bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 40;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        bodyText.enableWordWrapping = true;

        var bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.pivot = new Vector2(0.5f, 0.5f);
        bodyRT.offsetMin = new Vector2(30f, 100f);  // leaves room for close button at bottom
        bodyRT.offsetMax = new Vector2(-30f, -110f); // leaves room for title at top

        // Close button — anchored to bottom-centre
        var closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(panelGO.transform, false);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        closeButton = closeGO.AddComponent<Button>();

        var closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.5f, 0f);
        closeRT.anchorMax = new Vector2(0.5f, 0f);
        closeRT.pivot = new Vector2(0.5f, 0f);
        closeRT.sizeDelta = new Vector2(240f, 65f);
        closeRT.anchoredPosition = new Vector2(0f, 18f);

        var closeTxtGO = new GameObject("Text");
        closeTxtGO.transform.SetParent(closeGO.transform, false);
        var closeTxt = closeTxtGO.AddComponent<TextMeshProUGUI>();
        closeTxt.text = "Press x to close";
        closeTxt.fontSize = 36;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = Color.white;
        var closeTxtRT = closeTxtGO.GetComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero;
        closeTxtRT.anchorMax = Vector2.one;
        closeTxtRT.offsetMin = Vector2.zero;
        closeTxtRT.offsetMax = Vector2.zero;

        // Try to assign a known TMP font from Resources so text actually renders.
        var tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (tmpFont != null)
        {
            titleText.font = tmpFont;
            bodyText.font = tmpFont;
            closeTxt.font = tmpFont;
        }

        closeButton.onClick.AddListener(Hide);
    }

    PlayerMovement GetLocalPlayerMovement()
    {
        foreach (var pm in FindObjectsOfType<PlayerMovement>())
            if (pm.IsOwner) return pm;
        return null;
    }

    void PlaceInFrontOfCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || canvas == null) return;

        // Flat forward so the panel stays vertical
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;
        forward.Normalize();

        canvas.transform.position = cam.transform.position + forward * 1.0f;
        canvas.transform.rotation = Quaternion.LookRotation(forward);
    }

    public void Show(string title, string body)
    {
        EnsureUI();

        if (titleText != null) titleText.text = string.IsNullOrEmpty(title) ? "Info" : title;
        if (bodyText != null) bodyText.text = body ?? "";

        PlaceInFrontOfCamera();

        GetLocalPlayerMovement()?.SetMovementEnabled(false);

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        EnsureUI();

        GetLocalPlayerMovement()?.SetMovementEnabled(true);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Toggle(string title, string body)
    {
        if (IsOpen)
        {
            Hide();
        }
        else
        {
            Show(title, body);
        }
    }
}
