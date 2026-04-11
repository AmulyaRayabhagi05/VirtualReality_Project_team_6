using TMPro;   // Remove if using legacy TextMesh instead of TextMeshPro
using UnityEngine;

/// <summary>
/// Attach to the cube. Requires a Box Collider (default on cubes).
/// Calls DinoTransitionController.OnButtonPressed() on click or reticle input.
/// </summary>
public class PressButtonController : MonoBehaviour
{
    [Header("References")]
    public DinoTransitionController dinoController;

    [Header("Label")]
    public TextMeshPro buttonText;      // swap for TextMesh if using legacy 3D text

    [Header("Input")]
    [SerializeField] private string reticleButton = "js2";
    [SerializeField] private KeyCode keyboardKey = KeyCode.R;
    [SerializeField] private float fallbackRayLength = 20f;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.65f, 0.65f, 0.65f);

    private Renderer rend;
    private Collider targetCollider;

    void Start()
    {
        rend = GetComponent<Renderer>();
        targetCollider = GetComponent<Collider>();
        ApplyColor(normalColor);
        SetButtonText("Press");
    }

    void Update()
    {
        if (!IsPressInputDown() || !IsReticleOverButton())
        {
            return;
        }

        TriggerPress();
    }

    void OnMouseDown()
    {
        TriggerPress();
    }

    void OnMouseUp() => ApplyColor(normalColor);

    public void SetButtonText(string text)
    {
        if (buttonText) buttonText.text = text;
    }

    private void TriggerPress()
    {
        ApplyColor(pressedColor);

        if (dinoController != null)
        {
            dinoController.OnButtonPressed();
        }
    }

    private bool IsPressInputDown()
    {
        return Input.GetButtonDown(reticleButton) || Input.GetKeyDown(keyboardKey);
    }

    private bool IsReticleOverButton()
    {
        if (targetCollider == null)
        {
            return false;
        }

        Ray ray;
        float rayLength = fallbackRayLength;

        if (RaycastPointer.instance != null)
        {
            ray = RaycastPointer.instance.GetRay();
            rayLength = RaycastPointer.instance.raycastLength;
        }
        else if (Camera.main != null)
        {
            ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        }
        else
        {
            return false;
        }

        return targetCollider.Raycast(ray, out _, rayLength);
    }

    void ApplyColor(Color c)
    {
        if (rend) rend.material.color = c;
    }
}
