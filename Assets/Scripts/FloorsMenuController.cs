using UnityEngine;
using UnityEngine.UI;

public class FloorsMenuController : MonoBehaviour
{
    public Canvas menuCanvas;
    public Vector3 spawnOffset = new Vector3(0f, 0f, 1.5f);
    public bool facePlayer = true;
    public Button floorOneButton;
    public Button floorTwoButton;
    public Button floorThreeButton;
    public Button closeButton;

    public CharacterMovement characterMovement;

    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 4;

    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;

    private static readonly Color SELECTED_COLOR   = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private Camera _vrCamera;


    private void Awake()
    {
        _vrCamera = Camera.main;
        menuCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!menuCanvas.gameObject.activeSelf) return;

        _cooldownTimer -= Time.unscaledDeltaTime;
        HandleNavigation();
        HandleConfirm();
        HandleMouseClick();
    }


    public void OpenMenu()
    {
        if (PauseMenuController.IsPaused) return;

        PositionMenuInFrontOfPlayer();
        menuCanvas.gameObject.SetActive(true);
        if (characterMovement != null) characterMovement.enabled = false;

        _selectedIndex = 0;
        UpdateHighlight();

        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        _stickNeutral = true;
        _cooldownTimer = 0f;
        menuCanvas.gameObject.SetActive(false);
        if (characterMovement != null) characterMovement.enabled = true;
        Time.timeScale = 1f;
    }

    public void LoadFloorOne()
    {
        Time.timeScale = 1f;
        PlayerSceneHandler.RestoreMovementOnLoad = true;
        NetworkSceneLoader.Load("FirstFloor");
    }

    public void LoadFloorTwo()
    {
        Time.timeScale = 1f;
        PlayerSceneHandler.RestoreMovementOnLoad = true;
        NetworkSceneLoader.Load("SecondFloor");
    }

    public void LoadFloorThree()
    {
        Time.timeScale = 1f;
        PlayerSceneHandler.RestoreMovementOnLoad = true;
        NetworkSceneLoader.Load("Future");
    }

    private void HandleNavigation()
    {
        float axis = Input.GetAxis("Vertical");
        bool upDown = Input.GetKeyDown(KeyCode.UpArrow);
        bool downDown = Input.GetKeyDown(KeyCode.DownArrow);

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _stickNeutral = true;
            // Still allow keyboard nav when the stick is idle.
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
            CloseMenu();
            return;
        }

        if (!confirm) return;

        switch (_selectedIndex)
        {
            case 0: LoadFloorOne(); break;
            case 1: LoadFloorTwo(); break;
            case 2: LoadFloorThree(); break;
            case 3: CloseMenu();    break;
        }
    }

    private void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        // If there's no camera assigned to the world-space canvas, fall back to main camera.
        Camera cam = menuCanvas != null && menuCanvas.worldCamera != null ? menuCanvas.worldCamera : Camera.main;

        TryClickButton(floorOneButton, cam);
        TryClickButton(floorTwoButton, cam);
        TryClickButton(floorThreeButton, cam);
        TryClickButton(closeButton, cam);
    }

    private void TryClickButton(Button btn, Camera cam)
    {
        if (btn == null) return;

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;

        // For Screen Space Overlay, camera is ignored. For World Space it’s required.
        if (RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam))
        {
            btn.onClick.Invoke();
        }
    }

    private void UpdateHighlight()
    {
        SetButtonColor(floorOneButton, _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(floorTwoButton, _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(floorThreeButton, _selectedIndex == 2 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(closeButton,    _selectedIndex == 3 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }


    private void PositionMenuInFrontOfPlayer()
    {
        if (_vrCamera == null || !_vrCamera.gameObject.activeInHierarchy)
            _vrCamera = Camera.main;
        if (_vrCamera == null) return;

        Transform cam = _vrCamera.transform;
        menuCanvas.transform.position = cam.position + cam.TransformDirection(spawnOffset);

        if (facePlayer)
        {
            Vector3 lookDir = menuCanvas.transform.position - cam.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                menuCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
