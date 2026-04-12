using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Button backButton;
    public AudioManager audioManagerObj;
    public PauseMenuController pauseMenuController;
    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;
    public float volumeStep = 0.05f;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 4;

    private bool _verticalNeutral   = true;
    private bool _horizontalNeutral = true;
    private float _vertCooldown = 0f;
    private float _horizCooldown = 0f;

    private static readonly Color SELECTED_COLOR   = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private void OnEnable()
    {
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();

        masterVolumeSlider.onValueChanged.AddListener(audioManagerObj.SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(audioManagerObj.SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(audioManagerObj.SetSFXVolume);

        _selectedIndex = 0;
        UpdateHighlight();
    }

    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }

    private void Update()
    {
        _vertCooldown  -= Time.unscaledDeltaTime;
        _horizCooldown -= Time.unscaledDeltaTime;

        HandleVerticalNavigation();
        HandleHorizontalAdjustment();
        HandleConfirm();
    }

    private void HandleVerticalNavigation()
    {
        float axis = Input.GetAxis("Vertical");

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _verticalNeutral = true;
            return;
        }

        if (!_verticalNeutral || _vertCooldown > 0f) return;

        if (axis > -stickThreshold)
            _selectedIndex = (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT;
        else
            _selectedIndex = (_selectedIndex + 1) % OPTION_COUNT;

        _verticalNeutral = false;
        _vertCooldown = navigationCooldown;
        UpdateHighlight();
    }

    private void HandleHorizontalAdjustment()
    {
        if (_selectedIndex == 3) return;

        float axis = Input.GetAxis("Horizontal");

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _horizontalNeutral = true;
            return;
        }

        if (!_horizontalNeutral || _horizCooldown > 0f) return;

        float delta = axis > 0 ? volumeStep : -volumeStep;
        AdjustSelectedSlider(delta);

        _horizontalNeutral = false;
        _horizCooldown = navigationCooldown;
    }

    private void AdjustSelectedSlider(float delta)
    {
        Slider slider = GetSelectedSlider();
        if (slider == null) return;
        slider.value = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
    }

    private Slider GetSelectedSlider()
    {
        switch (_selectedIndex)
        {
            case 0: return masterVolumeSlider;
            case 1: return musicVolumeSlider;
            case 2: return sfxVolumeSlider;
            default: return null;
        }
    }

    private void HandleConfirm()
    {
        if (!Input.GetButtonDown("js2")) return;

        if (_selectedIndex == 3)
            pauseMenuController.BackToPauseMenu();
    }

    private void UpdateHighlight()
    {
        SetSliderColor(masterVolumeSlider, _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetSliderColor(musicVolumeSlider,  _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetSliderColor(sfxVolumeSlider,    _selectedIndex == 2 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(backButton,         _selectedIndex == 3 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetSliderColor(Slider slider, Color color)
    {
        if (slider == null) return;
        var fill = slider.fillRect;
        if (fill != null)
        {
            var img = fill.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }
}