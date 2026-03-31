using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class SettingsMenuManager : MonoBehaviour
{
    public static SettingsMenuManager instance;

    [Header("Settings Menu UI")]
    public Canvas settingsMenuCanvas;
    public Button[] menuButtons;

    [Header("Menu Placement")]
    public float menuDistance = 2f;
    public float menuHeightOffset = 0f;

    [Header("References")]
    public PlayerMovement playerMovement;
    public RaycastPointer raycastPointer;
    public ObjectMenuManager objectMenuManager;
    public InventoryManager inventoryManager;

    private float[] raycastLengths = { 1f, 10f, 50f };
    private int raycastLengthIndex = 1;

    private string[] speedLabels = { "High", "Medium", "Low" };
    private float[] speedValues = { 20f, 10f, 5f };
    private int speedIndex = 1;

    public bool IsMenuOpen() => menuOpen;

    private bool menuOpen = false;
    private int selectedIndex = 0;

    private bool bWasPressed = false;
    private float joystickCooldown = 0f;
    private const float JoystickDelay = 0.3f;

    private Color normalColor = Color.black;
    private Color highlightColor = Color.yellow;
    void Awake()
    {
        instance = this;
        if (settingsMenuCanvas != null)
            settingsMenuCanvas.gameObject.SetActive(false);
    }

    void Start()
    {
        ApplyRaycastLength();
        ApplySpeed();
        UpdateButtonLabels();
    }

    void Update()
    {
        bool okPressed = Input.GetButton("js11");
        if (okPressed)
        {
            if (!menuOpen)
            {
                OpenMenu();
            }
        }

        if (!menuOpen)
        {
            return;
        }

        joystickCooldown -= Time.deltaTime;
        float v = Input.GetAxis("Vertical");

        if (joystickCooldown <= 0f && Mathf.Abs(v) > 0.5f)
        {
            joystickCooldown = JoystickDelay;

            if (v > 0)
            {
                selectedIndex = Mathf.Max(0, selectedIndex - 1);
            }
            else{
                selectedIndex = Mathf.Min(menuButtons.Length - 1, selectedIndex + 1);
                }
                HighlightSelected();
            }

        bool bPressed = Input.GetButton("js1");
        bool bDown = bPressed && !bWasPressed;

        if (bDown)
        {
            SelectCurrentItem();
        }
        bWasPressed = bPressed;
    }


    void HighlightSelected()
    {
        ResetHighlights();

        if (menuButtons == null || menuButtons.Length == 0) {
            return;
        }
        if (selectedIndex < 0 || selectedIndex >= menuButtons.Length) {
            return;
        }

        Button btn = menuButtons[selectedIndex];

        Image img = btn.GetComponent<Image>();
        if (img != null) {
            img.color = highlightColor;
        }

        Text txt = btn.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.color = Color.white;
        }
    }

    void ResetHighlights()
    {
        if (menuButtons == null) return;
        foreach (var btn in menuButtons)
        {
            if (btn == null) { continue; }
            Image img = btn.GetComponent<Image>();
            if (img != null){
                img.color = normalColor;
            }
            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null){
                txt.color = Color.white;
        }
        }
    }

    void OpenMenu()
    {
        menuOpen = true;
        selectedIndex = 0;

        if (objectMenuManager != null && objectMenuManager.IsAnyMenuOpen())
        {
            objectMenuManager.CloseMenu();
        }

        if (playerMovement != null) {
            playerMovement.SetMovementEnabled(false);
        }
        if (raycastPointer != null) {
            raycastPointer.IsEnabled = false;
        }
        Camera cam = raycastPointer.gazeCamera;
        Vector3 flatForward = cam.transform.forward;
        flatForward.y = 0f;
        
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }
        flatForward.Normalize();

        Vector3 menuPos = cam.transform.position + flatForward * menuDistance + Vector3.up * menuHeightOffset;

        settingsMenuCanvas.transform.position = menuPos;

        settingsMenuCanvas.transform.LookAt(cam.transform.position);
        settingsMenuCanvas.transform.Rotate(0f, 180f, 0f);
        settingsMenuCanvas.gameObject.SetActive(true);
        UpdateButtonLabels();
        HighlightSelected();
    }

    void CloseMenu()
    {
        menuOpen = false;
        settingsMenuCanvas.gameObject.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }
        if (raycastPointer != null){

            raycastPointer.IsEnabled = true;
        }

        ResetHighlights();
    }

    void SelectCurrentItem()
    {
        switch (selectedIndex){
            case 0: DoResume(); break;
            case 1: DoRaycastLength(); break;
            case 2: DoInventory(); break;
            case 3: DoSpeed(); break;
            case 4: DoQuit(); break;
        }
    }
    void DoResume()
    {
        CloseMenu();
    }

    void DoRaycastLength()
    {
        raycastLengthIndex = (raycastLengthIndex + 1) % raycastLengths.Length;
        ApplyRaycastLength();
        UpdateButtonLabels();
        HighlightSelected();
    }

    void DoInventory()
    {
        CloseMenu();
        if (inventoryManager != null)
        {
            inventoryManager.ShowInventoryPanel();
        }
    }

    void DoSpeed()
    {
        speedIndex = (speedIndex + 1) % speedLabels.Length;
        ApplySpeed();
        UpdateButtonLabels();
        HighlightSelected();
    }

    void DoQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void ApplyRaycastLength()
    {
        if (raycastPointer != null)
        {
            raycastPointer.raycastLength = raycastLengths[raycastLengthIndex];
        }
    }

    void ApplySpeed()
    {
        if (playerMovement != null) { 
            playerMovement.SetSpeed(speedValues[speedIndex]);
        }
    }

    void UpdateButtonLabels()
    {
        if (menuButtons == null) {
            return; 
        }

        if (menuButtons.Length > 1 && menuButtons[1] != null){

            Text t = menuButtons[1].GetComponentInChildren<Text>();
            if (t != null){
                t.text = "Raycast Length: " + raycastLengths[raycastLengthIndex] + "m";
            }
        }

        if (menuButtons.Length > 3 && menuButtons[3] != null){
            Text t = menuButtons[3].GetComponentInChildren<Text>();
            if (t != null){
                t.text = "Speed: " + speedLabels[speedIndex];
            }
        }
    }
}