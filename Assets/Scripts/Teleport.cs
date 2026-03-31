using UnityEngine;

public class Teleport : MonoBehaviour{
    public Camera gazeCamera;
    public LayerMask floorLayer;
    private CharacterController characterController;
    private bool aWasPressed=false;

    void Start(){ characterController=GetComponent<CharacterController>();}

    void Update(){
        bool aPressed=Input.GetButton("js0");

        if (aPressed && !aWasPressed){TryTeleport();}

        aWasPressed=aPressed;
    }

    public float characterHeight=1f;
    void TryTeleport(){

        Ray ray=new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200f, floorLayer)){

            Vector3 teleportPos=hit.point+new Vector3(0, characterHeight, 0);
            characterController.enabled=false;
            transform.position=teleportPos;
            characterController.enabled=true;
        }
    }
}