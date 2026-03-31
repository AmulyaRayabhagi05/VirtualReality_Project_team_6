using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectMenuManager : MonoBehaviour
{
    public static ObjectMenuManager instance;

    [Header("Object Menu UI")]
    public Canvas objectMenuCanvas;
    public Button destroyButton;
    public Button storeButton;
    public Button exitButton;

    [Header("References")]
    public Camera gazeCamera;
    public PlayerMovement playerMovement;
    public InventoryManager inventoryManager;

    [Header("Menu placement")]
    public float menuDistance = 1.5f;

    private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color highlightColor = Color.yellow;

    private GameObject currentTarget;
    private bool menuOpen = false;
    private Button hoveredButton = null;
    private bool bWasPressed = false;

    public static Stack<DestroyedObjectInfo> destroyedStack = new Stack<DestroyedObjectInfo>();

    public static DestroyedObjectInfo lastDestroyedInfo
    {
        get => destroyedStack.Count > 0 ? destroyedStack.Peek() : null;
        set
        {
            if (value == null)
            {
                destroyedStack.Clear();
            }else{
                destroyedStack.Push(value);
            }
        }
    }

    public static bool HasDestroyedObjects => destroyedStack.Count > 0;

    public class DestroyedObjectInfo
    {
        public PrimitiveType primitiveType;
        public Vector3 scale;
        public Color color;
        public string tag;
        public bool hasTranslate;
        public bool hasRotate;
    }

    public bool IsAnyMenuOpen() => menuOpen;

    void Awake()
    {
        instance = this;
        if (objectMenuCanvas != null)
        {
            objectMenuCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!menuOpen)
        {
            return;
        }

        if (currentTarget != null)
        {
            Vector3 toCamera = (gazeCamera.transform.position - currentTarget.transform.position).normalized;
            Vector3 menuPos = currentTarget.transform.position + toCamera * menuDistance;
            menuPos.y = currentTarget.transform.position.y + 0.5f;

            objectMenuCanvas.transform.position = menuPos;
            objectMenuCanvas.transform.LookAt(gazeCamera.transform.position);
            objectMenuCanvas.transform.Rotate(0, 180f, 0);
        }else{
            CloseMenu();
            return;
        }

        DetectHoveredButton();

        bool bPressed = Input.GetButton("js1");
        bool bDown = bPressed && !bWasPressed;

        if (bDown)
        {
            if (hoveredButton == destroyButton)
            {
                DoDestroy();
            }
            else if (hoveredButton == storeButton){
                DoStore();
            }
            else if (hoveredButton == exitButton) {
                DoExit();
            }
        }
        bWasPressed = bPressed;
    }

    void DetectHoveredButton()
    {
        ResetButtonColors();
        hoveredButton = null;

        Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);

        Button closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Button btn in new Button[] { destroyButton, storeButton, exitButton })
        {
            if (btn == null)
            {
                continue;
            }

            RectTransform rt = btn.GetComponent<RectTransform>();
            
            if (rt == null) 
            { 
                continue; 
            }

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Plane plane = new Plane(objectMenuCanvas.transform.forward, corners[0]);

            float dist;
            if (plane.Raycast(ray, out dist) && dist < closestDist) {
                Vector3 hitPoint = ray.GetPoint(dist);
                if (PointInRect(hitPoint, corners)) {
                    closestDist = dist;
                    closest = btn;
                }
            }
        }

        if (closest != null)
        {
            hoveredButton = closest;
            SetButtonColor(closest, highlightColor);
        }
    }

    bool PointInRect(Vector3 point, Vector3[] corners)
    {
        Vector3 right = corners[3] - corners[0];
        Vector3 up = corners[1] - corners[0];
        Vector3 local = point - corners[0];

        float u = Vector3.Dot(local, right) / right.sqrMagnitude;
        float v = Vector3.Dot(local, up) / up.sqrMagnitude;

        return (u >= 0f && u <= 1f && v >= 0f && v <= 1f);
    }

    void ResetButtonColors()
    {
        SetButtonColor(destroyButton, normalColor);
        SetButtonColor(storeButton, normalColor);
        SetButtonColor(exitButton, normalColor);
    }

    void SetButtonColor(Button btn, Color c)
    {
        if (btn == null)
        {
            return;
        }

        Image img = btn.GetComponent<Image>();

        if (img != null)
        {
            img.color = c;
        }

        Text txt = btn.GetComponentInChildren<Text>();

        if (txt != null) {
            txt.color = (c == highlightColor) ? Color.black : Color.white;
        }
    }

    public void TryOpenMenu(GameObject target)
    {
        if (menuOpen)
        {
            CloseMenu();
        }

        currentTarget = target;
        menuOpen = true;

        objectMenuCanvas.gameObject.SetActive(true);

        if (playerMovement != null) {
            playerMovement.SetMovementEnabled(false);
        }
    }


    void DoDestroy()
    {
        if (currentTarget == null)
        {
            return;
        }

        lastDestroyedInfo = ExtractInfo(currentTarget);
        Destroy(currentTarget);
        currentTarget = null;
        CloseMenu();
    }

    void DoStore()
    {
        if (currentTarget == null) {
            return;
        }

        if (inventoryManager != null)
        {
            bool stored = inventoryManager.StoreObject(currentTarget);
            if (!stored)
            {
                return;
            }
        }

        currentTarget = null;
        CloseMenu();
    }

    void DoExit()
    {
        CloseMenu();
    }

    public void CloseMenu()
    {
        menuOpen = false;
        currentTarget = null;

        if (objectMenuCanvas != null) { 
            objectMenuCanvas.gameObject.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }

        ResetButtonColors();
        hoveredButton = null;
        bWasPressed = false;
    }
    public static DestroyedObjectInfo ExtractInfo(GameObject obj)
    {
        var info = new DestroyedObjectInfo();
        info.scale = obj.transform.localScale;
        info.tag = obj.tag;
        info.hasTranslate = obj.GetComponent<Translate>() != null;
        info.hasRotate = obj.GetComponent<Rotate>() != null;

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf != null)
        {
            string mn = mf.sharedMesh.name.ToLower();
            info.primitiveType = mn.Contains("sphere") ? PrimitiveType.Sphere : PrimitiveType.Cube;
        }
        else
        {
            info.primitiveType = PrimitiveType.Cube;
        }

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            info.color = rend.material.color;
        }

        return info;
    }

    public static void SpawnStoredInfo(DestroyedObjectInfo info, Vector3 groundPoint)
    {
        float halfH = info.scale.y / 2f;
        Vector3 spawnPos = groundPoint + new Vector3(0, halfH, 0);

        GameObject newObj = GameObject.CreatePrimitive(info.primitiveType);
        newObj.transform.position = spawnPos;
        newObj.transform.localScale = info.scale;

        if (!string.IsNullOrEmpty(info.tag))
        {
            newObj.tag = info.tag;
        }

        Renderer rend = newObj.GetComponent<Renderer>();
        
        if (rend != null)
        {
            rend.material.color = info.color;
        }

        Rigidbody rb = newObj.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (info.hasTranslate) {
            newObj.AddComponent<Translate>();
        }
        
        if (info.hasRotate)
        {
            newObj.AddComponent<Rotate>();
        }

        newObj.AddComponent<ObjectMenuTrigger>();

        Outline outline = newObj.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = Color.white;
        outline.OutlineWidth = 5f;
        outline.enabled = false;
    }
}