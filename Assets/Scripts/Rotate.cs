using UnityEngine;
using UnityEngine.EventSystems;

public class Rotate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public float rotateSpeed=90f;
    public Vector3 rotateAxis=Vector3.up;

    private bool isGazed=false;

    public void OnPointerEnter(PointerEventData eventData){isGazed=true;}

    public void OnPointerExit(PointerEventData eventData){isGazed=false;}

    void Update(){
        if (isGazed && Input.GetButton("js2")){

            transform.Rotate(rotateAxis*rotateSpeed*Time.deltaTime, Space.World);

        }
    }
}