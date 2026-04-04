using UnityEngine;
using UnityEngine.SpatialTracking; // add this at the top


[RequireComponent(typeof(FlyController))]
public class CollisionDeathHandler : MonoBehaviour
{
    [Header("References")]
    public DeathUIManager deathUI;
    public HapticManager hapticManager;

    [Header("Crash Haptic Settings")]
    public float vibrationDuration = 0.8f;
    [Range(0f, 1f)]
    public float vibrationIntensity = 1f;

    [Header("Ignore Layers")]
    [Tooltip("Objects on these layers will NOT trigger death (e.g. ground, UI, the player itself)")]
    public LayerMask ignoreLayers;

    [Header("Bounce Detection")]
    [Tooltip("If position barely moves despite input, this many frames in a row = crash")]
    public float stuckThreshold = 0.01f;   // metres: if moved less than this per frame
    public int stuckFramesNeeded = 3;        // consecutive stuck frames before death triggers

    [Header("Mesh Collision Detection")]
    public float raycastDistance = 1.5f;  // how close before death triggers
    public LayerMask mountainLayer;        // set this to your mountain layer

    private FlyController flyController;
    private CharacterController characterController;
    private Rigidbody rb;
    private bool isDead = false;
    private int stuckFrames = 0;
    private Vector3 lastPosition;
    private TrackedPoseDriver trackedPoseDriver;
    private MonoBehaviour characterMovement;

    // Spawn point — saved at Start so Try Again resets here
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    
    void CheckMeshCollision()
    {
        // Cast rays in multiple directions around the player
        Vector3[] directions = new Vector3[]
        {
        transform.forward,
        -transform.forward,
        transform.right,
        -transform.right,
        transform.up,
        -transform.up,
        // Diagonals
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
                Debug.Log($"[RaycastCollision] Hit: {hit.collider.gameObject.name} " +
                         $"| Distance: {hit.distance} " +
                         $"| Direction: {dir}");
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

        
        // Save starting position and rotation for restart
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        lastPosition = transform.position;
    }

    void Update()
    {
        if (isDead) return;
        if (!flyController.enabled) return;
        if (!characterController.enabled) return;
        characterMovement.enabled = false;

        CheckMeshCollision();
        lastPosition = transform.position;
    }


    // Also keep OnCollisionEnter as a backup for any convex colliders in the scene
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Collision] Hit: {collision.gameObject.name} " +
                  $"| Layer: {collision.gameObject.layer} " +
                  $"| Tag: {collision.gameObject.tag} " +
                  $"| Has MeshCollider: {collision.gameObject.GetComponent<MeshCollider>() != null} " +
                  $"| Contact point: {collision.contacts[0].point} " +
                  $"| Relative velocity: {collision.relativeVelocity.magnitude}");

        if (isDead) return;
        if ((ignoreLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            Debug.Log($"[Collision] IGNORED - {collision.gameObject.name} is on ignored layer");
            return;
        }

        Debug.Log($"[Collision] DEATH TRIGGERED by {collision.gameObject.name}");
        TriggerDeath();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Trigger] Entered: {other.gameObject.name} " +
                  $"| Layer: {other.gameObject.layer} " +
                  $"| Has MeshCollider: {other.gameObject.GetComponent<MeshCollider>() != null}");
    }


    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        

        Quaternion frozenRotation = transform.rotation;

        // Disable TrackedPoseDriver FIRST before anything else
        if (trackedPoseDriver != null) trackedPoseDriver.enabled = false;

        flyController.DisableInput();
        flyController.enabled = false;       // stops ALL Update() in FlyController
        characterController.enabled = false;


        // Re-apply frozen rotation AFTER disabling everything
        transform.rotation = frozenRotation;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        hapticManager?.TriggerVibration(vibrationDuration, vibrationIntensity);

        if (deathUI != null)
            deathUI.ShowDeathScreen();
    }

    public void Restart()
    {
        isDead = false;
        stuckFrames = 0;
        if (trackedPoseDriver != null) trackedPoseDriver.enabled = true;
        
        // Reset position and rotation to spawn point
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        lastPosition = spawnPosition;

        // Re-enable physics and flying
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (characterMovement != null)
            characterMovement.enabled = false;

        flyController.enabled = true;
        flyController.EnableInput();
        characterController.enabled = true;
        deathUI?.HideDeathScreen();

        Debug.Log("[CollisionDeathHandler] Restarted at spawn position.");
    }
}