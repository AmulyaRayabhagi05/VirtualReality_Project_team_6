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

        Ray centerProbe = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        RaycastHit hit;
        if (!Physics.SphereCast(centerProbe, brushRadius, out hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        ExcavationDirtPile dirtPile = hit.collider.GetComponentInParent<ExcavationDirtPile>();
        if (dirtPile == null)
        {
            return;
        }

        dirtPile.Excavate(hit.point, brushRadius);
    }
}
