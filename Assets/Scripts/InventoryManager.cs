using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    public bool IsPanelVisible() => panelVisible;

    public const int MaxSlots = 3;

    [Header("Inventory UI")]
    public Canvas inventoryPanelCanvas;
    public RawImage[] slots;
    public Text fullMessage;

    [Header("Thumbnail")]
    public Texture2D[] thumbnails;

    [Header("References")]
    public Camera gazeCamera;
    public PlayerMovement playerMovement;
    public ObjectMenuManager objectMenuManager;
    public LayerMask floorLayer;

    private List<StoredItem> inventory = new List<StoredItem>();

    private int selectedSlotIndex = 0;
    private bool panelVisible = false;

    private GameObject carriedObject = null;
    private StoredItem carriedItem = null;

    private bool bWasPressed = false;
    private float joystickCooldown = 0f;
    private const float JoystickDelay = 0.3f;

    public class StoredItem
    {
        public ObjectMenuManager.DestroyedObjectInfo info;
        public Texture2D thumbnail;
    }

    void Awake()
    {
        instance = this;
        if (inventoryPanelCanvas != null)
        {
            inventoryPanelCanvas.gameObject.SetActive(false);
        }

        if (fullMessage != null) {
            fullMessage.gameObject.SetActive(false);
        }
        UpdateSlotUI();
    }

    public bool StoreObject(GameObject obj)
    {
        if (inventory.Count >= MaxSlots)
        {
            StartCoroutine(ShowFullMessage());
            return false;
        }

        var info = ObjectMenuManager.ExtractInfo(obj);
        var thumb = FindThumbnailForTag(obj.tag);

        inventory.Add(new StoredItem { info = info, thumbnail = thumb });
        Destroy(obj);
        UpdateSlotUI();
        return true;
    }

    Texture2D FindThumbnailForTag(string tag)
    {
        if (thumbnails == null || thumbnails.Length < 2)
        {
            return null;
        }
        if (tag.StartsWith("Cube")) {
            return thumbnails[0];
        }   
        if (tag.StartsWith("Sphere")){
            return thumbnails[1];
        }
        return null;
    }

    IEnumerator ShowFullMessage()
    {
        if (fullMessage != null)  {
            fullMessage.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            fullMessage.gameObject.SetActive(false);
        }
    }

    public void ShowInventoryPanel()
    {
        panelVisible = true;
        selectedSlotIndex = 0;
        bWasPressed = true;

        if (inventoryPanelCanvas != null)
        {
            inventoryPanelCanvas.gameObject.SetActive(true);
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(false);
        }

        if (RaycastPointer.instance != null) {
            RaycastPointer.instance.IsEnabled = false;
        }
        else { 
            Debug.LogError("RaycastPointer.instance is NULL");
        }

        Camera cam = gazeCamera;

        Vector3 flatForward = cam.transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f){
            flatForward = Vector3.forward;
        }
        flatForward.Normalize();

        Vector3 menuPos = cam.transform.position + flatForward * 0.5f + Vector3.up * -0.5f;

        inventoryPanelCanvas.transform.position = menuPos;
        inventoryPanelCanvas.transform.LookAt(cam.transform.position);
        inventoryPanelCanvas.transform.Rotate(0f, 180f, 0f);

        UpdateSlotUI();
        HighlightSelectedSlot();
    }

    public void HideInventoryPanel()
    {
        panelVisible = false;
        if (inventoryPanelCanvas != null)
        {
            inventoryPanelCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (carriedObject != null)
        {
            carriedObject.transform.position = gazeCamera.transform.position + gazeCamera.transform.forward * 1.5f;
            carriedObject.transform.rotation = gazeCamera.transform.rotation;
        }

        if (!panelVisible) return;

        joystickCooldown -= Time.deltaTime;
        float v = Input.GetAxis("Vertical");
        if (joystickCooldown <= 0f && Mathf.Abs(v) > 0.5f){

            joystickCooldown = JoystickDelay;
            selectedSlotIndex = v > 0 ? Mathf.Max(0, selectedSlotIndex - 1) : Mathf.Min(slots.Length - 1, selectedSlotIndex + 1);
            HighlightSelectedSlot();
        }

        bool bPressed = Input.GetButton("js1");

        if (bPressed && !bWasPressed)   {GrabSelectedItem();}

        bWasPressed = bPressed;
    }

    void GrabSelectedItem()
    {
        if (inventory.Count == 0) {
            return;
        }

        if (selectedSlotIndex < 0 || selectedSlotIndex >= inventory.Count)
        {
            return;
        }

        carriedItem = inventory[selectedSlotIndex];
        inventory.RemoveAt(selectedSlotIndex);
        selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, Mathf.Max(0, inventory.Count - 1));

        carriedObject = GameObject.CreatePrimitive(carriedItem.info.primitiveType);
        carriedObject.transform.localScale = carriedItem.info.scale * 0.3f;

        Renderer rend = carriedObject.GetComponent<Renderer>();
        if (rend != null) {
            rend.material.color = carriedItem.info.color;
        }

        Rigidbody rb = carriedObject.GetComponent<Rigidbody>();
        if (rb != null) {
            Destroy(rb);
        }
        
        Collider col = carriedObject.GetComponent<Collider>();
        if (col != null) {
            col.enabled = false;
        }

        HideInventoryPanel();

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }

        StartCoroutine(ReenableRaycast());
    }

    IEnumerator ReenableRaycast()
    {
        yield return new WaitForEndOfFrame();
        if (RaycastPointer.instance != null) { 
            RaycastPointer.instance.IsEnabled = true;
        }
    }

    public bool IsCarryingObject() => carriedObject != null;


    public void ReleaseCarriedObject(RaycastHit hit, bool didHit, LayerMask floorLayer,
                                     Vector3 rayOrigin, Vector3 rayDir, float rayLength)
    {
        if (carriedObject == null)
        {
            return;
        }

        Destroy(carriedObject);
        carriedObject = null;

        RaycastHit floorHit;
        if (Physics.Raycast(rayOrigin, rayDir, out floorHit, rayLength, floorLayer)){
            ObjectMenuManager.SpawnStoredInfo(carriedItem.info, floorHit.point);
        }else{
            ObjectMenuManager.SpawnStoredInfo(carriedItem.info,
            new Vector3(gazeCamera.transform.position.x, 0f, gazeCamera.transform.position.z));
        }   

        carriedItem = null;
        UpdateSlotUI();
    }

    void UpdateSlotUI()
{
    if (slots == null) {
        return;
    }
        for (int i = 0; i < slots.Length; i++){
            if (slots[i] == null) continue;
            slots[i].gameObject.SetActive(true);

            if (i < inventory.Count && inventory[i] != null){
                slots[i].texture = inventory[i].thumbnail != null ? (Texture)inventory[i].thumbnail : (Texture)MakeColorTex(inventory[i].info.color);
                slots[i].color = Color.white;
            }else{
                slots[i].texture = null;
                slots[i].color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }
    }

    void HighlightSelectedSlot()
    {
        if (slots == null){
            return;
        }
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null) {
                continue; 
        }
            slots[i].color = (i == selectedSlotIndex) ? Color.yellow : Color.white;
        }
    }

    Texture2D MakeColorTex(Color c)
    {
        var tex = new Texture2D(64, 64);
        var pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = c;
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public int Count => inventory.Count;
}