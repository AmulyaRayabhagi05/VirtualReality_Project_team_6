using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

// Add this component to the NetworkManager GameObject in your scene.
// Builds a World Space Canvas at runtime so the lobby buttons are visible in Cardboard VR.
public class NetworkGameManager : MonoBehaviour
{
    private string joinAddress = "127.0.0.1";
    private ushort port = 7777;

    private GameObject lobbyUI;

    void Start()
    {
        // In VR builds the Game view doesn't show OnGUI, so use a World Space Canvas.
        // In the Editor (e.g. Multiplayer Play Mode) OnGUI is simpler and works fine.
        if (XRSettings.isDeviceActive)
            BuildLobbyCanvas();
    }

    void OnGUI()
    {
        if (XRSettings.isDeviceActive) return;
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        GUILayout.BeginArea(new Rect(20, 20, 260, 150));
        GUILayout.Label("Multiplayer");
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
        lobbyUI = new GameObject("LobbyUI");

        var canvas = lobbyUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        lobbyUI.AddComponent<GraphicRaycaster>();

        // Size in canvas pixels; scale brings it to ~1m x 0.7m in world space
        var canvasRT = lobbyUI.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(400, 280);
        lobbyUI.transform.localScale = Vector3.one * 0.0025f;

        // Position 2.5m in front of the camera (or origin if camera not ready yet)
        Camera cam = Camera.main;
        if (cam != null)
        {
            lobbyUI.transform.position = cam.transform.position + cam.transform.forward * 2.5f;
            lobbyUI.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }
        else
        {
            lobbyUI.transform.position = new Vector3(0f, 1.6f, 2.5f);
        }

        // Dark background panel
        var bg = lobbyUI.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        AddLabel(canvasRT, "Multiplayer", new Vector2(0, 95), 32, FontStyle.Bold);

        AddButton(canvasRT, "Host Game", new Vector2(0, 25), new Color(0.15f, 0.55f, 0.95f), () =>
        {
            SetTransportAddress("0.0.0.0", port);
            NetworkManager.Singleton.StartHost();
            Destroy(lobbyUI);
        });

        AddButton(canvasRT, "Join Game", new Vector2(0, -65), new Color(0.15f, 0.75f, 0.45f), () =>
        {
            SetTransportAddress(joinAddress, port);
            NetworkManager.Singleton.StartClient();
            Destroy(lobbyUI);
        });
    }

    void AddLabel(RectTransform parent, string text, Vector2 pos, int fontSize, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, 50);
        rt.anchoredPosition = pos;

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    void AddButton(RectTransform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 70);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);

        var textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta = rt.sizeDelta;
        textRT.anchoredPosition = Vector2.zero;

        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 28;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    void SetTransportAddress(string address, ushort targetPort)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(address, targetPort);
    }
}
