using UnityEngine;

public class SealMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float slideSpeed = 15f;
    public float turnSpeed = 100f; // How fast the body rotates
    
    [Header("Seal Neck Constraints")]
    public float maxNeckAngle = 50f; // How far you can look before the body must turn
    public float mouseSensitivity = 100f;

    [Header("Slide Settings")]
    public float maxSlideTime = 2f; 
    public float slideCooldown = 1.5f;
    private float slideTimer;
    private float cooldownTimer;
    private bool isSliding = false;
    private Vector3 slideDirection;

    [Header("Bobbing Settings")]
    public float bobSpeed = 10f; 
    public float bobAmount = 0.05f;
    
    [Header("References")]
    public Transform playerBody; // The Capsule
    public Transform cam; // The Camera

    float xRotation = 0f; // Up/Down
    float yRotation = 0f; // Left/Right (Neck)
    float defaultY;
    float bobTimer = 0;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; 
        defaultY = cam.localPosition.y;
    }

    void Update()
    {
        // 1. NECK MOVEMENT (Mouse)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        // Horizontal neck movement
        yRotation += mouseX;
        
        // CLAMP NECK: You can't look 180 degrees behind you
        if (yRotation > maxNeckAngle) {
            float overflow = yRotation - maxNeckAngle;
            playerBody.Rotate(Vector3.up * overflow); // Body "chases" the look
            yRotation = maxNeckAngle;
        }
        else if (yRotation < -maxNeckAngle) {
            float overflow = yRotation + maxNeckAngle;
            playerBody.Rotate(Vector3.up * overflow);
            yRotation = -maxNeckAngle;
        }

        cam.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // 2. INPUTS
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S
        bool isMoving = Mathf.Abs(moveZ) > 0.1f || Mathf.Abs(moveX) > 0.1f;

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        // 3. SLIDE LOGIC
        HandleSliding(moveX, moveZ);

        // 4. APPLY MOVEMENT
        if (isSliding)
        {
            rb.linearVelocity = new Vector3(slideDirection.x * slideSpeed, rb.linearVelocity.y, slideDirection.z * slideSpeed);
            float smoothY = Mathf.Lerp(cam.localPosition.y, defaultY - 0.4f, Time.deltaTime * 10f);
            cam.localPosition = new Vector3(cam.localPosition.x, smoothY, cam.localPosition.z);
        }
        else
        {
            // PHYSICAL TURNING (A/D rotates the body instead of strafing)
            playerBody.Rotate(Vector3.up * moveX * turnSpeed * Time.deltaTime);

            // ONLY MOVE FORWARD/BACK (S is slower because seals aren't great at reversing)
            float finalSpeed = (moveZ < 0) ? speed * 0.5f : speed;
            Vector3 move = playerBody.forward * moveZ;
            rb.linearVelocity = new Vector3(move.x * finalSpeed, rb.linearVelocity.y, move.z * finalSpeed);

            // BOBBING (Sync bobbing to movement AND rotation)
            if (isMoving)
            {
                bobTimer += Time.deltaTime * bobSpeed;
                float bobY = defaultY + Mathf.Sin(bobTimer) * bobAmount;
                cam.localPosition = new Vector3(cam.localPosition.x, bobY, cam.localPosition.z);
                
                // Add a slight "wobble" to the camera when turning
                if (Mathf.Abs(moveX) > 0.1f) {
                    cam.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(bobTimer) * 2f);
                }
            }
            else
            {
                bobTimer = 0;
                float smoothY = Mathf.Lerp(cam.localPosition.y, defaultY, Time.deltaTime * 5f);
                cam.localPosition = new Vector3(cam.localPosition.x, smoothY, cam.localPosition.z);
            }
        }
    }

    void HandleSliding(float mx, float mz)
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSliding && cooldownTimer <= 0)
        {
            isSliding = true;
            slideTimer = maxSlideTime;
            slideDirection = playerBody.forward; // Always slide the way the body is facing
            yRotation = 0; // Snap head forward for the slide
        }

        if (isSliding)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > 5f) 
                {
                    slideTimer = maxSlideTime;
                    Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                    slideDirection = Vector3.Lerp(slideDirection, slopeDown, Time.deltaTime * 2f);
                    playerBody.forward = Vector3.Lerp(playerBody.forward, new Vector3(slideDirection.x, 0, slideDirection.z), Time.deltaTime * 2f);
                }
                else { slideTimer -= Time.deltaTime; }
            }
            if (slideTimer <= 0) { isSliding = false; cooldownTimer = slideCooldown; }
        }
    }
}