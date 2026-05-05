using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapManager : MonoBehaviour
{
    [Header("Map Icon Buttons")]
    public Button dinosaurButton;
    public Button planeButton;
    public Button pyramidButton;
    public Button greeceButton;
    public Button JapanButton;

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

    [Header("Greece Info")]
    public Sprite greeceImage;
    [TextArea(3, 6)]
    public string greeceTitle = "Greece";
    [TextArea(3, 10)]
    public string greeceText = "Test";

    [Header("Japan Info")]
    public Sprite japanImage;
    [TextArea(3, 6)]
    public string japanTitle = "Japan";
    [TextArea(3, 10)]
    public string japanText = "Test";

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
        if (greeceButton != null) greeceButton.onClick.AddListener(OnGreeceClicked);
        if (japanButton != null) japanButton.onClick.AddListener(OnJapanClicked);
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
            case 3: infoPanelController.ShowInfo(greeceImage, greeceTitle, greeceText); break;
            case 4: infoPanelController.ShowInfo(japanImage, japanTitle, japanText); break;
        }
    }

    void ShowMapButtons(bool visible)
    {
        if (dinosaurButton != null) dinosaurButton.gameObject.SetActive(visible);
        if (planeButton != null) planeButton.gameObject.SetActive(visible);
        if (pyramidButton != null) pyramidButton.gameObject.SetActive(visible);
        if (greeceButton != null) greeceButton.gameObject.SetActive(visible);
        if (japanButton != null) japanButton.gameObject.SetActive(visible);
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
