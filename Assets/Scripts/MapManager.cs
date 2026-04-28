using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapManager : MonoBehaviour
{
    [Header("Map Icon Buttons")]
    public Button dinosaurButton;
    public Button planeButton;
    public Button pyramidButton;

    [Header("Info Panel Controller ")]
    public InfoPanelController infoPanelController;

    [Header("Dinosaur Info")]
    public Sprite dinosaurImage;
    [TextArea(3, 6)]
    public string dinosaurTitle = "DINO";
    [TextArea(3, 10)]
    public string dinosaurText = "Test";

    [Header("Plane Info")]
    public Sprite planeImage;
    [TextArea(3, 6)]
    public string planeTitle = "Plane";
    [TextArea(3, 10)]
    public string planeText = "Test";

    [Header("Pyramid Info")]
    public Sprite pyramidImage;
    [TextArea(3, 6)]
    public string pyramidTitle = "Pyramid";
    [TextArea(3, 10)]
    public string pyramidText = "Test";

    private MapNetworkSync _networkSync;

    void Start()
    {
        _networkSync = GetComponent<MapNetworkSync>();

        if (infoPanelController == null)
        {
            Debug.LogError("InfoPanel not assigned!");
            return;
        }

        infoPanelController.HidePanel();
        ShowMapButtons(true);

        infoPanelController.OnPanelClosed = () =>
        {
            if (_networkSync != null && _networkSync.IsSpawned)
                _networkSync.RequestHidePanel();
            else
                ApplyPanelSelection(-1);
        };

        if (dinosaurButton != null) dinosaurButton.onClick.AddListener(OnDinosaurClicked);
        if (planeButton != null) planeButton.onClick.AddListener(OnPlaneClicked);
        if (pyramidButton != null) pyramidButton.onClick.AddListener(OnPyramidClicked);
    }

    // Called by MapNetworkSync on every client when the panel selection changes.
    public void ApplyPanelSelection(int index)
    {
        if (index < 0)
        {
            infoPanelController.HidePanelSilent();
            ShowMapButtons(true);
            return;
        }

        ShowMapButtons(false);
        switch (index)
        {
            case 0: infoPanelController.ShowInfo(dinosaurImage, dinosaurTitle, dinosaurText); break;
            case 1: infoPanelController.ShowInfo(planeImage, planeTitle, planeText); break;
            case 2: infoPanelController.ShowInfo(pyramidImage, pyramidTitle, pyramidText); break;
        }
    }

    void ShowMapButtons(bool visible)
    {
        if (dinosaurButton != null) dinosaurButton.gameObject.SetActive(visible);
        if (planeButton != null) planeButton.gameObject.SetActive(visible);
        if (pyramidButton != null) pyramidButton.gameObject.SetActive(visible);
    }

    void OnDinosaurClicked()
    {
        if (_networkSync != null && _networkSync.IsSpawned)
            _networkSync.RequestShowPanel(0);
        else
            ApplyPanelSelection(0);
    }

    void OnPlaneClicked()
    {
        if (_networkSync != null && _networkSync.IsSpawned)
            _networkSync.RequestShowPanel(1);
        else
            ApplyPanelSelection(1);
    }

    void OnPyramidClicked()
    {
        if (_networkSync != null && _networkSync.IsSpawned)
            _networkSync.RequestShowPanel(2);
        else
            ApplyPanelSelection(2);
    }
}
