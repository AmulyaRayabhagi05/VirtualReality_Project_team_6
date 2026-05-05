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
        EnsureRuntimeUI();
        if (objectMenuCanvas != null) {
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

        bool keyboardSelectDown = Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
        bool selectDown = bDown || keyboardSelectDown;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
            bWasPressed = bPressed;
            return;
        }

        if (selectDown)
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

        EnsureRuntimeUI();
        if (objectMenuCanvas == null)
        {
            Debug.LogWarning("[ObjectMenuManager] Cannot open menu: objectMenuCanvas is not set.", this);
            return;
        }

        if (gazeCamera == null)
        {
            gazeCamera = Camera.main;
        }

        currentTarget = target;
        menuOpen = true;

        objectMenuCanvas.gameObject.SetActive(true);

        if (playerMovement != null) {
            playerMovement.SetMovementEnabled(false);
        }
    }

    void EnsureRuntimeUI()
    {
        // If a scene hasn't been wired in the inspector, build a minimal world-space menu at runtime.
        if (objectMenuCanvas != null && destroyButton != null && storeButton != null && exitButton != null)
        {
            return;
        }

        if (gazeCamera == null)
        {
            gazeCamera = Camera.main;
        }

        // Canvas
        var canvasGO = new GameObject("ObjectMenuCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = gazeCamera;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(350f, 220f);
        // World-space canvases use real-world units; keep this panel reasonably sized in the scene.
        canvasGO.transform.localScale = Vector3.one * 0.0025f;

        // Panel background
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(320f, 180f);
        panelRT.anchoredPosition = Vector2.zero;

        destroyButton = destroyButton != null ? destroyButton : CreateRuntimeButton(panelGO.transform, "DestroyButton", "Destroy", new Vector2(0f, 50f));
        storeButton = storeButton != null ? storeButton : CreateRuntimeButton(panelGO.transform, "StoreButton", "Store", new Vector2(0f, 0f));
        exitButton = exitButton != null ? exitButton : CreateRuntimeButton(panelGO.transform, "ExitButton", "Exit", new Vector2(0f, -50f));

        objectMenuCanvas = canvas;
        objectMenuCanvas.gameObject.SetActive(false);
    }

    Button CreateRuntimeButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        var img = btnGO.AddComponent<Image>();
        img.color = normalColor;

        var btn = btnGO.AddComponent<Button>();
        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260f, 45f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        // Unity removed Arial.ttf as a builtin font. LegacyRuntime.ttf is the supported builtin.
        // If this ever fails (older Unity), the Text will still render with Unity's default font.
        try
        {
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            // Intentionally ignore.
        }
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        return btn;
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
