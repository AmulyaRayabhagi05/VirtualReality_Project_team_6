using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if !UNITY_EDITOR
using UnityEngine.XR;
#endif

public class ReticleJoystickClick : MonoBehaviour
{
    [SerializeField] private string clickButton = "js2";
    [SerializeField] private KeyCode clickKey = KeyCode.F;
    [SerializeField] private bool debugReticleClick;
    public static bool IsJustOpened { get; private set; }

    private EventSystem _eventSystem;
    private PointerEventData _pointerEventData;

    private void ClearJustOpened()
    {
        IsJustOpened = false;
    }

    private void Awake()
    {
        _eventSystem = GetComponent<EventSystem>();
        if (_eventSystem == null)
        {
            _eventSystem = EventSystem.current;
        }
    }

    private void Update()
    {
        if (_eventSystem == null || (!Input.GetButtonDown(clickButton) && !Input.GetKeyDown(clickKey)))
        {
            return;
        }
        if (PlaneMenuController.IsJustOpened)
        {
            LogDebug("menu opened in secondFloor");
            return;
        }
        // When the plane cockpit menu is open, we want joystick navigation only (no reticle UI clicking).
        var planeMenu = FindFirstObjectByType<PlaneMenuController>();
        if (planeMenu != null && planeMenu.IsMenuOpen)
        {
            LogDebug("Plane menu is open; reticle click disabled.");
            return;
        }

        LogDebug($"Click input received: {clickButton} or {clickKey}");

        if (_pointerEventData == null)
        {
            _pointerEventData = new PointerEventData(_eventSystem);
        }

        _pointerEventData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);
        _pointerEventData.delta = Vector2.zero;

        var raycastResults = new List<RaycastResult>();
        _eventSystem.RaycastAll(_pointerEventData, raycastResults);
        raycastResults = raycastResults.OrderBy(result => !result.module.GetComponent<GraphicRaycaster>()).ToList();
        RaycastResult firstRaycast = FindFirstValidRaycast(raycastResults);
        LogDebug($"First raycast target: {(firstRaycast.gameObject != null ? firstRaycast.gameObject.name : "None")}");
        GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(firstRaycast.gameObject);

        if (target == null)
        {
            LogWarning("No clickable UI target under reticle.");
            return;
        }

        Selectable selectable = target.GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable)
        {
            LogWarning($"Target '{target.name}' is not interactable.");
            return;
        }

        _pointerEventData.pointerCurrentRaycast = firstRaycast;
        LogDebug($"Dispatching click to: {target.name}");
        ExecuteEvents.ExecuteHierarchy(target, _pointerEventData, ExecuteEvents.pointerClickHandler);
    }

    private static RaycastResult FindFirstValidRaycast(List<RaycastResult> raycastResults)
    {
        for (int i = 0; i < raycastResults.Count; ++i)
        {
            if (raycastResults[i].gameObject != null)
            {
                return raycastResults[i];
            }
        }

        return new RaycastResult();
    }

    private void LogDebug(string message)
    {
        if (!debugReticleClick)
        {
            return;
        }

        Debug.Log($"[ReticleJoystickClick] {message}", this);
    }

    private void LogWarning(string message)
    {
        if (!debugReticleClick)
        {
            return;
        }

        Debug.LogWarning($"[ReticleJoystickClick] {message}", this);
    }
}
