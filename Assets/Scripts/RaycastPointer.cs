using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
public class RaycastPointer : MonoBehaviour
{
    public static RaycastPointer instance;

    [Header("References")]
    public Camera gazeCamera;
    public LayerMask floorLayer;
    public CharacterController characterController;
    public ObjectMenuManager objectMenuManager;

    [Header("Raycast")]
    [HideInInspector] public float raycastLength = 10f;
    public bool debugReticleHits;

    private LineRenderer lineRenderer;
    private Transform currentHighlight;      
    private string lastHitName;

    private bool xWasPressed = false;
    private bool aWasPressed = false;

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) {
                return;
            }
            
            _isEnabled = value;

            if (!value && currentHighlight != null){
                var exitEvent = new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current);
                foreach (var handler in currentHighlight
                    .GetComponents<UnityEngine.EventSystems.IPointerExitHandler>())
                {
                    handler.OnPointerExit(exitEvent);
                }
                ClearHighlight();
            }
        }
    }

    void Awake()
    {
        instance = this;
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = 0.02f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;
    }

    void Start()
    {
        // Scenes are allowed to omit this manager; we’ll create/find one so `ObjectMenuTrigger` can open a UI.
        if (objectMenuManager == null)
        {
            objectMenuManager = FindObjectOfType<ObjectMenuManager>();
        }

        if (objectMenuManager == null)
        {
            objectMenuManager = gameObject.AddComponent<ObjectMenuManager>();
        }

        if (objectMenuManager != null)
        {
            if (objectMenuManager.gazeCamera == null)
            {
                objectMenuManager.gazeCamera = gazeCamera;
            }
        }
    }

    void Update()
    {
        if (gazeCamera == null || !gazeCamera.gameObject.activeInHierarchy)
            return;
        instance = this;

        Vector3 origin = gazeCamera.transform.position;
        Vector3 direction = gazeCamera.transform.forward;

        lineRenderer.enabled = _isEnabled;

        if (!_isEnabled){
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            ClearHighlight();
            return;
        }

        lineRenderer.SetPosition(0, gazeCamera.transform.position + new Vector3(0, -0.1f, 0));
        lineRenderer.SetPosition(1, gazeCamera.transform.position + gazeCamera.transform.forward * raycastLength + new Vector3(0, -0.1f, 0));


        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, direction, out hit, raycastLength);
        TraceReticleHit(didHit, hit);

        HandleHighlight(didHit, hit);

        bool xPressed = Input.GetButton("js2");
        bool zPressed = Input.GetKeyDown(KeyCode.Z);

        // Global toggle: if the stand info panel is open, allow the same open input to close it.
        if ((xPressed && !xWasPressed) || zPressed)
        {
            var standPanel = StandInfoPanelManager.instance != null
                ? StandInfoPanelManager.instance
                : FindObjectOfType<StandInfoPanelManager>();

            if (standPanel != null && standPanel.IsOpen)
            {
                standPanel.Hide();
                xWasPressed = xPressed;
                return;
            }
        }

        if ((xPressed && !xWasPressed) || zPressed){
            if (currentHighlight != null && didHit && hit.distance <= raycastLength)
            {
                var infoTrigger = currentHighlight.GetComponent<InfoStandTrigger>();
                if (infoTrigger != null)
                {
                    infoTrigger.TryOpen();
                }
                else
                {
                    ObjectMenuTrigger trigger = currentHighlight.GetComponent<ObjectMenuTrigger>();
                    if (trigger != null) trigger.TryOpenMenu();
                }
            }
        }
        xWasPressed = xPressed;

        if (objectMenuManager != null && objectMenuManager.IsAnyMenuOpen())
        {
            if (currentHighlight != null)
            {
                var exitEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                foreach (var handler in currentHighlight
                    .GetComponents<UnityEngine.EventSystems.IPointerExitHandler>()){
                    handler.OnPointerExit(exitEvent);
                }
            }
            return;
        }

        bool aPressed = Input.GetButton("js0");
        if (aPressed && !aWasPressed)
        {
            if (InventoryManager.instance != null && InventoryManager.instance.IsCarryingObject())
            {
                InventoryManager.instance.ReleaseCarriedObject(hit, didHit, floorLayer, origin, direction, raycastLength);
            }
            else
            {
                TrySpawnLastDestroyed(origin, direction);
            }
        }
        aWasPressed = aPressed;
    }

    void HandleHighlight(bool didHit, RaycastHit hit)
    {
        if (didHit && hit.distance <= raycastLength)
        {
            InfoStandTrigger infoTrigger = hit.collider.GetComponent<InfoStandTrigger>();
            if (infoTrigger == null)
            {
                infoTrigger = hit.collider.GetComponentInParent<InfoStandTrigger>();
            }

            if (infoTrigger != null)
            {
                Transform t = infoTrigger.transform;

                if (t != currentHighlight)
                {
                    ClearHighlight();
                    Outline o = t.GetComponent<Outline>();
                    if (o != null) o.enabled = true;
                    currentHighlight = t;
                }
                return;
            }

            ObjectMenuTrigger trigger = hit.collider.GetComponent<ObjectMenuTrigger>();

            if (trigger == null)
            {
                trigger = hit.collider.GetComponentInParent<ObjectMenuTrigger>();
            }

            if (trigger != null)
            {
                Transform t = trigger.transform;

                if (t != currentHighlight)
                {
                    ClearHighlight();
                    Outline o = t.GetComponent<Outline>();
                    if (o != null) o.enabled = true;
                    currentHighlight = t;
                }
                return;
            }
        }

        ClearHighlight();
    }

    void ClearHighlight()
    {
        if (currentHighlight != null){
            Outline o = currentHighlight.GetComponent<Outline>();
            if (o != null) o.enabled = false;
            currentHighlight = null;
        }
    }

    void TraceReticleHit(bool didHit, RaycastHit hit)
    {
        if (!debugReticleHits)
        {
            return;
        }

        string currentHitName = didHit && hit.collider != null ? hit.collider.name : "nothing";
        if (currentHitName == lastHitName)
        {
            return;
        }

        lastHitName = currentHitName;
        Debug.Log(
            didHit && hit.collider != null
                ? $"[RaycastPointer] Reticle hit: {currentHitName} at {hit.distance:F2}m"
                : "[RaycastPointer] Reticle hit: nothing",
            this);
    }

    void TryTeleport(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, raycastLength, floorLayer))
        {
            float h = characterController != null ? characterController.height / 2f : 1f;
            Vector3 pos = hit.point + new Vector3(0, h, 0);
            if (characterController != null)
            {
                characterController.enabled = false;
                characterController.transform.position = pos;
                characterController.enabled = true;
            }
        }
    }

    void TrySpawnLastDestroyed(Vector3 origin, Vector3 direction)
    {
        if (!ObjectMenuManager.HasDestroyedObjects)
        {
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, raycastLength, floorLayer)){
            var info = ObjectMenuManager.lastDestroyedInfo;
            ObjectMenuManager.destroyedStack.Pop();
            ObjectMenuManager.SpawnStoredInfo(info, hit.point);
        }
    }

    public Vector3 GetRayEndPoint()
    {
        return gazeCamera.transform.position + gazeCamera.transform.forward * raycastLength;
    }

    public Ray GetRay()
    {
        return new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
    }
}
