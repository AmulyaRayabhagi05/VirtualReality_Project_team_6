using System.Collections.Generic;
using UnityEngine;

public class BrushEquipController : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Transform brushTransform;
    [SerializeField] private Transform brushAnchor;
    [SerializeField] private string pickupButton = "js2";
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float reticleThresholdPixels = 120f;
    [SerializeField] private Vector3 equippedLocalPosition = new Vector3(0f, -0.12f, 0.5f);
    [SerializeField] private Vector3 equippedLocalEulerAngles = new Vector3(-40f, 120f, 15f);
    [SerializeField] private float dropDistance = 1.25f;
    [SerializeField] private float dropHeightOffset = 0.1f;
    [SerializeField] private bool startEquipped;
    [SerializeField] private bool debugReticleTargeting;

    private readonly List<GameObject> _reticleObjects = new List<GameObject>();
    private readonly List<ReticleJoystickClick> _reticleClickers = new List<ReticleJoystickClick>();
    private Renderer[] _brushRenderers;
    private Collider[] _brushColliders;
    private bool _isEquipped;
    private string _lastHitName;

    public bool IsEquipped
    {
        get { return _isEquipped; }
    }

    public Ray GetCurrentReticleRay()
    {
        return new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
    }

    private void Awake()
    {
        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        if (brushTransform != null)
        {
            _brushRenderers = brushTransform.GetComponentsInChildren<Renderer>(true);
            _brushColliders = brushTransform.GetComponentsInChildren<Collider>(true);
        }

        CacheReticleObjects();
    }

    private void Start()
    {
        if (brushTransform == null || brushAnchor == null || sourceCamera == null)
        {
            return;
        }

        if (startEquipped)
        {
            EquipBrush();
            return;
        }

        if (brushTransform.IsChildOf(brushAnchor))
        {
            DropBrush();
            return;
        }

        SetReticleState(true);
    }

    private void Update()
    {
        if (brushTransform == null || brushAnchor == null || sourceCamera == null)
        {
            return;
        }

        bool pickupPressed = Input.GetButtonDown(pickupButton) || Input.GetKeyDown(pickupKey);

        if (_isEquipped)
        {
            if (pickupPressed)
            {
                LogDebug("Pickup input detected while equipped. Dropping brush.");
                DropBrush();
            }

            return;
        }

        TraceCurrentReticleHit();

        bool isTargeted = IsBrushTargeted();
        if (pickupPressed)
        {
            LogDebug(isTargeted ? "Pickup input detected while targeting brush." : "Pickup input detected, but brush is not targeted.");
        }

        if (pickupPressed && isTargeted)
        {
            EquipBrush();
        }
    }

    private bool IsBrushTargeted()
    {
        Collider[] colliders = _brushColliders ?? brushTransform.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
        {
            LogDebug("Brush has no colliders to raycast against.");
            return false;
        }

        float maxDistance = RaycastPointer.instance != null ? RaycastPointer.instance.raycastLength : pickupRange;
        Ray ray = GetCurrentReticleRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            if (hit.collider == collider || hit.collider.transform.IsChildOf(brushTransform))
            {
                return true;
            }
        }

        return false;
    }

    private void EquipBrush()
    {
        LogDebug("Equipping brush.");
        brushTransform.SetParent(brushAnchor, false);
        brushTransform.localPosition = equippedLocalPosition;
        brushTransform.localRotation = Quaternion.Euler(equippedLocalEulerAngles);
        SetBrushCollidersEnabled(false);
        _isEquipped = true;
        SetReticleState(false);
    }

    private void DropBrush()
    {
        Transform cameraTransform = sourceCamera.transform;
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = cameraTransform.forward;
        }

        forward.Normalize();
        Vector3 dropPosition = brushTransform.position;

        if (Physics.Raycast(dropPosition + Vector3.up, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            dropPosition.y = hit.point.y + dropHeightOffset;
        }

        brushTransform.SetParent(null, true);
        brushTransform.position = dropPosition;
        brushTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        SetBrushCollidersEnabled(true);
        _isEquipped = false;
        _lastHitName = null;
        SetReticleState(true);
    }

    private void TraceCurrentReticleHit()
    {
        float maxDistance = RaycastPointer.instance != null ? RaycastPointer.instance.raycastLength : pickupRange;
        Ray ray = GetCurrentReticleRay();
        string currentHitName = "nothing";
        float currentHitDistance = -1f;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            currentHitName = hit.collider != null ? hit.collider.name : "unnamed collider";
            currentHitDistance = hit.distance;
        }

        if (currentHitName == _lastHitName)
        {
            return;
        }

        _lastHitName = currentHitName;
        LogDebug(currentHitDistance >= 0f
            ? $"Reticle hit: {currentHitName} at {currentHitDistance:F2}m"
            : "Reticle hit: nothing");
    }

    private void CacheReticleObjects()
    {
        _reticleObjects.Clear();
        foreach (XRCardboardReticle reticle in Resources.FindObjectsOfTypeAll<XRCardboardReticle>())
        {
            if (reticle == null || !reticle.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!_reticleObjects.Contains(reticle.gameObject))
            {
                _reticleObjects.Add(reticle.gameObject);
            }
        }

        _reticleClickers.Clear();
        foreach (ReticleJoystickClick clicker in Resources.FindObjectsOfTypeAll<ReticleJoystickClick>())
        {
            if (clicker == null || !clicker.gameObject.scene.IsValid())
            {
                continue;
            }

            _reticleClickers.Add(clicker);
        }
    }

    private void SetReticleState(bool isVisible)
    {
        for (int i = 0; i < _reticleObjects.Count; i++)
        {
            if (_reticleObjects[i] != null)
            {
                _reticleObjects[i].SetActive(isVisible);
            }
        }

        for (int i = 0; i < _reticleClickers.Count; i++)
        {
            if (_reticleClickers[i] != null)
            {
                _reticleClickers[i].enabled = isVisible;
            }
        }
    }

    private void SetBrushCollidersEnabled(bool isEnabled)
    {
        if (_brushColliders == null)
        {
            return;
        }

        for (int i = 0; i < _brushColliders.Length; i++)
        {
            if (_brushColliders[i] != null)
            {
                _brushColliders[i].enabled = isEnabled;
            }
        }
    }

    private void LogDebug(string message)
    {
        if (!debugReticleTargeting)
        {
            return;
        }

        Debug.Log($"[BrushEquipController] {message}", this);
    }
}
