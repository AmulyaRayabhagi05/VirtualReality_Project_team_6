using UnityEngine;
using UnityEngine.EventSystems;

public class Translate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float moveSpeed=2f;
    public Vector3 moveDirection=Vector3.right;
    private bool LookedAt=false;
    private Rigidbody rb;

    void Start(){rb=GetComponent<Rigidbody>();}

    public void OnPointerEnter(PointerEventData eventData){LookedAt=true;}

    public void OnPointerExit(PointerEventData eventData){LookedAt=false;}

    void FixedUpdate(){
        if (LookedAt && Input.GetButton("js2")){
            rb.MovePosition(rb.position+moveDirection*moveSpeed*Time.fixedDeltaTime);
        }
    }
}