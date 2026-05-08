using UnityEngine;

public class SealMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float slideSpeed = 12f;
    public float mouseSensitivity = 100f;
    
    [Header("Bobbing Settings")]
    public float bobSpeed = 10f; 
    public float bobAmount = 0.05f;
    
    [Header("References")]
    public Transform playerBody;
    public Transform cam;

    float xRotation = 0f;
    float defaultY;
    float timer = 0;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; 
        // Remember the camera's starting height
        defaultY = cam.localPosition.y;
    }

    void Update()
    {
        // 1. LOOKING
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        // 2. MOVING & SLIDING
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        
        // Check if we are holding Shift to slide
        float currentSpeed = speed;
        float targetY = defaultY;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = slideSpeed;
            targetY = defaultY - 0.3f; // Drop down low for the slide
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        rb.linearVelocity = new Vector3(move.x * currentSpeed, rb.linearVelocity.y, move.z * currentSpeed);

        // 3. THE SEAL BOB & HEIGHT ADJUST
        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f)
        {
            // Player is moving - apply bobbing
            timer += Time.deltaTime * (Input.GetKey(KeyCode.LeftShift) ? bobSpeed * 1.5f : bobSpeed);
            float newY = targetY + Mathf.Sin(timer) * bobAmount;
            cam.localPosition = new Vector3(cam.localPosition.x, newY, cam.localPosition.z);
        }
        else
        {
            // Standing still - smooth reset
            timer = 0;
            cam.localPosition = new Vector3(cam.localPosition.x, Mathf.Lerp(cam.localPosition.y, targetY, Time.deltaTime * bobSpeed), cam.localPosition.z);
        }
    }
}