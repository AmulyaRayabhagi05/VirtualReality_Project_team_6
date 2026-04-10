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

    private EventSystem _eventSystem;
    private PointerEventData _pointerEventData;

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
        if (_eventSystem == null || !Input.GetButtonDown(clickButton))
        {
            return;
        }

        Debug.Log($"[ReticleJoystickClick] Click input received: {clickButton}");

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
        Debug.Log($"[ReticleJoystickClick] First raycast target: {(firstRaycast.gameObject != null ? firstRaycast.gameObject.name : "None")}");
        GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(firstRaycast.gameObject);

        if (target == null)
        {
            Debug.LogWarning("[ReticleJoystickClick] No clickable UI target under reticle.");
            return;
        }

        Selectable selectable = target.GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable)
        {
            Debug.LogWarning($"[ReticleJoystickClick] Target '{target.name}' is not interactable.");
            return;
        }

        _pointerEventData.pointerCurrentRaycast = firstRaycast;
        Debug.Log($"[ReticleJoystickClick] Dispatching click to: {target.name}");
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
}
