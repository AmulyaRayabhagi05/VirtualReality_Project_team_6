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
    [SerializeField] private float dropHeightOffset = 0.15f;
    [SerializeField] private bool startEquipped;

    private readonly List<GameObject> _reticleObjects = new List<GameObject>();
    private readonly List<ReticleJoystickClick> _reticleClickers = new List<ReticleJoystickClick>();
    private Renderer[] _brushRenderers;
    private bool _isEquipped;

    public bool IsEquipped
    {
        get { return _isEquipped; }
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
                DropBrush();
            }

            return;
        }

        if (pickupPressed && IsBrushTargeted())
        {
            EquipBrush();
        }
    }

    private bool IsBrushTargeted()
    {
        if (_brushRenderers == null || _brushRenderers.Length == 0)
        {
            return false;
        }

        Bounds bounds = _brushRenderers[0].bounds;
        for (int i = 1; i < _brushRenderers.Length; i++)
        {
            bounds.Encapsulate(_brushRenderers[i].bounds);
        }

        Vector3 center = bounds.center;
        float distance = Vector3.Distance(sourceCamera.transform.position, center);
        if (distance > pickupRange)
        {
            return false;
        }

        Vector3 screenPoint = sourceCamera.WorldToScreenPoint(center);
        if (screenPoint.z <= 0f)
        {
            return false;
        }

        Vector2 viewportCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 screenPosition = new Vector2(screenPoint.x, screenPoint.y);
        return Vector2.Distance(viewportCenter, screenPosition) <= reticleThresholdPixels;
    }

    private void EquipBrush()
    {
        brushTransform.SetParent(brushAnchor, false);
        brushTransform.localPosition = equippedLocalPosition;
        brushTransform.localRotation = Quaternion.Euler(equippedLocalEulerAngles);
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
        Vector3 dropPosition = cameraTransform.position + (forward * dropDistance);

        if (Physics.Raycast(dropPosition + Vector3.up, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            dropPosition.y = hit.point.y + dropHeightOffset;
        }

        brushTransform.SetParent(null, true);
        brushTransform.position = dropPosition;
        brushTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        _isEquipped = false;
        SetReticleState(true);
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
}
