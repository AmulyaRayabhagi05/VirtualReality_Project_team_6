using UnityEngine;

/// <summary>
/// Fly controller with phone IMU (gyroscope/accelerometer) tilt support.
///
/// CONTROLS:
///   Phone tilt          — look/steer direction (gyroscope if available, accelerometer fallback)
///   W / S  or  Swipe    — move forward / backward
///   A / D               — strafe left / right
///   Q / E               — fly down / up
///   Right Mouse (editor)— look around with mouse (editor testing only)
///   Left Shift          — speed boost
///
/// SETUP:
///   Attach to your player GameObject (with Main Camera as a child, or directly on camera).
///   In Inspector, set Input Mode to match your device.
/// </summary>
public class FlyController : MonoBehaviour
{
    private Camera mainCamera;

    // ── Enums ────────────────────────────────────────────────────────
    public enum InputMode
    {
        MouseOnly,          // Editor/desktop — right-click to look
        GyroscopePhone,     // Phone gyroscope (most accurate, 6DOF)
        AccelerometerPhone, // Phone accelerometer (tilt only, no spin)
        Auto                // Tries gyroscope first, falls back to accelerometer, then mouse
    }

    // ── Inspector ────────────────────────────────────────────────────
    [Header("Input Mode")]
    public InputMode inputMode = InputMode.Auto;

    [Header("Movement")]
    public float moveSpeed      = 10f;
    public float fastMultiplier = 3f;

    [Header("Mouse Look (editor)")]
    public float mouseSensitivity = 2f;

    [Header("Camera Reference")]
    public Transform cameraTransform; // drag Main Camera in here

    [Header("Gyroscope / Tilt")]
    [Tooltip("How strongly the phone tilt affects the view rotation")]
    public float gyroSensitivity   = 1f;
    [Tooltip("Smoothing — lower = snappier, higher = smoother")]
    [Range(1f, 20f)]
    public float gyroSmoothing     = 8f;
    [Tooltip("Tilt the phone to this angle (degrees) to reach max look angle")]
    public float tiltDeadzone      = 5f;    // ignore tiny wobbles

    [Header("Screen Tilt Visual Roll")]
    [Tooltip("How much the camera rolls (tilts sideways) when you bank left/right")]
    public float rollIntensity     = 15f;   // degrees of visual roll
    [Range(1f, 20f)]
    public float rollSmoothing     = 6f;
    [Header("Height Limit")]
    public float maxHeight = 500f;

    // ── Private ──────────────────────────────────────────────────────
    private InputMode   activeMode;
    private Quaternion  gyroOffset      = Quaternion.identity; // calibration offset
    private Quaternion  targetRotation;
    private float       currentRoll     = 0f;

    // Mouse look fallback
    private float pitch = 0f;
    private float yaw   = 0f;

    // ── Unity ────────────────────────────────────────────────────────

    void Start()
    {
        targetRotation = transform.rotation;
        pitch = transform.eulerAngles.x;
        yaw   = transform.eulerAngles.y;
        mainCamera = Camera.main;

        activeMode = ResolveInputMode();

        if (activeMode == InputMode.GyroscopePhone)
        {
            Input.gyro.enabled = true;
            CalibrateGyro();
        }

        Debug.Log($"[FlyController] Active input mode: {activeMode}");
    }

    public bool IsDead { get; set; } = false;

    void Update()
    {
        if (IsDead) return;
        switch (activeMode)
        {
            case InputMode.GyroscopePhone:     UpdateGyroscope();     break;
            case InputMode.AccelerometerPhone: UpdateAccelerometer(); break;
            default:                           UpdateMouseLook();     break;
        }

        HandleMovement();
        ApplyVisualRoll();
    }
    void LateUpdate()
    {
        if (IsDead) return; // nothing moves after this
    }

    // ── Input Mode Resolution ─────────────────────────────────────────

    InputMode ResolveInputMode()
    {
        if (inputMode != InputMode.Auto) return inputMode;

        if (SystemInfo.supportsGyroscope)         return InputMode.GyroscopePhone;
        if (SystemInfo.supportsAccelerometer)     return InputMode.AccelerometerPhone;
        return InputMode.MouseOnly;
    }

    // ── Gyroscope ────────────────────────────────────────────────────

    /// <summary>Call this to re-zero the gyro to current phone orientation.</summary>
    public void CalibrateGyro()
    {
        // Capture current gyro attitude as the "forward" reference
        gyroOffset = GyroToUnity(Input.gyro.attitude);
        Debug.Log("[FlyController] Gyro calibrated.");
    }

    public void DisableInput()
    {
        IsDead = true;
        Input.gyro.enabled = false;  // kills the hardware feed
    }

    public void EnableInput()
    {
        IsDead = false;
        if (activeMode == InputMode.GyroscopePhone)
        {
            Input.gyro.enabled = true;
            CalibrateGyro(); // re-zero so player doesn't snap on restart
        }
    }

    void UpdateGyroscope()
    {
        // Convert gyro quaternion from right-handed (gyro) to left-handed (Unity)
        Quaternion rawGyro    = GyroToUnity(Input.gyro.attitude);
        Quaternion relative   = Quaternion.Inverse(gyroOffset) * rawGyro;

        // Smooth towards target
        targetRotation = Quaternion.Slerp(targetRotation, relative, Time.deltaTime * gyroSmoothing);
        transform.rotation = targetRotation;
    }

    // Converts Unity's Input.gyro.attitude (right-handed) to Unity's left-handed space
    static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    // ── Accelerometer (tilt only) ─────────────────────────────────────
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[FlyController Collision] Hit: {collision.gameObject.name} " +
                  $"| Layer: {collision.gameObject.layer} " +
                  $"| Has MeshCollider: {collision.gameObject.GetComponent<MeshCollider>() != null}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[FlyController Trigger] Entered: {other.gameObject.name} " +
                  $"| Layer: {other.gameObject.layer}");
    }

    void UpdateAccelerometer()
    {
        Vector3 accel = Input.acceleration;

        // accel.x = left/right tilt,  accel.y = forward/back tilt (portrait)
        float tiltX = Mathf.Clamp(accel.y * 90f, -90f, 90f);   // pitch (up/down)
        float tiltZ = Mathf.Clamp(-accel.x * 90f, -90f, 90f);  // roll  (bank)

        // Apply deadzone
        if (Mathf.Abs(tiltX) < tiltDeadzone) tiltX = 0f;
        if (Mathf.Abs(tiltZ) < tiltDeadzone) tiltZ = 0f;

        Quaternion target = Quaternion.Euler(tiltX * gyroSensitivity, yaw, tiltZ * gyroSensitivity);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * gyroSmoothing);
    }

    // ── Mouse Look (editor / desktop fallback) ───────────────────────

    void UpdateMouseLook()
    {
        if (!Input.GetMouseButton(1)) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // Rotate character left/right (yaw)
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Rotate CAMERA up/down (pitch) separately
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // ── Visual Roll (banking effect) ─────────────────────────────────

    void ApplyVisualRoll()
    {
        // Only use keyboard for roll, not accelerometer in editor
        float lateralInput = 0f;

        if (Input.GetKey(KeyCode.D)) lateralInput = 1f;
        if (Input.GetKey(KeyCode.A)) lateralInput = -1f;

        // On phone, use accelerometer X for roll
        if (activeMode == InputMode.GyroscopePhone || activeMode == InputMode.AccelerometerPhone)
            lateralInput = Mathf.Clamp(-Input.acceleration.x * 2f, -1f, 1f);

        float targetRoll = -lateralInput * rollIntensity;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmoothing);

        // Apply roll to CAMERA only, keeping mouse pitch/yaw on character
        if (cameraTransform != null)
        {
            // Preserve existing camera local rotation, just add roll on top
            Vector3 currentEuler = cameraTransform.localEulerAngles;
            cameraTransform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentRoll);
        }
        else
        {
            // Fallback — apply to character if no camera assigned
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, currentRoll);
        }
    }

    // ── Movement ─────────────────────────────────────────────────────

    void HandleMovement()
    {
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * fastMultiplier : moveSpeed;

        Vector3 dir = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) dir += camForward;
        if (Input.GetKey(KeyCode.S)) dir -= camForward;
        if (Input.GetKey(KeyCode.A)) dir -= camRight;
        if (Input.GetKey(KeyCode.D)) dir += camRight;
        if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) dir -= Vector3.up;

        // Calculate next position
        Vector3 newPosition = transform.position + dir * speed * Time.deltaTime;

        // Clamp Y to height limit
        if (newPosition.y > maxHeight)
        {
            newPosition.y = maxHeight;
            Debug.Log($"[HeightLimit] Ceiling reached at Y: {maxHeight}");
        }

        transform.position = newPosition;
    }
}
