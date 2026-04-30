using UnityEngine;

public class TrapDoorInteractable : MonoBehaviour
{
    [SerializeField] private TrapDoorPromptController canvas;
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private string interactButton = "js2";
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactRange = 8f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private bool debugInteract;

    private Collider[] _colliders;

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider>(true);

        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (canvas == null || canvas.IsOpen)
        {
            return;
        }

        if (sourceCamera == null || !sourceCamera.gameObject.activeInHierarchy)
        {
            sourceCamera = Camera.main;
            if (sourceCamera == null)
            {
                return;
            }
        }

        if (!Input.GetButtonDown(interactButton) && !Input.GetKeyDown(interactKey))
        {
            return;
        }

        Ray interactionRay = RaycastPointer.instance != null
            ? RaycastPointer.instance.GetRay()
            : new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        float rayLength = RaycastPointer.instance != null
            ? Mathf.Max(interactRange, RaycastPointer.instance.raycastLength)
            : interactRange;

        RaycastHit hit;
        if (!Physics.Raycast(interactionRay, out hit, rayLength, interactMask, QueryTriggerInteraction.Ignore))
        {
            LogDebug("Interact input fired but raycast hit nothing.");
            return;
        }

        if (!OwnsCollider(hit.collider))
        {
            LogDebug($"Interact input hit '{hit.collider.name}', not the trap door.");
            return;
        }

        LogDebug("Trap door hit by interaction ray. Opening prompt.");
        canvas.OpenPrompt();
    }

    private bool OwnsCollider(Collider hitCollider)
    {
        if (hitCollider == null || _colliders == null)
        {
            return false;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == hitCollider)
            {
                return true;
            }
        }

        return false;
    }

    private void LogDebug(string message)
    {
        if (!debugInteract)
        {
            return;
        }

        Debug.Log($"[TrapDoorInteractable] {message}", this);
    }
}
