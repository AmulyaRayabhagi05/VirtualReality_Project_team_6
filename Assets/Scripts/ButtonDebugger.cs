using UnityEngine;
using UnityEngine.UI;

public class ButtonDebugger : MonoBehaviour
{
    public Button targetButton;

    void Start()
    {
        targetButton.onClick.AddListener(() =>
        {
            Debug.Log("Button CLicked");
        });
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"Left click at: {Input.mousePosition}");

            var pointerData = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

            foreach (var r in results)
            {
                Debug.Log($"Raycast hit: {r.gameObject.name}");

                if (r.gameObject.name == "Try again button")
                {
                    Debug.Log("button clicking");
                    r.gameObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                }
            }
        }
    }
}