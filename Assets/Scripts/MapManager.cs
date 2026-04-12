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
    public string dinosaurText =
        "Test";

    [Header("Plane Info")]
    public Sprite planeImage;
    [TextArea(3, 6)]
    public string planeTitle = "Plane";
    [TextArea(3, 10)]
    public string planeText =
        "Test";

    [Header("Pyramid Info")]
    public Sprite pyramidImage;
    [TextArea(3, 6)]
    public string pyramidTitle = "Pyramid";
    [TextArea(3, 10)]
    public string pyramidText =
        "Test";

    void Start()
    {
        if (infoPanelController == null)
        {
            Debug.LogError("InfoPanel not assigned!");
            return;
        }

        infoPanelController.HidePanel();
        ShowMapButtons(true);

        infoPanelController.OnPanelClosed = () => ShowMapButtons(true);

        if (dinosaurButton != null)
        {
            dinosaurButton.onClick.AddListener(OnDinosaurClicked);
        }

        if (planeButton != null) { 
            planeButton.onClick.AddListener(OnPlaneClicked);
        }
        if (pyramidButton != null) { 
            pyramidButton.onClick.AddListener(OnPyramidClicked);
        }
    }

    void ShowMapButtons(bool visible)
    {
        if (dinosaurButton != null) { dinosaurButton.gameObject.SetActive(visible); }
        if (planeButton != null){ 
            planeButton.gameObject.SetActive(visible);
        }
        if (pyramidButton != null)
        {
            pyramidButton.gameObject.SetActive(visible);
        }
    }

    void OnDinosaurClicked()
    {
        ShowMapButtons(false);
        infoPanelController.ShowInfo(dinosaurImage, dinosaurTitle, dinosaurText);
    }

    void OnPlaneClicked()
    {
        ShowMapButtons(false);
        infoPanelController.ShowInfo(planeImage, planeTitle, planeText);
    }

    void OnPyramidClicked()
    {
        ShowMapButtons(false);
        infoPanelController.ShowInfo(pyramidImage, pyramidTitle, pyramidText);
    }
}