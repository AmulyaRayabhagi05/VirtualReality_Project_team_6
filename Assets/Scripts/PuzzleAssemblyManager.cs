using UnityEngine;

public class PuzzleAssemblyManager : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private PuzzleAssemblyPiece[] pieces;
    [SerializeField] private string pickupButton = "js2";
    [SerializeField] private KeyCode pickupKey = KeyCode.R;
    [SerializeField] private string rotateButton = "js0";
    [SerializeField] private KeyCode rotateKey = KeyCode.T;
    [SerializeField] private float holdDistance = 1.2f;
    [SerializeField] private Vector3 holdOffset = Vector3.zero;
    [SerializeField] private bool followReticleToSurface = true;
    [SerializeField] private bool lockHeldPieceToPuzzleHeight = true;
    [SerializeField] private float pickupRange = 6f;
    [SerializeField] private LayerMask pickupMask = ~0;
    [SerializeField] private LayerMask placementSurfaceMask = ~0;
    [SerializeField] private float placementSurfaceRange = 20f;
    [SerializeField] private float heldRotationSpeed = 120f;
    [SerializeField] private GameObject[] activateOnComplete;
    [SerializeField] private GameObject[] deactivateOnComplete;

    private PuzzleAssemblyPiece _heldPiece;
    private bool _completed;
    private void Awake()
    {
        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null)
            {
                pieces[i].Initialize(this);
            }
        }
    }

    private void Update()
    {
        if (sourceCamera == null)
        {
            return;
        }

        if (IsPickupPressed())
        {
            if (_heldPiece != null)
            {
                DropHeldPiece();
                return;
            }

            TryHandlePickupInput();
            return;
        }

        if (_heldPiece == null)
        {
            return;
        }

        Transform cameraTransform = sourceCamera.transform;
        Vector3 holdPosition = cameraTransform.position +
            cameraTransform.forward * holdDistance +
            cameraTransform.TransformVector(holdOffset);

        if (followReticleToSurface)
        {
            Ray surfaceRay = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit surfaceHit;
            if (Physics.Raycast(surfaceRay, out surfaceHit, placementSurfaceRange, placementSurfaceMask, QueryTriggerInteraction.Ignore))
            {
                holdPosition = surfaceHit.point;
            }
        }

        if (lockHeldPieceToPuzzleHeight)
        {
            Vector3 placementPosition;
            if (_heldPiece.TryGetPlacementPosition(out placementPosition))
            {
                holdPosition.y = placementPosition.y;
            }
        }

        _heldPiece.MoveHeldPiece(holdPosition, cameraTransform.rotation);
        ApplyHeldRotation();
    }

    private void TryHandlePickupInput()
    {
        Ray ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("[PuzzleAssemblyManager] Pickup input pressed but raycast hit nothing.");
            return;
        }

        PuzzleAssemblyPiece piece = hit.collider.GetComponentInParent<PuzzleAssemblyPiece>();
        if (piece == null)
        {
            Debug.LogWarning($"[PuzzleAssemblyManager] Raycast hit '{hit.collider.name}' but no PuzzleAssemblyPiece was found in parents.");
            return;
        }

        Debug.LogWarning($"[PuzzleAssemblyManager] Picking up {piece.name}");
        TryPickUp(piece);
    }

    private bool IsPickupPressed()
    {
        return Input.GetButtonDown(pickupButton) || Input.GetKeyDown(pickupKey);
    }

    public bool TryPickUp(PuzzleAssemblyPiece piece)
    {
        if (piece == null || _heldPiece != null || piece.IsPlaced)
        {
            Debug.LogWarning("[PuzzleAssemblyManager] TryPickUp rejected.");
            return false;
        }

        _heldPiece = piece;
        piece.BeginHold();
        return true;
    }

    private void DropHeldPiece()
    {
        if (_heldPiece == null)
        {
            return;
        }

        PuzzleAssemblyPiece pieceToDrop = _heldPiece;
        _heldPiece = null;
        pieceToDrop.Drop();

        if (pieceToDrop.TryPlaceAfterDrop())
        {
            RefreshPlacedOutlines();
            CheckCompletion();
        }
    }

    private void ApplyHeldRotation()
    {
        if (_heldPiece == null)
        {
            return;
        }

        if (!Input.GetButton(rotateButton) && !Input.GetKey(rotateKey))
        {
            return;
        }

        _heldPiece.RotateHeld(heldRotationSpeed * Time.deltaTime);
    }

    private void CheckCompletion()
    {
        if (_completed)
        {
            return;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && !pieces[i].IsPlaced)
            {
                return;
            }
        }

        _completed = true;
        RefreshPlacedOutlines();

        SetObjectsActive(activateOnComplete, true);
        SetObjectsActive(deactivateOnComplete, false);
    }

    private void RefreshPlacedOutlines()
    {
        int placedCount = 0;
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && pieces[i].IsPlaced)
            {
                placedCount++;
            }
        }

        bool shouldOutlinePlacedPieces = placedCount >= 2;
        Debug.Log($"[PuzzleAssemblyManager] placedCount={placedCount}, shouldOutline={shouldOutlinePlacedPieces}");
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && pieces[i].IsPlaced)
            {
                pieces[i].SetOutline(shouldOutlinePlacedPieces);
            }
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(isActive);
            }
        }
    }
}
