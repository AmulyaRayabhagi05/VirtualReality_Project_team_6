using UnityEngine;

public class PuzzlePlacementBase : MonoBehaviour
{
    [SerializeField] private string expectedPieceId;
    [SerializeField] private Vector3 boundsPadding = new Vector3(0.08f, 0.08f, 0.08f);

    private BoxCollider _triggerCollider;
    private int _matchingOverlapCount;

    public void Configure(string pieceId)
    {
        expectedPieceId = pieceId;
        EnsureTriggerCollider();
    }

    public bool Matches(PuzzleAssemblyPiece piece)
    {
        return piece != null && piece.PieceId == expectedPieceId;
    }

    public bool IsMatchedByDroppedPiece()
    {
        return _matchingOverlapCount > 0;
    }

    public Vector3 GetPlacementPosition()
    {
        if (_triggerCollider != null)
        {
            return _triggerCollider.bounds.center;
        }

        return transform.position;
    }

    public bool IsOverlappingPiece(PuzzleAssemblyPiece piece)
    {
        if (!Matches(piece) || _triggerCollider == null || piece == null)
        {
            return false;
        }

        Collider[] pieceColliders = piece.GetPlacementColliders();
        if (pieceColliders == null)
        {
            return false;
        }

        Bounds triggerBounds = _triggerCollider.bounds;
        for (int i = 0; i < pieceColliders.Length; i++)
        {
            Collider pieceCollider = pieceColliders[i];
            if (pieceCollider != null && pieceCollider.enabled && triggerBounds.Intersects(pieceCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void OnEnable()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        PuzzleAssemblyPiece piece = other.GetComponentInParent<PuzzleAssemblyPiece>();
        if (Matches(piece))
        {
            _matchingOverlapCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PuzzleAssemblyPiece piece = other.GetComponentInParent<PuzzleAssemblyPiece>();
        if (Matches(piece))
        {
            _matchingOverlapCount = Mathf.Max(0, _matchingOverlapCount - 1);
        }
    }

    private void EnsureTriggerCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponent<BoxCollider>();
        }

        if (_triggerCollider == null)
        {
            _triggerCollider = gameObject.AddComponent<BoxCollider>();
        }

        _triggerCollider.isTrigger = true;
        _triggerCollider.center = transform.InverseTransformPoint(combinedBounds.center);
        _triggerCollider.size = combinedBounds.size + boundsPadding;
    }
}
