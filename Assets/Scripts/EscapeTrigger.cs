using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// NOTE: I changed the class name below to 'EscapeTrigger' so it perfectly 
// matches your Unity file name 'EscapeTrigger.cs'. This will fix your compilation!
public class EscapeTrigger : MonoBehaviour
{
    [Header("Abyss Height Trigger")]
    [Tooltip("The negative Y coordinate where the cutscene takes over.")]
    public float escapeHeightThreshold = -5f;

    [Header("The Independent Movie Set")]
    [Tooltip("Drag your Abyss_Cutscene_Set object here.")]
    public GameObject abyssCutsceneSet;
    
    [Tooltip("Drag your Cutscene_Player_Dummy object here so the script can play its sound directly!")]
    public AudioSource cutsceneAudioSource;

    [Header("UI Victory Screens (Optional)")]
    [Tooltip("Drag your Victory_Screen panel here.")]
    public GameObject victoryScreenPanel;
    [Tooltip("Drag your TimeDisplayText UI element here.")]
    public Text timeDisplayMesh;

    private MazeGridGenerator gridManager;
    private bool cutsceneStarted = false;

    void Start()
    {
        gridManager = FindObjectOfType<MazeGridGenerator>();
        if (victoryScreenPanel != null) victoryScreenPanel.SetActive(false);
    }

    void Update()
    {
        // Detect when the live player slips past the maze floor bounds
        if (transform.position.y <= escapeHeightThreshold && !cutsceneStarted)
        {
            StartCoroutine(SwitchToForcedCutscene());
        }

        // Quick restart testing shortcut 
        if (cutsceneStarted && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    IEnumerator SwitchToForcedCutscene()
    {
        cutsceneStarted = true;

        // 1. Instantly activate our independent cutscene movie set 
        if (abyssCutsceneSet != null)
        {
            abyssCutsceneSet.SetActive(true);
        }

        // 2. Play the meme scream sound at a adjusted 0.6f volume level
        if (cutsceneAudioSource != null)
        {
            cutsceneAudioSource.volume = 0.6f; 
            cutsceneAudioSource.Play();
        }

        // 3. Hide/Disable the live gameplay player entirely so they vanish instantly
        this.gameObject.SetActive(false);

        // 4. Let the frontflip animation play out for 2.5 seconds
        yield return new WaitForSeconds(2.5f);

        // 5. Smoothly turn the sound down over 1 second to simulate falling away
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

        // 6. Calculate and display final completion times from your generator
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

        // Unlock mouse cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pull up the victory screen panel
        if (victoryScreenPanel != null)
        {
            victoryScreenPanel.SetActive(true);
        }
    }
}