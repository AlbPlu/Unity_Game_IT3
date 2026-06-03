using UnityEngine;

public class PlayerWalking : MonoBehaviour
{
    [Header("References")]
    private CharacterController characterController;
    private AudioSource audioSource;

    [Header("Audio Settings")]
    [Tooltip("Drag your single footstep sound asset here.")]
    public AudioClip footstepSound;
    
    [Range(0f, 1f)] 
    public float footstepVolume = 0.4f;

    [Header("Pacing Settings")]
    [Tooltip("Time in seconds between each individual footstep.")]
    public float stepInterval = 0.5f;

    private float stepTimer;

    void Start()
    {
        // Automatically find the character controller on our player setup
        characterController = GetComponentInChildren<CharacterController>();
        
        // Dynamically add a clean AudioSource component so you don't have to add one manually
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.0f; // 2D flat sound so it sits perfectly inside the player's head
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Update()
    {
        // Don't play steps if the controller is missing or disabled (e.g., during menu or endings)
        if (characterController == null || !characterController.enabled) return;

        // Check if the player is pushing movement keys AND is physically touching the floor
        bool isMoving = characterController.velocity.magnitude > 0.1f && characterController.isGrounded;

        if (isMoving)
        {
            stepTimer -= Time.deltaTime;
            
            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval; // Reset the countdown timer
            }
        }
        else
        {
            // Reset with a tiny buffer when standing still.
            // This stops a step from playing instantly the exact millisecond you tap a key.
            stepTimer = 0.15f;
        }
    }

    void PlayFootstep()
    {
        if (footstepSound != null && audioSource != null)
        {
            // PlayOneShot handles clean overlapping audio without clipping issues
            audioSource.PlayOneShot(footstepSound, footstepVolume);
        }
    }
}