using UnityEngine;
using System.Collections;
using TMPro;

public class EscapeTrigger : MonoBehaviour
{
    [Header("Abyss Height Trigger")]
    public float escapeHeightThreshold = -5f;

    [Header("The Independent Movie Set")]
    public GameObject abyssCutsceneSet;
    public AudioSource cutsceneAudioSource;

    [Header("Gameplay Camera to Track & Disable")]
    public GameObject gameplayCamera; 

    [Header("UI Victory Screens")]
    [Tooltip("Keep your canvas panel linked here so it turns on for the 5-second countdown!")]
    public GameObject victoryScreenPanel;
    public TextMeshProUGUI timeDisplayMesh;

    private MazeGridGenerator gridManager;
    private bool cutsceneStarted = false;

    void Start()
    {
        gridManager = FindObjectOfType<MazeGridGenerator>();
        
        // Ensure everything is default on boot
        if (victoryScreenPanel != null) victoryScreenPanel.SetActive(false);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);
        if (abyssCutsceneSet != null) abyssCutsceneSet.SetActive(false);
        cutsceneStarted = false;
    }

    void Update()
    {
        // Tracking the moving camera height
        if (gameplayCamera != null && !cutsceneStarted)
        {
            if (gameplayCamera.transform.position.y <= escapeHeightThreshold)
            {
                StartCoroutine(SwitchToForcedCutscene());
            }
        }
    }

    IEnumerator SwitchToForcedCutscene()
    {
        cutsceneStarted = true;

        // 1. Turn on cinematic movie set
        if (abyssCutsceneSet != null) abyssCutsceneSet.SetActive(true);

        // 2. Play scream
        if (cutsceneAudioSource != null)
        {
            cutsceneAudioSource.volume = 0.6f; 
            cutsceneAudioSource.Play();
        }

        // 3. Turn off gameplay vision
        if (gameplayCamera != null) gameplayCamera.SetActive(false);

        // 4. Wait out frontflip animation sequence
        yield return new WaitForSeconds(2.5f);

        // 5. Fade out sound smoothly
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

        // 6. Grab the score/time safely from the maze generator
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

        // 7. Turn on your clean black victory screen panel
        if (victoryScreenPanel != null)
        {
            victoryScreenPanel.SetActive(true);
        }

        // This pauses the script for exactly 5 seconds, leaving the 
        // victory text perfectly frozen on the screen for the player.
        yield return new WaitForSeconds(5.0f);

        // 8. AUTOMATICALLY SHUT DOWN THE GAME
        CloseGameApplication();
    }

    void CloseGameApplication()
    {
        Debug.Log("5 seconds are up! Shutting down the game completely.");
        
        // Closes a built desktop (.exe) game application cleanly
        Application.Quit();
        
        #if UNITY_EDITOR
        // Safely stops the simulator inside the Unity Editor window
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}