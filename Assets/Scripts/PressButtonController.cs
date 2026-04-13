using UnityEngine;
public class PressButtonController : MonoBehaviour
{
    public DinoTransitionController dinoController;

    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.65f, 0.65f, 0.65f);

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        ApplyColor(normalColor);
    }

    void Update()
    {
        if (Input.GetButtonDown("js2") || Input.GetKeyDown(KeyCode.M))
        {
            ApplyColor(pressedColor);
            dinoController.OnButtonPressed();
        }

        if (Input.GetButtonUp("js2") || Input.GetKeyUp(KeyCode.M))
        {
            ApplyColor(normalColor);
        }
    }

    void ApplyColor(Color c)
    {
        if (rend) {
            rend.material.color = c;
        }
    }
}