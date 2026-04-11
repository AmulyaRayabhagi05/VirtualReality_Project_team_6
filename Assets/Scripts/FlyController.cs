using UnityEngine;
using UnityEngine.SceneManagement;

public class FlyController : MonoBehaviour
{
    private Camera mainCamera;

    public enum InputMode
    {
        MouseOnly,          
        GyroscopePhone,     
        AccelerometerPhone, 
        Auto                
    }

    [Header("Input Mode")]
    public InputMode inputMode = InputMode.Auto;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMultiplier = 3f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Tilt")]
    [Tooltip("How strongly the phone tilt changes the view rotation")]
    public float gyroSensitivity = 1f;
    [Range(1f, 20f)]
    public float gyroSmoothing= 8f;
    public float tiltDeadzone = 5f;

    [Header("Screen Tilt Visual Roll")]
    public float rollIntensity = 15f;  
    [Range(1f, 20f)]
    public float rollSmoothing = 6f;
    [Header("Height Limit")]
    public float maxHeight = 500f;

    private InputMode activeMode;
    private Quaternion gyroOffset= Quaternion.identity;
    private Quaternion  targetRotation;
    private float currentRoll = 0f;
    
    private float pitch = 0f;
    private float yaw   = 0f;

    void Start()
    {
        targetRotation = transform.rotation;
        pitch = transform.eulerAngles.x;
        yaw = transform.eulerAngles.y;
        mainCamera = Camera.main;

        activeMode = ResolveInputMode();
        cameraTransform = Camera.main.transform;
        
        if (activeMode ==InputMode.GyroscopePhone)
        {
            Input.gyro.enabled = true;
            CalibrateGyro();
        }

    }

    public bool IsDead {get; set;} = false;

    void Update()
    {
        if (IsDead) return;
        switch (activeMode)
        {
            case InputMode.GyroscopePhone: UpdateGyroscope(); break;
            case InputMode.AccelerometerPhone: UpdateAccelerometer(); break;
            default: UpdateMouseLook(); break;
        }

        HandleMovement();
    }
    void LateUpdate()
    {
        if (IsDead) {
		return;
	}
        
	ApplyVisualRoll();
    }


    InputMode ResolveInputMode()
    {
        if (inputMode != InputMode.Auto) {
		return inputMode;
	}

        if (SystemInfo.supportsGyroscope){
		return InputMode.GyroscopePhone;
        }

	if (SystemInfo.supportsAccelerometer)     
	{
		return InputMode.AccelerometerPhone;
        }
	return InputMode.MouseOnly;
    }

    public void CalibrateGyro()
    {
        gyroOffset = GyroToUnity(Input.gyro.attitude);
    }

    private static FlyController instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else{
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (cameraTransform == null || cameraTransform != Camera.main.transform){
            cameraTransform = Camera.main.transform;
	}
        activeMode = ResolveInputMode();

        if (activeMode == InputMode.GyroscopePhone)
        {
            Input.gyro.enabled = true;
            CalibrateGyro();
        }

        GameObject[] planeViews = GameObject.FindGameObjectsWithTag("PlaneControl");
        for (int i = 1; i < planeViews.Length; i++){
            Destroy(planeViews[i]);
	}
    }



    public void DisableInput()
    {
        IsDead = true;
        Input.gyro.enabled = false;
    }

    public void EnableInput()
    {
        IsDead = false;
        if (activeMode == InputMode.GyroscopePhone)
        {
            Input.gyro.enabled = true;
            CalibrateGyro();
        }
    }

    void UpdateGyroscope()
    {
        Quaternion rawGyro = GyroToUnity(Input.gyro.attitude);
        Quaternion relative= Quaternion.Inverse(gyroOffset) * rawGyro;

        targetRotation = Quaternion.Slerp(targetRotation, relative, Time.deltaTime * gyroSmoothing);
        transform.rotation = targetRotation;
    }

    static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
   
    void UpdateAccelerometer()
    {
        Vector3 accel = Input.acceleration;

        float tiltX = Mathf.Clamp(accel.y * 90f, -90f, 90f); 
        float tiltZ = Mathf.Clamp(-accel.x * 90f, -90f, 90f);

        if (Mathf.Abs(tiltX) < tiltDeadzone) {
		tiltX = 0f;
        }

	if (Mathf.Abs(tiltZ) < tiltDeadzone) {
		tiltZ = 0f;
	}

        Quaternion target = Quaternion.Euler(tiltX * gyroSensitivity, yaw, tiltZ * gyroSensitivity);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * gyroSmoothing);
    }



    void UpdateMouseLook()
    {
        if (!Input.GetMouseButton(1)){ return;}

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }


    void ApplyVisualRoll()
    {

        float lateralInput = 0f;

        if (Input.GetKey(KeyCode.D)){ lateralInput = 1f;}
        if (Input.GetKey(KeyCode.A)) {lateralInput = -1f;}

        float joystickH = Input.GetAxis("Horizontal");
        if (Mathf.Abs(joystickH) > 0.1f){
            lateralInput = joystickH;
	}

        if (activeMode == InputMode.GyroscopePhone || activeMode == InputMode.AccelerometerPhone){
            lateralInput = Mathf.Clamp(-Input.acceleration.x * 2f, -1f, 1f);
	}

        float targetRoll = -lateralInput * rollIntensity;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmoothing);

        if (cameraTransform != null)
        {
            Vector3 currentEuler = cameraTransform.localEulerAngles;
            cameraTransform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentRoll);
        }else{
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, currentRoll);
        }
    }

    void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * fastMultiplier : moveSpeed;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        if (Input.GetKey(KeyCode.W)){ 
		moveDirection += camForward;
	}
        if (Input.GetKey(KeyCode.S)){ 
		moveDirection -= camForward;
        }
	if (Input.GetKey(KeyCode.D)){ 
		moveDirection += camRight;
        }
	if (Input.GetKey(KeyCode.A)){ 
		moveDirection -= camRight;
	}

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveDirection += camForward * vertical;
        moveDirection += camRight * horizontal;

        if (Input.GetButton("js0")) {moveDirection += Vector3.up;}
        if (Input.GetButton("js1")) {moveDirection -= Vector3.up;}

        if (moveDirection.magnitude > 1f) {moveDirection.Normalize();}

        transform.position += moveDirection * speed * Time.deltaTime;

        if (transform.position.y > maxHeight){
            transform.position = new Vector3(transform.position.x, maxHeight, transform.position.z);
	}
    }
}
