using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 3;
    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;
    private float _openCooldown = 0f;

    private static readonly Color SELECTED_COLOR = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private CanvasGroup menuPanelGroup;
    private Canvas menuCanvas;
    private GameObject currentGazedButton = null;

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
        if (menuPanelGroup.alpha > 0f)
        {
            _openCooldown -= Time.unscaledDeltaTime;
            _cooldownTimer -= Time.unscaledDeltaTime;

            HandleNavigation();

            if (_openCooldown <= 0f)
            {
                HandleConfirm();
                HandleGazeAndController();
            }
        }
    }

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
        if (!Input.GetKeyDown(KeyCode.JoystickButton2)) return;

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

    void HandleGazeAndController()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width / 2f, Screen.height / 2f)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            Debug.Log("Gaze hit:" + r.gameObject.name);
        }

        if (results.Count > 0)
        {
            GameObject gazedButton = null;
            foreach (var r in results)
            {
                Button btn = r.gameObject.GetComponent<Button>();
                if (btn != null)
                {
                    gazedButton = r.gameObject;
                    break;
                }
            }

            if (gazedButton != null)
            {
                currentGazedButton = gazedButton;
                Debug.Log("looking at button:" + gazedButton.name);

                if (Input.GetButtonDown("js1"))
                {

                    if (gazedButton.name == "CloseMenu")
                    {
                        HideMenu();
                    }
                    else if (gazedButton.name == "Outside")
                    {
                        GoOutside();
                    }
                    else if (gazedButton.name == "StartSim")
                    {
                        StartSimulation();
                    }
                    Button btn = gazedButton.GetComponent<Button>();
                    if (btn != null) btn.onClick.Invoke();
                }
            }
            else
            {
                currentGazedButton = null;
            }
        }
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

    private MonoBehaviour cachedMovementScript;

    public void SetMovementScript(MonoBehaviour script)
    {
        cachedMovementScript = script;
    }

    public void GoOutside()
    {
        MonoBehaviour scriptToEnable = movementScript != null ? movementScript : cachedMovementScript;

        if (xrCardboardRig != null && outsideDestination != null)
        {
            xrCardboardRig.transform.position = outsideDestination.position;

            if (scriptToEnable != null)
            {
                scriptToEnable.enabled = true;
                Debug.Log("Movement re-enabled");
            }
            else
            {
                Debug.LogError("No movement script found");
            }
        }
        else
        {
            Debug.LogError("xrCardboardRig or outsideDestination is nul");
        }

        HideMenu();
    }

    public void StartSimulation()
    {
        Debug.Log("Starting Simulation");
        SceneManager.LoadScene("Flight");
    }
}