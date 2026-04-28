using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;

public class NetworkGameManager : MonoBehaviour
{
    private string joinAddress = "192.168.1.100";
    private ushort port = 7777;

    private GameObject lobbyUI;
    private InputField _ipInputField;
    private TouchScreenKeyboard _keyboard;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 2;
    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;
    private float stickThreshold = 0.5f;
    private float navigationCooldown = 0.3f;
    private Button _hostBtn;
    private Button _joinBtn;

    private static readonly Color SELECTED_COLOR = new Color(1f, 0.85f, 0f);
    private static readonly Color HOST_COLOR      = new Color(0.15f, 0.55f, 0.95f);
    private static readonly Color JOIN_COLOR      = new Color(0.15f, 0.75f, 0.45f);

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(BuildLobbyCanvasWhenReady());
#endif
    }

    IEnumerator BuildLobbyCanvasWhenReady()
    {
        while (Camera.main == null)
            yield return null;
        BuildLobbyCanvas();
    }

    void Update()
    {
        if (lobbyUI == null) return;
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        if (_keyboard != null)
        {
            if (_keyboard.status == TouchScreenKeyboard.Status.Done ||
                _keyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                if (_keyboard.status == TouchScreenKeyboard.Status.Done)
                {
                    joinAddress = _keyboard.text;
                    if (_ipInputField != null) _ipInputField.text = joinAddress;
                }
                _keyboard = null;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            _keyboard = TouchScreenKeyboard.Open(
                joinAddress,
                TouchScreenKeyboardType.Default,
                false, false, false, false,
                "Enter host IP:");
            return;
        }

        _cooldownTimer -= Time.unscaledDeltaTime;
        HandleNavigation();
        HandleConfirm();
    }

    void HandleNavigation()
    {
        float axis = Input.GetAxis("Vertical");

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _stickNeutral = true;
            return;
        }

        if (!_stickNeutral || _cooldownTimer > 0f) return;

        if (axis > stickThreshold)
            _selectedIndex = (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT;
        else
            _selectedIndex = (_selectedIndex + 1) % OPTION_COUNT;

        _stickNeutral = false;
        _cooldownTimer = navigationCooldown;
        UpdateHighlight();
    }

    void HandleConfirm()
    {
        if (!Input.GetKeyDown(KeyCode.JoystickButton2)) return;
        ConfirmSelection();
    }

    void ConfirmSelection()
    {
        if (_selectedIndex == 0)
        {
            SetTransportAddress("0.0.0.0", port);
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            if (_ipInputField != null)
                joinAddress = _ipInputField.text;
            SetTransportAddress(joinAddress, port);
            NetworkManager.Singleton.StartClient();
        }
        Destroy(lobbyUI);
    }

    void UpdateHighlight()
    {
        SetButtonColor(_hostBtn, _selectedIndex == 0 ? SELECTED_COLOR : HOST_COLOR);
        SetButtonColor(_joinBtn, _selectedIndex == 1 ? SELECTED_COLOR : JOIN_COLOR);
    }

    void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }


    void OnGUI()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return;
#endif
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        GUILayout.BeginArea(new Rect(Screen.width / 2 - 130, Screen.height / 2 - 110, 260, 210));
        GUILayout.Label("Multiplayer", GUILayout.Height(30));
        GUILayout.Label("Server IP:");
        joinAddress = GUILayout.TextField(joinAddress, GUILayout.Height(36));
        GUILayout.Space(6);
        if (GUILayout.Button("Host Game", GUILayout.Height(50)))
        {
            SetTransportAddress("0.0.0.0", port);
            NetworkManager.Singleton.StartHost();
        }
        GUILayout.Space(6);
        if (GUILayout.Button("Join Game", GUILayout.Height(50)))
        {
            SetTransportAddress(joinAddress, port);
            NetworkManager.Singleton.StartClient();
        }
        GUILayout.EndArea();
    }


    void BuildLobbyCanvas()
    {
        Camera cam = Camera.main;

        lobbyUI = new GameObject("LobbyUI");
        lobbyUI.transform.SetParent(cam.transform, false);
        lobbyUI.transform.localPosition = new Vector3(0f, 0f, 2f);
        lobbyUI.transform.localRotation = Quaternion.identity;
        lobbyUI.transform.localScale    = Vector3.one * 0.003f;

        var canvas = lobbyUI.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        lobbyUI.AddComponent<GraphicRaycaster>();

        var canvasRT = lobbyUI.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(400, 420);

        var bg = lobbyUI.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        AddLabel(canvasRT, "Multiplayer",        new Vector2(0,  175), 32, FontStyle.Bold);
        AddLabel(canvasRT, "Server IP Address:",  new Vector2(0,  110), 22);
        _ipInputField = AddInputField(canvasRT, joinAddress, new Vector2(0, 60));
        AddLabel(canvasRT, "Tap screen to edit IP", new Vector2(0, 18), 18);

        _hostBtn = AddButton(canvasRT, "Host Game", new Vector2(0, -55),  HOST_COLOR);
        _joinBtn = AddButton(canvasRT, "Join Game", new Vector2(0, -140), JOIN_COLOR);

        AddLabel(canvasRT, "Use stick to select, js2 to confirm", new Vector2(0, -195), 16);

        UpdateHighlight();
    }


    static Font _defaultFont;
    static Font DefaultFont
    {
        get
        {
            if (_defaultFont == null)
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _defaultFont;
        }
    }

    void AddLabel(RectTransform parent, string text, Vector2 pos, int fontSize, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(380, 50);
        rt.anchoredPosition = pos;

        var txt = go.AddComponent<Text>();
        txt.font      = DefaultFont;
        txt.text      = text;
        txt.fontSize  = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
    }

    InputField AddInputField(RectTransform parent, string defaultText, Vector2 pos)
    {
        var go = new GameObject("IPInputField");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(340, 55);
        rt.anchoredPosition = pos;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var field = go.AddComponent<InputField>();

        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform, false);
        var pRT = placeholderGO.AddComponent<RectTransform>();
        pRT.anchorMin  = Vector2.zero;
        pRT.anchorMax  = Vector2.one;
        pRT.offsetMin  = new Vector2(8, 4);
        pRT.offsetMax  = new Vector2(-8, -4);
        var placeholder = placeholderGO.AddComponent<Text>();
        placeholder.font      = DefaultFont;
        placeholder.text      = "Enter IP...";
        placeholder.fontSize  = 24;
        placeholder.color     = new Color(0.6f, 0.6f, 0.6f, 1f);
        placeholder.alignment = TextAnchor.MiddleCenter;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin  = Vector2.zero;
        tRT.anchorMax  = Vector2.one;
        tRT.offsetMin  = new Vector2(8, 4);
        tRT.offsetMax  = new Vector2(-8, -4);
        var txt = textGO.AddComponent<Text>();
        txt.font      = DefaultFont;
        txt.fontSize  = 24;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        field.targetGraphic       = bg;
        field.textComponent       = txt;
        field.placeholder         = placeholder;
        field.text                = defaultText;
        field.characterLimit      = 15;
        field.contentType         = InputField.ContentType.Standard;
        field.characterValidation = InputField.CharacterValidation.None;

        return field;
    }

    Button AddButton(RectTransform parent, string label, Vector2 pos, Color color)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(340, 70);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);

        var textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta      = rt.sizeDelta;
        textRT.anchoredPosition = Vector2.zero;

        var txt = textGO.AddComponent<Text>();
        txt.font      = DefaultFont;
        txt.text      = label;
        txt.fontSize  = 28;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;

        return btn;
    }

    void SetTransportAddress(string address, ushort targetPort)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(address, targetPort);
    }
}
