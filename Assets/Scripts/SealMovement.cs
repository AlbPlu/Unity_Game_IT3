using UnityEngine;

public class SealMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float slideSpeed = 12f;
    public float turnSpeed = 100f;

    [Header("Neck Hard-Clamp")]
    public float mouseSensitivity = 2f;
    [Range(10f, 90f)] 
    public float neckLimitAngle = 50f;
    public float headSmoothSpeed = 10f;

    [Header("Slide Settings")]
    public float maxSlideTime = 1.5f;
    public float slideCooldown = 5f;
    private float slideTimer;
    private float cooldownTimer;
    private bool isSliding = false;
    private Vector3 slideDirection;

    [Header("Geometric Arc (x^2 + y^2 = r^2)")]
    public float bobSpeed = 1.5f;     
    public float bobSmoothness = 8f;  
    public float arcRadius = 0.3f;   
    public float bobDelay = 0.4f;    
    
    private float arcX = 0f;         
    private float targetArcX = 0f;
    private bool isAscending = true; 
    private bool isWaiting = false;
    private float delayTimer = 0f;

    [Header("Clipping Prevention")]
    public float cameraForwardOffset = 0.4f;
    public float cameraSideSwingMod = 0.2f;

    [Header("UI Gauge Settings")]
    public RectTransform neckIndicator; 
    public float gaugeWidth = 100f; 

    [Header("References")]
    public Transform playerBody;
    public Transform cam;

    float yaw = 0f;   
    float pitch = 0f; 
    float defaultY;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
        defaultY = cam.localPosition.y;
    }

    void Update()
    {
        HandleNeckRotation();

        float moveX = Input.GetAxis("Horizontal"); 
        float moveZ = Input.GetAxis("Vertical");   
        bool isInputting = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f;

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        HandleSliding(moveX, moveZ);

        if (isSliding)
        {
            rb.linearVelocity = new Vector3(slideDirection.x * slideSpeed, rb.linearVelocity.y, slideDirection.z * slideSpeed);
            cam.localPosition = Vector3.Lerp(cam.localPosition, new Vector3(0, defaultY - 0.4f, cameraForwardOffset), Time.deltaTime * 10f);
        }
        else
        {
            if (isInputting && !isWaiting)
            {
                // Progress the target
                if (isAscending) 
                    targetArcX += Time.deltaTime * bobSpeed;
                else 
                    targetArcX -= Time.deltaTime * bobSpeed;

                // Clamp target strictly
                targetArcX = Mathf.Clamp(targetArcX, 0f, arcRadius);

                // Smoothly follow the target
                arcX = Mathf.Lerp(arcX, targetArcX, Time.deltaTime * bobSmoothness);

                // y = sqrt(r^2 - x^2)
                float yOffset = Mathf.Sqrt(Mathf.Max(0, (arcRadius * arcRadius) - (arcX * arcX)));

                // Pulse movement: Shove forward when camera is lowest (arcX near 0)
                float movementFactor = Mathf.Clamp01(1f - (arcX / arcRadius));
                
                playerBody.Rotate(Vector3.up * moveX * turnSpeed * movementFactor * Time.deltaTime);
                Vector3 moveDir = playerBody.forward * moveZ * (moveZ < 0 ? speed * 0.5f : speed) * movementFactor;
                rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.z);

                // Update Camera Position
                float dynamicZ = cameraForwardOffset + (Mathf.Abs(yaw / neckLimitAngle) * cameraSideSwingMod);
                cam.localPosition = new Vector3(arcX, defaultY + (arcRadius - yOffset), dynamicZ);

                // Check for turnarounds
                if (isAscending && targetArcX >= arcRadius) 
                {
                    isAscending = false; 
                }
                else if (!isAscending && targetArcX <= 0f && arcX < 0.01f) // Ensure the Lerp is also basically done
                {
                    // FINISH STEP
                    isAscending = true;
                    isWaiting = true;
                    delayTimer = bobDelay;
                    
                    // FORCE HARD RESET to prevent snapping on next start
                    targetArcX = 0;
                    arcX = 0; 
                    rb.linearVelocity = Vector3.zero;
                }
            }
            else if (isWaiting)
            {
                delayTimer -= Time.deltaTime;
                rb.linearVelocity = Vector3.zero;
                if (delayTimer <= 0) 
                {
                    isWaiting = false;
                    // Double check reset state
                    arcX = 0;
                    targetArcX = 0;
                }
            }
            else
            {
                // IDLE: Cleanly return to center
                rb.linearVelocity = Vector3.zero;
                targetArcX = 0f;
                arcX = Mathf.MoveTowards(arcX, 0f, Time.deltaTime * bobSpeed);
                
                float dynamicZ = cameraForwardOffset + (Mathf.Abs(yaw / neckLimitAngle) * cameraSideSwingMod);
                cam.localPosition = Vector3.MoveTowards(cam.localPosition, new Vector3(0, defaultY, dynamicZ), Time.deltaTime * bobSpeed);
            }

            float tilt = isInputting ? -moveX * 4f : 0;
            cam.localRotation = Quaternion.Slerp(cam.localRotation, Quaternion.Euler(pitch, yaw, tilt), Time.deltaTime * headSmoothSpeed);
        }

        if (neckIndicator != null)
        {
            float normalizedYaw = yaw / neckLimitAngle;
            neckIndicator.anchoredPosition = new Vector2(normalizedYaw * gaugeWidth, neckIndicator.anchoredPosition.y);
        }
    }

    void HandleNeckRotation()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        float currentLimit = isSliding ? 15f : neckLimitAngle;
        yaw = Mathf.Clamp(yaw, -currentLimit, currentLimit);
        pitch = Mathf.Clamp(pitch, -30f, 30f);
    }

    void HandleSliding(float mx, float mz)
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && cooldownTimer <= 0)
        {
            isSliding = true; slideTimer = maxSlideTime; slideDirection = playerBody.forward; 
        }
        if (isSliding)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > 5f) {
                    slideTimer = maxSlideTime;
                    Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                    slideDirection = Vector3.Lerp(slideDirection, slopeDown, Time.deltaTime * 2f);
                    playerBody.forward = Vector3.Lerp(playerBody.forward, new Vector3(slideDirection.x, 0, slideDirection.z), Time.deltaTime * 2f);
                }
                else slideTimer -= Time.deltaTime;
            }
            if (slideTimer <= 0) { isSliding = false; cooldownTimer = slideCooldown; }
        }
    }
}