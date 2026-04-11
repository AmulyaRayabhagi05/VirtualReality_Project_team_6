using UnityEngine;
using TMPro;   
public class PressButtonController : MonoBehaviour
{
    [Header("References")]
    public DinoTransitionController dinoController;

    [Header("Label")]
    public TextMeshPro buttonText;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.65f, 0.65f, 0.65f);

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        ApplyColor(normalColor);
        SetButtonText("Press");
    }

    void OnMouseDown()
    {
        ApplyColor(pressedColor);
        dinoController.OnButtonPressed();
    }

    void OnMouseUp() => ApplyColor(normalColor);

    public void SetButtonText(string text)
    {
        if (buttonText)
        {
            buttonText.text = text;
        }
    }

    void ApplyColor(Color c)
    {
        if (rend) {
            rend.material.color = c;
        }
    }
}