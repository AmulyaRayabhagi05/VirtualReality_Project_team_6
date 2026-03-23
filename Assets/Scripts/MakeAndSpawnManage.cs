using UnityEngine;

public class MakeAndSpawnManage : MonoBehaviour{
    public static MakeAndSpawnManage instance;

    private PrimitiveType lastType;
    private Vector3 lastScale;
    private Color lastColor;
    private bool hasStoredObject=false;
    private bool spawnWasPressed=false;
    private int storedFrame=-1;
    private string lastTag="";
    public Camera gazeCamera;
    public LayerMask floorLayer;

    void Awake(){instance=this;}

    public void StoreObject(GameObject obj){

        MeshFilter mf=obj.GetComponent<MeshFilter>();
        
	if (mf != null){

            string meshName=mf.sharedMesh.name.ToLower();
            if (meshName.Contains("cube")){
                lastType=PrimitiveType.Cube;
	    }else if (meshName.Contains("sphere")){
                lastType=PrimitiveType.Sphere;
	    }
        }

        lastScale=obj.transform.localScale;
        lastTag=obj.tag;

        Renderer rend=obj.GetComponent<Renderer>();

        if (rend != null){lastColor=rend.material.color;}

        hasStoredObject=true;
        storedFrame=Time.frameCount;
    }

    void Update(){
        bool spawnPressed=Input.GetButton("js3");

        if (spawnPressed && !IsGazingAtObject()){
            if (hasStoredObject){
                TrySpawnAtFloor();
            }
        }

        spawnWasPressed=spawnPressed;
    }

    bool IsGazingAtObject(){

        Ray ray=new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f)){
            return hit.collider.GetComponent<MakeAndSpawn>() != null;
        }
        return false;
    }


    void TrySpawnAtFloor(){

        Ray ray=new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer)){
            hasStoredObject=false;

            float halfHeight=lastScale.y / 2f;
            Vector3 spawnPos=hit.point+new Vector3(0, halfHeight, 0);

            GameObject newObj=GameObject.CreatePrimitive(lastType);
            newObj.transform.position=spawnPos;
            newObj.transform.localScale=lastScale;

            if (!string.IsNullOrEmpty(lastTag)){
                newObj.tag=lastTag;
	    }

            Renderer rend=newObj.GetComponent<Renderer>();

            if (rend != null){rend.material.color=lastColor;}

            Rigidbody rb=newObj.AddComponent<Rigidbody>();
            rb.isKinematic=false;
            rb.constraints=RigidbodyConstraints.FreezeRotation;

            if (lastTag=="Cube1"){
                newObj.AddComponent<Translate>();
	    } else if (lastTag=="Cube2"){
                newObj.AddComponent<Rotate>();
	    }

            newObj.AddComponent<HighlightUsingReticle>();
            newObj.AddComponent<MakeAndSpawn>();

            Outline outline=newObj.AddComponent<Outline>();
            outline.OutlineMode=Outline.Mode.OutlineVisible;
            outline.OutlineColor=Color.white;
            outline.OutlineWidth=5f;
            outline.enabled=false;
        }
    }
}