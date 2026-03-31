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

    private LineRenderer lineRenderer;
    private Transform currentHighlight;      

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
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = 0.02f;
        lineRenderer.useWorldSpace = true;
    }

    void Update()
    {
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

        HandleHighlight(didHit, hit);

        bool xPressed = Input.GetButton("js2");
        if (xPressed && !xWasPressed){
            if (currentHighlight != null && didHit && hit.distance <= raycastLength)
            {
                ObjectMenuTrigger trigger = currentHighlight.GetComponent<ObjectMenuTrigger>();
                if (trigger != null) trigger.TryOpenMenu();
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