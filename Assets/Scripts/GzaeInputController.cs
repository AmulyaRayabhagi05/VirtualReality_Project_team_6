using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ClickInputController : MonoBehaviour
{
    [Header("Ray distance")]
    public float rayDistance = 50f;

    void Update()
    {
        bool clicked = Input.GetButtonDown("js2");

        if (!clicked) {return; }

        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            Button btn = hit.collider.GetComponent<Button>();
            if (btn == null)
            {
                btn = hit.collider.GetComponentInParent<Button>();
            }

            if (btn != null)
            {
                btn.onClick.Invoke();
            }
        }
    }
}