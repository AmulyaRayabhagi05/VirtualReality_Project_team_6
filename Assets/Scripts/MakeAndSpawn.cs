using UnityEngine;
using UnityEngine.EventSystems;

public class MakeAndSpawn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public static GameObject lastDestroyedPrefab=null;
    public static string lastDestroyedTag="";

    private bool isGazed=false;
    private bool yWasPressed=false;

    public void OnPointerEnter(PointerEventData eventData){isGazed=true;}

    public void OnPointerExit(PointerEventData eventData){ isGazed=false; }

    void Update(){

        bool yPressed=Input.GetButton("js3");

        if (yPressed && !yWasPressed){

            if (isGazed){

                lastDestroyedPrefab=this.gameObject;
                lastDestroyedTag=this.gameObject.tag;

                MakeAndSpawnManage.instance.StoreObject(this.gameObject);
                Destroy(this.gameObject);
            }
        }

        yWasPressed=yPressed;
    }
}