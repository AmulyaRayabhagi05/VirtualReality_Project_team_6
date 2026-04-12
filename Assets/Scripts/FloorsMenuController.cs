using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FloorsMenuController : MonoBehaviour
{
    [Header("VR Canvas Setup")]
    public Canvas menuCanvas;

    [Tooltip("Offset from the camera when the menu spawns (metres).")]
    public Vector3 spawnOffset = new Vector3(0f, 0f, 1.5f);

    [Tooltip("Lock the menu Y rotation to face the player?")]
    public bool facePlayer = true;

    [Header("Buttons")]
    public Button floorOneButton;
    public Button floorTwoButton;
    public Button closeButton;

    [Header("Player")]
    [Tooltip("Assign the CharacterMovement component on your player.")]
    public CharacterMovement characterMovement;

    [Header("Joystick Settings")]
    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;

    // Options: 0 = Floor One, 1 = Floor Two, 2 = Close
    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 3;

    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;

    private static readonly Color SELECTED_COLOR   = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private Camera _vrCamera;

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------

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
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public void OpenMenu()
    {
        // Don't open if the pause menu is already open
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
        SceneManager.LoadScene("FirstFloor");
    }

    public void LoadFloorTwo()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SecondFloor");
    }

    // -----------------------------------------------------------------------
    // Private - joystick navigation
    // -----------------------------------------------------------------------

    private void HandleNavigation()
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

    private void HandleConfirm()
    {
        if (!Input.GetKeyDown(KeyCode.JoystickButton5)) return;

        switch (_selectedIndex)
        {
            case 0: LoadFloorOne(); break;
            case 1: LoadFloorTwo(); break;
            case 2: CloseMenu();    break;
        }
    }

    private void UpdateHighlight()
    {
        SetButtonColor(floorOneButton, _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(floorTwoButton, _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(closeButton,    _selectedIndex == 2 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }

    // -----------------------------------------------------------------------
    // Private - canvas positioning
    // -----------------------------------------------------------------------

    private void PositionMenuInFrontOfPlayer()
    {
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