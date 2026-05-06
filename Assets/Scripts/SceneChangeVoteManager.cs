using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChangeVoteManager : NetworkBehaviour
{
    public static SceneChangeVoteManager Instance { get; private set; }

    private const float VOTE_TIMEOUT    = 30f;
    private const float STICK_THRESHOLD = 0.5f;
    private const float STICK_COOLDOWN  = 0.3f;
    private const int   VOTE_OPTION_COUNT = 2;

    private bool   _voteInProgress;
    private string _pendingScene;
    private ulong  _requesterId;
    private readonly Dictionary<ulong, bool> _votes = new();
    private Coroutine _timeoutCoroutine;

    private GameObject _voteUI;
    private Text       _countdownText;
    private float      _countdownRemaining;
    private bool       _showingCountdown;
    private bool       _hasVoted;
    private bool       _isRequester;

    private int            _voteSelection;
    private Button         _acceptBtn;
    private Button         _declineBtn;
    private bool           _stickNeutral = true;
    private float          _stickCooldown;
    private PlayerMovement _localPlayerMovement;

    private static readonly Color ACCEPT_COLOR       = new Color(0.10f, 0.65f, 0.25f);
    private static readonly Color DECLINE_COLOR      = new Color(0.70f, 0.15f, 0.15f);
    private static readonly Color SELECTED_HIGHLIGHT = new Color(1.00f, 0.85f, 0.00f);

    public override void OnNetworkSpawn()  { Instance = this; }
    public override void OnNetworkDespawn() { if (Instance == this) Instance = null; DestroyVoteUI(); }

    private void Update()
    {
        if (!_showingCountdown) return;

        _countdownRemaining -= Time.unscaledDeltaTime;
        if (_countdownText != null)
            _countdownText.text = Mathf.Max(0, Mathf.CeilToInt(_countdownRemaining)) + "s";

        if (_isRequester || _hasVoted) return;
        _stickCooldown -= Time.unscaledDeltaTime;
        HandleVoteNavigation();
        HandleVoteConfirm();
    }

    public void RequestSceneChange(string sceneName)
    {
        var nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsListening || !IsSpawned)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (nm.ConnectedClientsIds.Count <= 1)
        {
            if (nm.IsServer)
                nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            else
                SoloLoadServerRpc(sceneName);
            return;
        }

        RequestVoteServerRpc(sceneName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SoloLoadServerRpc(string sceneName)
    {
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestVoteServerRpc(string sceneName, ServerRpcParams rpc = default)
    {
        if (_voteInProgress)
        {
            VoteBlockedClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { rpc.Receive.SenderClientId } }
            });
            return;
        }

        _voteInProgress = true;
        _pendingScene   = sceneName;
        _requesterId    = rpc.Receive.SenderClientId;
        _votes.Clear();
        _votes[_requesterId] = true; // requester auto-accepts

        ShowVoteUIClientRpc(sceneName, _requesterId, VOTE_TIMEOUT);
        _timeoutCoroutine = StartCoroutine(TimeoutRoutine());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitVoteServerRpc(bool accepted, ServerRpcParams rpc = default)
    {
        if (!_voteInProgress) return;
        _votes[rpc.Receive.SenderClientId] = accepted;
        CheckAllVotes();
    }


    [ClientRpc]
    private void VoteBlockedClientRpc(ClientRpcParams _ = default)
    {
        ShowBriefMessage("A floor-switch vote is already in progress.");
    }

    [ClientRpc]
    private void ShowVoteUIClientRpc(string sceneName, ulong requesterId, float timeout)
    {
        _hasVoted           = false;
        _countdownRemaining = timeout;
        _showingCountdown   = true;
        _isRequester        = NetworkManager.Singleton.LocalClientId == requesterId;
        _voteSelection      = 0;
        _stickNeutral       = true;
        _stickCooldown      = 0f;

        BuildVoteCanvas(sceneName, requesterId, _isRequester);
    }

    [ClientRpc]
    private void VoteResolvedClientRpc(bool accepted)
    {
        _showingCountdown = false;
        DestroyVoteUI();
        if (!accepted) ShowBriefMessage("Floor switch was declined.");
    }


    private void CheckAllVotes()
    {
        foreach (var kv in _votes)
            if (!kv.Value) { ResolveVote(false); return; }

        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            if (!_votes.ContainsKey(id)) return;

        ResolveVote(true);
    }

    private void ResolveVote(bool accepted)
    {
        if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
        _voteInProgress = false;

        VoteResolvedClientRpc(accepted);
        if (accepted)
            NetworkManager.SceneManager.LoadScene(_pendingScene, LoadSceneMode.Single);
    }

    private IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(VOTE_TIMEOUT);
        if (!_voteInProgress) yield break;
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            if (!_votes.ContainsKey(id)) _votes[id] = true;
        ResolveVote(true);
    }

    public void SubmitVote(bool accepted)
    {
        if (_hasVoted) return;
        _hasVoted = true;
        DestroyVoteUI();
        SubmitVoteServerRpc(accepted);
    }

    private void HandleVoteNavigation()
    {
        float axis    = Input.GetAxis("Vertical");
        bool  upDown  = Input.GetKeyDown(KeyCode.UpArrow);
        bool  downDown = Input.GetKeyDown(KeyCode.DownArrow);

        if (Mathf.Abs(axis) < STICK_THRESHOLD)
            _stickNeutral = true;

        if (_stickCooldown > 0f) return;

        if (upDown || downDown)
        {
            _voteSelection = upDown
                ? (_voteSelection - 1 + VOTE_OPTION_COUNT) % VOTE_OPTION_COUNT
                : (_voteSelection + 1) % VOTE_OPTION_COUNT;
            _stickCooldown = STICK_COOLDOWN;
            UpdateVoteHighlight();
            return;
        }

        if (!_stickNeutral) return;
        if (Mathf.Abs(axis) < STICK_THRESHOLD) return;

        if (axis > STICK_THRESHOLD)
            _voteSelection = (_voteSelection - 1 + VOTE_OPTION_COUNT) % VOTE_OPTION_COUNT;
        else
            _voteSelection = (_voteSelection + 1) % VOTE_OPTION_COUNT;

        _stickNeutral  = false;
        _stickCooldown = STICK_COOLDOWN;
        UpdateVoteHighlight();
    }

    private void HandleVoteConfirm()
    {
        bool confirm =
            Input.GetKeyDown(KeyCode.JoystickButton2) ||
            Input.GetKeyDown(KeyCode.Return)          ||
            Input.GetKeyDown(KeyCode.KeypadEnter)     ||
            Input.GetKeyDown(KeyCode.Space);

        if (confirm) SubmitVote(_voteSelection == 0);
    }

    private void UpdateVoteHighlight()
    {
        SetButtonColor(_acceptBtn,  _voteSelection == 0 ? SELECTED_HIGHLIGHT : ACCEPT_COLOR);
        SetButtonColor(_declineBtn, _voteSelection == 1 ? SELECTED_HIGHLIGHT : DECLINE_COLOR);
    }

    private static void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }

    private void BuildVoteCanvas(string sceneName, ulong requesterId, bool isRequester)
    {
        DestroyVoteUI();

        Camera cam = Camera.main;
        if (cam == null) return;

        _voteUI = new GameObject("VoteUI");
        _voteUI.transform.SetParent(cam.transform, false);
        _voteUI.transform.localPosition = new Vector3(0f, 0f, 2f);
        _voteUI.transform.localRotation = Quaternion.identity;
        _voteUI.transform.localScale    = Vector3.one * 0.002f;

        var canvas = _voteUI.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        var rt = _voteUI.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, isRequester ? 200 : 340);
        _voteUI.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.93f);

        string floorLabel = SceneNameToLabel(sceneName);
        string msg = isRequester
            ? $"Waiting for other players...\nSwitching to {floorLabel}"
            : $"Player {requesterId} wants to switch\nto {floorLabel}";

        AddLabel(rt, msg, new Vector2(0f, isRequester ? 55f : 115f), 26);

        var cdGO = new GameObject("Countdown");
        cdGO.transform.SetParent(rt, false);
        var cdRT              = cdGO.AddComponent<RectTransform>();
        cdRT.sizeDelta        = new Vector2(200f, 36f);
        cdRT.anchoredPosition = new Vector2(0f, isRequester ? 5f : 55f);
        _countdownText        = cdGO.AddComponent<Text>();
        _countdownText.font      = GetFont();
        _countdownText.fontSize  = 22;
        _countdownText.alignment = TextAnchor.MiddleCenter;
        _countdownText.color     = new Color(0.9f, 0.9f, 0.3f);
        _countdownText.text      = Mathf.CeilToInt(_countdownRemaining) + "s";

        if (!isRequester)
        {
            _acceptBtn  = AddButton(rt, "Accept",  new Vector2(0f, -20f),  ACCEPT_COLOR);
            _declineBtn = AddButton(rt, "Decline", new Vector2(0f, -100f), DECLINE_COLOR);
            _acceptBtn.onClick.AddListener(() => SubmitVote(true));
            _declineBtn.onClick.AddListener(() => SubmitVote(false));
            AddLabel(rt, "Stick Up/Down to select  •  JsBtn2 / Enter to confirm",
                new Vector2(0f, -165f), 15);
            UpdateVoteHighlight();
        }

        var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            _localPlayerMovement = localPlayer.GetComponentInChildren<PlayerMovement>();
            if (_localPlayerMovement != null) _localPlayerMovement.enabled = false;
        }
        Time.timeScale = 0f;
    }

    private void DestroyVoteUI()
    {
        if (_voteUI != null) Destroy(_voteUI);
        _voteUI        = null;
        _countdownText = null;
        _acceptBtn     = null;
        _declineBtn    = null;

        if (_localPlayerMovement != null) _localPlayerMovement.enabled = true;
        _localPlayerMovement = null;
        Time.timeScale = 1f;
    }

    private void ShowBriefMessage(string message)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        var go = new GameObject("BriefMsg");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0.25f, 2f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * 0.002f;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(440f, 80f);
        go.AddComponent<Image>().color = new Color(0.45f, 0.08f, 0.08f, 0.88f);

        AddLabel(rt, message, Vector2.zero, 24);
        Destroy(go, 3f);
    }

    private static string SceneNameToLabel(string s) => s switch
    {
        "FirstFloor"  => "Floor 1",
        "SecondFloor" => "Floor 2",
        "Future"      => "Floor 3",
        _             => s
    };

    private static Font _font;
    private static Font GetFont()
    {
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _font;
    }

    private static void AddLabel(RectTransform parent, string text, Vector2 pos, int size,
        FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(480f, 80f);
        rt.anchoredPosition = pos;
        var t           = go.AddComponent<Text>();
        t.font          = GetFont();
        t.text          = text;
        t.fontSize      = size;
        t.fontStyle     = style;
        t.alignment     = TextAnchor.MiddleCenter;
        t.color         = Color.white;
    }

    private static Button AddButton(RectTransform parent, string label, Vector2 pos, Color color)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(190f, 70f);
        rt.anchoredPosition = pos;
        var img       = go.AddComponent<Image>();
        img.color     = color;
        var btn       = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var tGO = new GameObject("Text");
        tGO.transform.SetParent(go.transform, false);
        var tRT              = tGO.AddComponent<RectTransform>();
        tRT.sizeDelta        = rt.sizeDelta;
        tRT.anchoredPosition = Vector2.zero;
        var txt          = tGO.AddComponent<Text>();
        txt.font         = GetFont();
        txt.text         = label;
        txt.fontSize     = 28;
        txt.alignment    = TextAnchor.MiddleCenter;
        txt.color        = Color.white;

        return btn;
    }
}
