using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 10f;
    public Camera gazeCamera;

    private CharacterController cc;
    private bool movementEnabled = true;
    private float verticalVelocity = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.skinWidth = 0.01f;
    }

    void Update()
    {
        if (cc.isGrounded)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        Vector3 move = Vector3.zero;

        if (movementEnabled && (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)){
            Vector3 forward = gazeCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = gazeCamera.transform.right;
            right.y = 0;
            right.Normalize();

            move = (forward * Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal")) * moveSpeed;
        }

        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);
    }

    public void SetMovementEnabled(bool enabled) { 
        movementEnabled = enabled; 
    }
    public void SetSpeed(float speed) { 
        moveSpeed = speed; 
    }
}