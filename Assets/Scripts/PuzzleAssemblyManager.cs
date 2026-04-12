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

    public bool IsCompleted
    {
        get { return _completed; }
    }



    public bool AreAllPiecesPlaced()
    {
        if (pieces == null || pieces.Length == 0)
        {
            return false;
        }

        bool foundPiece = false;
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null)
            {
                continue;
            }

            foundPiece = true;
            if (!pieces[i].IsPlaced)
            {
                return false;
            }
        }

        return foundPiece;
    }

    public string GetCompletionSummary()
    {
        if (pieces == null || pieces.Length == 0)
        {
            return "no-pieces";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < pieces.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            PuzzleAssemblyPiece piece = pieces[i];
            if (piece == null)
            {
                sb.Append("null");
                continue;
            }

            sb.Append(piece.PieceId);
            sb.Append('=');
            sb.Append(piece.IsPlaced ? '1' : '0');
        }

        return sb.ToString();
    }

    public bool HasConfiguredPieces
    {
        get
        {
            if (pieces == null || pieces.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

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
            return;
        }

        PuzzleAssemblyPiece piece = hit.collider.GetComponentInParent<PuzzleAssemblyPiece>();
        if (piece == null)
        {
            return;
        }

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
        ForcePlacedOutlines(true);

        SetObjectsActive(activateOnComplete, true);
        SetObjectsActive(deactivateOnComplete, false);
    }

    public void SetPlacedOutlines(bool isOutlined)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && pieces[i].IsPlaced)
            {
                pieces[i].SetOutline(isOutlined);
            }
        }
    }

    private void ForcePlacedOutlines(bool isOutlined)
    {
        SetPlacedOutlines(isOutlined);
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
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && pieces[i].IsPlaced)
            {
                Debug.Log($"[PuzzleAssemblyManager:{gameObject.name}] outline {pieces[i].PieceId} => {shouldOutlinePlacedPieces}");
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
