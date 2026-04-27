using UnityEngine;
using UnityEngine.UI;

public class PressButtonController : MonoBehaviour
{
    public DinoTransitionController dinoController;
    public Button pressButton;

    private DinoNetworkSync _networkSync;

    void Start()
    {
        if (dinoController != null)
            _networkSync = dinoController.GetComponent<DinoNetworkSync>();

        if (pressButton != null)
            pressButton.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        if (_networkSync != null && _networkSync.IsSpawned)
            _networkSync.OnButtonPressedNetworked();
        else
            dinoController.OnButtonPressed();
    }
}
