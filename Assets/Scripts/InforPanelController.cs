using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class InfoPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;

    [Header("UI")]
    public Button closeButton;
    public Image displayImage;
    public TMP_Text titleText;
    public TMP_Text infoText;

    public Action OnPanelClosed;

    private CanvasGroup canvasGroup;

    void Awake()
    {
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
    }

    public void HidePanel()
    {
        SetVisible(false);
        OnPanelClosed?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}