using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class PlaneMenuController : MonoBehaviour
{
    public GameObject menuPanel;
    public Button outsideButton;
    public Button startSimButton;
    public Button closeMenuButton;
    public GameObject xrCardboardRig;
    public Transform outsideDestination;
    public MonoBehaviour movementScript;
    public float menuDistance = 2f;

    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;
    public static bool IsJustOpened { get; private set; }
    public bool IsMenuOpen => menuPanelGroup != null && menuPanelGroup.alpha > 0f;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 3;
    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;
    private float _openCooldown = 0f;
    private float _enteredPlaneCooldown = 0f;
    private bool _cWasDown = false;

    private static readonly Color SELECTED_COLOR = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private CanvasGroup menuPanelGroup;
    private Canvas menuCanvas;

    void Start()
    {
        menuCanvas = menuPanel.GetComponentInParent<Canvas>();

        menuPanelGroup = menuPanel.GetComponent<CanvasGroup>();
        if (menuPanelGroup == null)
        {
            menuPanelGroup = menuPanel.AddComponent<CanvasGroup>();
        }

        HideMenu();

        if (outsideButton)
        {
            outsideButton.onClick.AddListener(GoOutside);
        }
        if (startSimButton)
        {
            startSimButton.onClick.AddListener(StartSimulation);
        }
        if (closeMenuButton)
        {
            closeMenuButton.onClick.AddListener(HideMenu);
        }
    }

    void Update()
    {
        // PC: when you're already inside the plane, allow opening the cockpit menu with C.
        // (_frozenPlayer is set by TeleportCube.NotifyEnteredPlane)
        if (menuPanelGroup.alpha <= 0f)
        {
            _enteredPlaneCooldown -= Time.unscaledDeltaTime;

            bool cDown = Input.GetKey(KeyCode.C);
            bool cPressedEdge = cDown && !_cWasDown;
            _cWasDown = cDown;

            // Prevent immediate open on the same keypress used to enter the plane.
            if (_frozenPlayer != null && _enteredPlaneCooldown <= 0f && cPressedEdge)
            {
                ShowMenu();
            }
            return;
        }

        if (menuPanelGroup.alpha > 0f)
        {
            _openCooldown -= Time.unscaledDeltaTime;
            _cooldownTimer -= Time.unscaledDeltaTime;

            HandleNavigation();

            if (_openCooldown <= 0f)
            {
                HandleConfirm();
            }
        }
    }

    private void HandleNavigation()
    {
        // Some Android controllers report D-pad / stick on different axes.
        // Use the dominant axis magnitude so up/down works across mappings.
        float vAxis = Input.GetAxisRaw("Vertical");
        float hAxis = Input.GetAxisRaw("Horizontal");
        float axis = Mathf.Abs(vAxis) >= Mathf.Abs(hAxis) ? vAxis : hAxis;
        bool upDown = Input.GetKeyDown(KeyCode.UpArrow);
        bool downDown = Input.GetKeyDown(KeyCode.DownArrow);

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _stickNeutral = true;
            // Still allow keyboard nav when stick is idle.
        }

        if (_cooldownTimer > 0f) return;

        // Keyboard navigation (one step per key press).
        if (upDown || downDown)
        {
            _selectedIndex = upDown
                ? (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT
                : (_selectedIndex + 1) % OPTION_COUNT;

            _cooldownTimer = navigationCooldown;
            UpdateHighlight();
            return;
        }

        // Stick navigation (edge-triggered).
        if (!_stickNeutral) return;
        if (Mathf.Abs(axis) < stickThreshold) return;

        if (axis > stickThreshold)
            _selectedIndex = (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT;
        else
            _selectedIndex = (_selectedIndex + 1) % OPTION_COUNT;

        _stickNeutral = false;
        _cooldownTimer = navigationCooldown;
        UpdateHighlight();
    }

    private void HandleConfirm()
    {
        bool confirm =
            Input.GetKeyDown(KeyCode.JoystickButton2) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideMenu();
            return;
        }

        if (!confirm) return;

        switch (_selectedIndex)
        {
            case 0: GoOutside(); break;
            case 1: StartSimulation(); break;
            case 2: HideMenu(); break;
        }
    }

    private void UpdateHighlight()
    {
        SetButtonColor(outsideButton, _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(startSimButton, _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(closeMenuButton, _selectedIndex == 2 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }

    void LockMenuToCamera()
    {
        Transform cam = Camera.main.transform;

        menuCanvas.transform.position = cam.position + cam.forward * menuDistance;

        menuCanvas.transform.rotation = Quaternion.LookRotation(
            menuCanvas.transform.position - cam.position
        );
    }

    public void ShowMenu()
    {
        if (movementScript != null)
        {
            movementScript.enabled = false;
        }
        LockMenuToCamera();

        menuPanelGroup.alpha = 1f;
        menuPanelGroup.interactable = true;
        menuPanelGroup.blocksRaycasts = true;
        EventSystem.current.SetSelectedGameObject(null);

        _selectedIndex = 0;
        _stickNeutral = true;
        _cooldownTimer = 0f;
        _openCooldown = 0.3f;
        IsJustOpened = true;
        Invoke(nameof(ClearJustOpened), 0.3f);
        UpdateHighlight();
    }

    private void ClearJustOpened()
    {
        IsJustOpened = false;
    }


    public void HideMenu()
    {
        _stickNeutral = true;
        _cooldownTimer = 0f;
        menuPanelGroup.alpha = 0f;
        menuPanelGroup.interactable = false;
        menuPanelGroup.blocksRaycasts = false;
    }

    private GameObject _frozenPlayer;
    private MonoBehaviour cachedMovementScript;

    public void SetMovementScript(MonoBehaviour script)
    {
        cachedMovementScript = script;
    }

    // Called by TeleportCube when the player enters the plane.
    public void NotifyEnteredPlane(GameObject player)
    {
        _frozenPlayer = player;
        _enteredPlaneCooldown = 0.35f;
    }

    public void GoOutside()
    {
        GameObject player = _frozenPlayer ?? TeleportCube.GetLocalPlayerObject() ?? xrCardboardRig;

        if (player != null && outsideDestination != null)
        {
            player.transform.position = outsideDestination.position;

            var cc = player.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = true;

            var pm = player.GetComponentInChildren<PlayerMovement>();
            if (pm != null)
                pm.enabled = true;
            else
            {
                MonoBehaviour scriptToEnable = movementScript != null ? movementScript : cachedMovementScript;
                if (scriptToEnable != null) scriptToEnable.enabled = true;
            }
        }

        _frozenPlayer = null;
        HideMenu();
    }

    public void StartSimulation()
    {
        HideMenu();
        if (SceneChangeVoteManager.Instance != null)
            SceneChangeVoteManager.Instance.RequestSceneChange("Flight");
        else
            NetworkSceneLoader.Load("Flight");
    }
}
