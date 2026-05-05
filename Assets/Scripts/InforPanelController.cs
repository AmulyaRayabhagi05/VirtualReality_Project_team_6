using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class InfoPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;

    [Header("Placement")]
    public Camera gazeCamera;
    public bool placeInFrontOfCamera = true;
    public float distanceFromCamera = 1.75f;
    public float verticalOffset = -0.05f;

    [Header("UI")]
    public Button closeButton;
    public Image displayImage;
    public TMP_Text titleText;
    public TMP_Text infoText;

    public Action OnPanelClosed;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (gazeCamera == null)
        {
            gazeCamera = Camera.main;
        }

        canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        if (closeButton != null) {
            closeButton.onClick.AddListener(HidePanel);
        }
        SetVisible(false);
    }

    public void ShowInfo(Sprite image, string title, string body)
    {
        if (displayImage != null)
        {
            displayImage.sprite = image;
            displayImage.enabled = (image != null);
        }

        if (titleText != null) {titleText.text = title; }
        if (infoText != null) {
            infoText.text = body;
        }

        SetVisible(true);
        PlacePanel();
    }

    public void HidePanel()
    {
        SetVisible(false);
        OnPanelClosed?.Invoke();
    }

    public void HidePanelSilent()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    void PlacePanel()
    {
        if (!placeInFrontOfCamera || panelRoot == null)
        {
            return;
        }

        if (gazeCamera == null)
        {
            gazeCamera = Camera.main;
        }

        if (gazeCamera == null)
        {
            return;
        }

        // If this is a world-space canvas, ensure it’s in front of the camera and facing it.
        var canvas = panelRoot.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = gazeCamera;
            }

            Transform t = canvas.transform;
            Vector3 pos = gazeCamera.transform.position + gazeCamera.transform.forward * distanceFromCamera;
            pos += new Vector3(0f, verticalOffset, 0f);
            t.position = pos;
            t.rotation = Quaternion.LookRotation(t.position - gazeCamera.transform.position);
        }
    }
}
