using UnityEngine;

[RequireComponent(typeof(Outline))]
public class HighlightUsingReticle : MonoBehaviour{
    private Outline outline;
    private Camera mainCamera;

    void OnEnable(){
        outline=GetComponent<Outline>();
        outline.enabled=false;

        mainCamera=Camera.main;
    }

    void Update(){
        if (mainCamera==null){return;}

        Ray ray=mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray,out hit)){
            if (hit.transform==transform){
                outline.enabled=true;
                return;
            }
        }
        outline.enabled=false;
    }
}