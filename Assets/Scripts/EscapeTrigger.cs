using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EscapeTrigger : MonoBehaviour
{
    [Header("Abyss Height Trigger")]
    [Tooltip("The negative Y coordinate where the cutscene takes over.")]
    public float escapeHeightThreshold = -5f;

    [Header("The Independent Movie Set")]
    public GameObject abyssCutsceneSet;
    public AudioSource cutsceneAudioSource;

    [Header("Gameplay Camera to Track & Disable")]
    [Tooltip("Drag your player's moving Main Camera here. The script will monitor THIS object's height!")]
    public GameObject gameplayCamera; 

    [Header("UI Victory Screens (Optional)")]
    public GameObject victoryScreenPanel;
    public TextMeshProUGUI timeDisplayMesh;

    private MazeGridGenerator gridManager;
    private bool cutsceneStarted = false;

    void Start()
    {
        gridManager = FindObjectOfType<MazeGridGenerator>();
        if (victoryScreenPanel != null) victoryScreenPanel.SetActive(false);
    }

    void Update()
    {
        // 1. Safety check to make sure you dragged the camera into the slot
        if (gameplayCamera != null && !cutsceneStarted)
        {
            // 2. Track the ACTUAL moving camera's Y position in the world!
            if (gameplayCamera.transform.position.y <= escapeHeightThreshold)
            {
                StartCoroutine(SwitchToForcedCutscene());
            }
        }

        // Quick restart testing shortcut (Only works after cutscene begins)
        if (cutsceneStarted && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    IEnumerator SwitchToForcedCutscene()
    {
        cutsceneStarted = true;

        // 1. Turn on the cinematic movie set
        if (abyssCutsceneSet != null)
        {
            abyssCutsceneSet.SetActive(true);
        }

        // 2. Play the scream sound
        if (cutsceneAudioSource != null)
        {
            cutsceneAudioSource.volume = 0.6f; 
            cutsceneAudioSource.Play();
        }

        // 3. Turn off the gameplay camera so the player loses control/vision
        if (gameplayCamera != null)
        {
            gameplayCamera.SetActive(false);
        }

        // 4. Wait out the frontflip animation sequence
        yield return new WaitForSeconds(2.5f);

        // 5. Smoothly fade the sound away
        if (cutsceneAudioSource != null)
        {
            float fadeDuration = 1.0f;
            float startVolume = cutsceneAudioSource.volume;
            while (cutsceneAudioSource.volume > 0)
            {
                cutsceneAudioSource.volume -= startVolume * (Time.deltaTime / fadeDuration);
                yield return null;
            }
            cutsceneAudioSource.Stop();
        }

        // 6. Calculate times from your generator
        if (gridManager != null)
        {
            float totalSeconds = gridManager.survivalTimer;
            int minutes = Mathf.FloorToInt(totalSeconds / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);
            
            if (timeDisplayMesh != null)
            {
                timeDisplayMesh.text = string.Format("Escape Time: {0:00}:{1:00}", minutes, seconds);
            }
        }

        // Unlock mouse cursor cleanly
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 7. Turn on the victory screen panel
        if (victoryScreenPanel != null)
        {
            victoryScreenPanel.SetActive(true);
        }
    }
}