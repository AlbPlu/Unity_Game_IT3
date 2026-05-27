using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 5.5f;
    public float gravity = 9.81f;

    [Header("Look Sensitivity")]
    public float mouseSensitivity = 2.0f;
    public float upDownRange = 80.0f;

    private CharacterController characterController;
    private Camera playerCamera;
    
    private float rotationX = 0.0f;
    private Vector3 moveDirection = Vector3.zero;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. CAMERA ROTATION (LOOKING AROUND)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Look up/down (clamped so you don't flip upside down)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -upDownRange, upDownRange);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // 2. PLAYER MOVEMENT
        // Check if player is holding Shift to run
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Get keyboard inputs (WASD / Arrow Keys)
        float forwardSpeed = Input.GetAxis("Vertical") * currentSpeed;
        float sideSpeed = Input.GetAxis("Horizontal") * currentSpeed;

        // Calculate direction relative to where the player is facing
        Vector3 speedMovement = (transform.forward * forwardSpeed) + (transform.right * sideSpeed);

        // Apply horizontal movement and preserve gravity
        moveDirection.x = speedMovement.x;
        moveDirection.z = speedMovement.z;

        // 3. APPLY GRAVITY
        if (characterController.isGrounded)
        {
            moveDirection.y = -0.5f; // Slight downward force to keep grounded securely
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime; // Fall down
        }

        // Final execution step moves the player securely handling all collisions
        characterController.Move(moveDirection * Time.deltaTime);
    }
}