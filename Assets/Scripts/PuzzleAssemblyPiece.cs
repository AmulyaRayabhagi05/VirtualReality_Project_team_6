using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzleAssemblyPiece : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string pieceId;
    [SerializeField] private Transform completedTarget;
    [SerializeField] private GameObject completedPieceVisual;
    [SerializeField] private float placementRadius = 0.2f;
    [SerializeField] private float placementHeightTolerance = 0.3f;
    [SerializeField] private bool hideCompletedPieceOnStart = true;

    private PuzzleAssemblyManager _manager;
    private Rigidbody _rigidbody;
    private Collider[] _colliders;
    private Collider[] _completedColliders;
    private Renderer[] _completedRenderers;
    private bool _isHeld;
    private bool _isPlaced;
    private Quaternion _holdRotationOffset = Quaternion.identity;
    private Quaternion _additionalHoldRotation = Quaternion.identity;
    private static readonly Quaternion PickupRotationOffset = Quaternion.Euler(-90f, 0f, 0f);

    public bool IsPlaced
    {
        get { return _isPlaced; }
    }

    public string PieceId
    {
        get { return pieceId; }
    }

    public GameObject CompletedPieceVisual
    {
        get { return completedPieceVisual; }
    }

    public Collider[] GetPlacementColliders()
    {
        return _colliders;
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
        CacheCompletedVisualComponents();

        if (completedPieceVisual != null)
        {
            Transform current = completedPieceVisual.transform;
            while (current != null)
            {
                current.gameObject.SetActive(true);
                current = current.parent;
            }

            PuzzlePlacementBaseVisual baseVisual = completedPieceVisual.GetComponent<PuzzlePlacementBaseVisual>();
            if (baseVisual == null)
            {
                baseVisual = completedPieceVisual.AddComponent<PuzzlePlacementBaseVisual>();
            }

            PuzzlePlacementBase placementBase = completedPieceVisual.GetComponent<PuzzlePlacementBase>();
            if (placementBase == null)
            {
                placementBase = completedPieceVisual.AddComponent<PuzzlePlacementBase>();
            }

            placementBase.Configure(pieceId);
        }
    }

    public void Initialize(PuzzleAssemblyManager manager)
    {
        _manager = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_manager == null || _isPlaced || _isHeld)
        {
            return;
        }

        _manager.TryPickUp(this);
    }

    public void BeginHold()
    {
        _isHeld = true;
        Debug.LogWarning($"[PuzzleAssemblyPiece] BeginHold {pieceId}");
        _holdRotationOffset = PickupRotationOffset;
        _additionalHoldRotation = Quaternion.identity;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(false);
    }

    public void MoveHeldPiece(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!_isHeld)
        {
            return;
        }

        transform.rotation = _additionalHoldRotation * _holdRotationOffset;

        Vector3 visualCenter = GetVisualCenter();
        Vector3 positionOffset = worldPosition - visualCenter;
        transform.position += positionOffset;
    }

    public void RotateHeld(float angleDegrees)
    {
        if (!_isHeld)
        {
            return;
        }

        _additionalHoldRotation = Quaternion.AngleAxis(angleDegrees, Vector3.up) * _additionalHoldRotation;
    }

    public bool CanPlace()
    {
        if (!_isHeld)
        {
            return false;
        }

        Vector3 targetPosition;
        if (!TryGetPlacementPoint(out targetPosition))
        {
            Debug.Log($"[PuzzleAssemblyPiece] {pieceId} has no placement target.");
            return false;
        }

        float horizontalDistance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(targetPosition.x, targetPosition.z));
        float heightDistance = Mathf.Abs(transform.position.y - targetPosition.y);
        bool canPlace = horizontalDistance <= placementRadius && heightDistance <= placementHeightTolerance;
        if (canPlace)
        {
            Debug.Log($"[PuzzleAssemblyPiece] {pieceId} can place. Horizontal={horizontalDistance:F3}, Height={heightDistance:F3}, radius={placementRadius:F3}");
        }

        return canPlace;
    }

    public bool TryGetPlacementPosition(out Vector3 targetPosition)
    {
        return TryGetPlacementPoint(out targetPosition);
    }

    public void Place()
    {
        _isHeld = false;
        _isPlaced = true;
        Debug.LogWarning($"[PuzzleAssemblyPiece] Placing {pieceId}");
        ShowCompletedBase();
        SetOutline(false);
        gameObject.SetActive(false);
    }

    public void Drop()
    {
        _isHeld = false;
        Debug.LogWarning($"[PuzzleAssemblyPiece] Drop {pieceId} at {transform.position}");

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
        }

        SetCollidersEnabled(true);
    }

    public bool TryPlaceAfterDrop()
    {
        if (_isHeld || _isPlaced)
        {
            Debug.LogWarning($"[PuzzleAssemblyPiece] TryPlaceAfterDrop skipped for {pieceId}. held={_isHeld} placed={_isPlaced}");
            return false;
        }

        PuzzlePlacementBase placementBase = completedPieceVisual != null
            ? completedPieceVisual.GetComponent<PuzzlePlacementBase>()
            : null;
        if (placementBase == null)
        {
            Debug.LogWarning($"[PuzzleAssemblyPiece] {pieceId} has no placement base on drop.");
            return false;
        }

        if (!placementBase.IsMatchedByDroppedPiece() && !placementBase.IsOverlappingPiece(this))
        {
            Debug.LogWarning($"[PuzzleAssemblyPiece] {pieceId} drop did not overlap its matching base '{placementBase.name}'.");
            return false;
        }

        Debug.LogWarning($"[PuzzleAssemblyPiece] {pieceId} accepted on drop.");
        Place();
        return true;
    }

    public void RevealCompletedVisual()
    {
        if (completedPieceVisual == null)
        {
            return;
        }

        Transform current = completedPieceVisual.transform;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }

        completedPieceVisual.SetActive(true);

        PuzzleCompletionOutline completionOutline = completedPieceVisual.GetComponent<PuzzleCompletionOutline>();
        if (completionOutline == null)
        {
            completionOutline = completedPieceVisual.AddComponent<PuzzleCompletionOutline>();
        }

        completionOutline.PlayPulse();
    }

    public void SetOutline(bool isOutlined)
    {
        Debug.Log($"[PuzzleAssemblyPiece] {pieceId} outline => {isOutlined}");
        GameObject outlineTarget = completedPieceVisual != null ? completedPieceVisual : gameObject;
        PuzzleCompletionOutline completionOutline = outlineTarget.GetComponent<PuzzleCompletionOutline>();
        if (completionOutline == null)
        {
            completionOutline = outlineTarget.AddComponent<PuzzleCompletionOutline>();
        }

        completionOutline.SetOutlined(isOutlined);
    }

    private void ShowCompletedBase()
    {
        if (completedPieceVisual == null)
        {
            return;
        }

        PuzzlePlacementBaseVisual baseVisual = completedPieceVisual.GetComponent<PuzzlePlacementBaseVisual>();
        if (baseVisual != null)
        {
            baseVisual.ShowCompletedVisual();
        }

        PuzzlePlacementBase placementBase = completedPieceVisual.GetComponent<PuzzlePlacementBase>();
        if (placementBase != null)
        {
            placementBase.enabled = false;
        }
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (_colliders == null)
        {
            return;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
            {
                _colliders[i].enabled = isEnabled;
            }
        }
    }

    private void CacheCompletedVisualComponents()
    {
        if (completedPieceVisual == null)
        {
            _completedColliders = null;
            _completedRenderers = null;
            return;
        }

        _completedColliders = completedPieceVisual.GetComponentsInChildren<Collider>(true);
        _completedRenderers = completedPieceVisual.GetComponentsInChildren<Renderer>(true);
    }

    private bool TryGetPlacementPoint(out Vector3 targetPosition)
    {
        if (completedTarget != null)
        {
            targetPosition = completedTarget.position;
            return true;
        }

        if (completedPieceVisual != null)
        {
            PuzzlePlacementBase placementBase = completedPieceVisual.GetComponent<PuzzlePlacementBase>();
            if (placementBase != null)
            {
                targetPosition = placementBase.GetPlacementPosition();
                return true;
            }

            if (_completedColliders == null || _completedRenderers == null)
            {
                CacheCompletedVisualComponents();
            }

            if (_completedRenderers != null && _completedRenderers.Length > 0 && _completedRenderers[0] != null)
            {
                Bounds combinedBounds = _completedRenderers[0].bounds;
                for (int i = 1; i < _completedRenderers.Length; i++)
                {
                    if (_completedRenderers[i] != null)
                    {
                        combinedBounds.Encapsulate(_completedRenderers[i].bounds);
                    }
                }

                targetPosition = combinedBounds.center;
                return true;
            }

            targetPosition = completedPieceVisual.transform.position;
            return true;
        }

        targetPosition = Vector3.zero;
        return false;
    }

    private Vector3 GetVisualCenter()
    {
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                Collider pieceCollider = _colliders[i];
                if (pieceCollider != null && pieceCollider.enabled)
                {
                    return pieceCollider.bounds.center;
                }
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                return renderers[i].bounds.center;
            }
        }

        return transform.position;
    }
}
