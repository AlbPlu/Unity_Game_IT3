using UnityEngine;

public class SealMovement : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    public Transform cam;
    
    float xRotation = 0f;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; 
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

        // 2. MOVING
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);
    }
}