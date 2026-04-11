using UnityEngine;

public class ExcavationBrushInteractor : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private BrushEquipController brushEquipController;
    [SerializeField] private string excavateButton = "js10";
    [SerializeField] private KeyCode excavateKey = KeyCode.R;
    [SerializeField] private float range = 6f;
    [SerializeField] private float brushRadius = 0.28f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool debugExcavation;

    private void Awake()
    {
        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (sourceCamera == null || brushEquipController == null)
        {
            return;
        }

        bool excavatePressed = Input.GetButton(excavateButton) || Input.GetKey(excavateKey);
        if (!brushEquipController.IsEquipped || !excavatePressed)
        {
            return;
        }

        LogDebug("Excavate input detected while brush is equipped.");

        Ray centerProbe = brushEquipController.GetCurrentReticleRay();
        RaycastHit hit;
        float castRange = RaycastPointer.instance != null ? RaycastPointer.instance.raycastLength : range;
        if (!Physics.SphereCast(centerProbe, brushRadius, out hit, castRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            LogDebug("Excavation spherecast hit nothing.");
            return;
        }

        LogDebug($"Excavation spherecast hit {hit.collider.name} at {hit.distance:F2}m.");
        ExcavationDirtPile dirtPile = hit.collider.GetComponentInParent<ExcavationDirtPile>();
        if (dirtPile == null)
        {
            LogDebug("Spherecast hit collider without an ExcavationDirtPile parent.");
            return;
        }

        LogDebug("Excavating via camera-forward spherecast.");
        dirtPile.Excavate(hit.point, brushRadius);
    }

    private void LogDebug(string message)
    {
        if (!debugExcavation)
        {
            return;
        }

        Debug.Log($"[ExcavationBrushInteractor] {message}", this);
    }
}
