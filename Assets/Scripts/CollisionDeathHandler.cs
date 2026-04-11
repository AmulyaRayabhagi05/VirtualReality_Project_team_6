using UnityEngine;
using UnityEngine.SpatialTracking;

[RequireComponent(typeof(FlyController))]
public class CollisionDeathHandler : MonoBehaviour
{
    [Header("References")]
    public DeathUIManager deathUI;
    public HapticManager hapticManager;

    [Header("Crash Haptic")]
    public float vibrationDuration = 0.8f;
    [Range(0f, 1f)]
    public float vibrationIntensity = 1f;

    [Header("Ignore Layer")]
    public LayerMask ignoreLayers;

    [Header("Bounce Detection")]
    public float stuckThreshold = 0.01f;
    public int stuckFramesNeeded = 3;   

    [Header("Mesh Collision")]
    public float raycastDistance = 1.5f; 
    public LayerMask mountainLayer;      

    private FlyController flyController;
    private CharacterController characterController;
    private Rigidbody rb;
    private bool isDead = false;
    private int stuckFrames = 0;
    private Vector3 lastPosition;
    private TrackedPoseDriver trackedPoseDriver;
    private MonoBehaviour characterMovement;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    
    void CheckMeshCollision()
    {
        Vector3[] directions = new Vector3[]
        {
        transform.forward,
        -transform.forward,
        transform.right,
        -transform.right,
        transform.up,
        -transform.up,

        (transform.forward + transform.right).normalized,
        (transform.forward - transform.right).normalized,
        (-transform.forward + transform.right).normalized,
        (-transform.forward - transform.right).normalized,
        };

        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir, out hit, raycastDistance, mountainLayer))
            {   
                Debug.Log("Raycast Hit: {hit.collider.gameObject.name} | Distance {hit.distance} | Direction: {dir}");
                TriggerDeath();
                return;
            }
        }
    }
    void Start()
    {
        flyController = GetComponent<FlyController>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        trackedPoseDriver = GetComponentInChildren<TrackedPoseDriver>();
        characterMovement = GetComponent<CharacterMovement>();

        
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (isDead) {
		return;
        }
	if (!flyController.enabled){ 
		return;
        }
	if (!characterController.enabled){ 
		return;
        }

	characterMovement.enabled = false;
        CheckMeshCollision();
        lastPosition = transform.position;
    }


    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision Hit: {collision.gameObject.name}| Layer: {collision.gameObject.layer} | Tag: {collision.gameObject.tag} | Has MeshCollider: {collision.gameObject.GetComponent<MeshCollider>() != null} |  Contact point: {collision.contacts[0].point}| Relative velocity:{collision.relativeVelocity.magnitude}");

        if (isDead) {
		return;
	}
	
        if ((ignoreLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            Debug.Log("Collision ignored: {collision.gameObject.name} is being ignored");
            return;
        }

        Debug.Log("Collision death by {collision.gameObject.name}");
        TriggerDeath();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered: {other.gameObject.name} | Layer: {other.gameObject.layer}| Has MeshCollider: {other.gameObject.GetComponent<MeshCollider>() != null}");
    }


    public void TriggerDeath()
    {
        if (isDead) {
		return;
	}

        isDead = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        

        Quaternion frozenRotation = transform.rotation;

        if (trackedPoseDriver != null) {
		trackedPoseDriver.enabled = false;
	}
        flyController.DisableInput();
        flyController.enabled = false;    
        characterController.enabled = false;


        transform.rotation = frozenRotation;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        hapticManager?.TriggerVibration(vibrationDuration, vibrationIntensity);

        if (deathUI != null){
            deathUI.ShowDeathScreen();
	}
    }

    public void Restart()
    {
        isDead = false;
        stuckFrames = 0;

        if (trackedPoseDriver != null){
            trackedPoseDriver.enabled = true;
	}
        
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        lastPosition = spawnPosition;

        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        flyController.enabled = true;
        flyController.EnableInput();
        flyController.CalibrateGyro();

        characterController.enabled = true;
        if (characterMovement != null){
            characterMovement.enabled = true;
	}

        deathUI?.HideDeathScreen();

        Debug.Log(" Restarted at spawn position");
    }
}