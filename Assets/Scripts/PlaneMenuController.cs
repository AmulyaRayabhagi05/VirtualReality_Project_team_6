using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlaneMenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuPanel;
    public Button outsideButton;
    public Button startSimButton;
    public Button closeMenuButton;

    [Header("Player")]
    public GameObject xrCardboardRig;
    public Transform outsideDestination;
    public MonoBehaviour movementScript;

    [Header("Menu Position")]
    public float menuDistance = 2f;
    private CanvasGroup menuPanelGroup;
    private Canvas menuCanvas;
    private GameObject currentGazedButton = null;

    void Start()
    {
        menuCanvas = menuPanel.GetComponentInParent<Canvas>();

        menuPanelGroup = menuPanel.GetComponent<CanvasGroup>();
        if (menuPanelGroup == null){
            menuPanelGroup = menuPanel.AddComponent<CanvasGroup>();
	}

        HideMenu();

        if (outsideButton) {
		outsideButton.onClick.AddListener(GoOutside);
        }
	if (startSimButton) {
		startSimButton.onClick.AddListener(StartSimulation);
        }
	if (closeMenuButton){
		closeMenuButton.onClick.AddListener(HideMenu);
   	} 
   }

    void Update()
    {
        if (menuPanelGroup.alpha > 0f)
        {
            HandleGazeAndController();
        }
    }

    void LockMenuToCamera()
    {
        Transform cam = Camera.main.transform;

        menuCanvas.transform.position = cam.position + cam.forward * menuDistance;

        menuCanvas.transform.rotation = Quaternion.LookRotation(
            menuCanvas.transform.position -cam.position
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

            if (gazedButton != null){
                currentGazedButton = gazedButton;
                Debug.Log("looking at button:" +gazedButton.name);

                if (Input.GetButtonDown("js1"))
                {
                    Debug.Log("js1 pressed on button:"+gazedButton.name);

                    if (gazedButton.name == "CloseMenu") {
			HideMenu();
                    }else if (gazedButton.name == "Outside"){
			GoOutside();
                    }else if (gazedButton.name == "StartSim"){ 
			StartSimulation();
		    }
                    Button btn = gazedButton.GetComponent<Button>();
                    if (btn != null) btn.onClick.Invoke();
                }
            }else{
                currentGazedButton = null;
            }
        }
    }

    public void ShowMenu()
    {
        if (movementScript != null){
            movementScript.enabled = false;
	}
        LockMenuToCamera();

        menuPanelGroup.alpha = 1f;
        menuPanelGroup.interactable = true;
        menuPanelGroup.blocksRaycasts = true;
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void HideMenu()
    {
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
            }else
            {
                Debug.LogError("No movement script found");
            }
        }else{
            Debug.LogError("xrCardboardRig or outsideDestination is nul");
        }

        HideMenu();
    }

    public void StartSimulation()
    {
        Debug.Log("Starting Simulation");
        EventSystem currentES = EventSystem.current;
        if (currentES != null) {
		currentES.gameObject.SetActive(false);
        }
	SceneManager.LoadScene("Flight", LoadSceneMode.Additive);
    }
}