using UnityEngine;
using System.Collections;

public class AtmosphereAudioDirector : MonoBehaviour
{
    [Header("Tracking Components")]
    [Tooltip("Drag your player object")]
    public Transform playerTransform;

    [Header("Background Ambiance")]
    [Tooltip("Drag your audio files")]
    public AudioClip continuousAmbianceClip;
    [Range(0f, 1f)] public float ambianceVolume = 0.1f;

    [Header("Close-Range Scares")]
    [Tooltip("Drag your audio files")]
    public AudioClip[] closeScaresPool;
    public float minCloseDistance = 4f;
    public float maxCloseDistance = 10f;

    [Header("Distant Scares")]
    [Tooltip("Drag your audio files")]
    public AudioClip[] distantScaresPool;
    public float minDistantRange = 15f;
    public float maxDistantRange = 30f; // Lowered slightly so it stays within physical limits better

    [Header("Pacing Settings")]
    public float minSecondsBetweenScares = 20f;
    public float maxSecondsBetweenScares = 30f;

    [Header("Grid Boundaries (Clamping Math)")]
    [Tooltip("The size of a single room in your maze")]
    public float roomUnitSize = 22f;
    [Tooltip("How many rooms wide is your maze grid?")]
    public int gridRoomsWide = 4;

    private AudioSource ambiance2DSource;
    private float maxGridCoordinate;
    
    // Trackers to prevent back-to-back repeats
    private int lastCloseIndex = -1;
    private int lastDistantIndex = -1;

    void Start()
    {
        maxGridCoordinate = gridRoomsWide * roomUnitSize;

        // Find the absolute root player or the unpacked fallback asset to match the generator logic
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("NestedParent_Unpack");
            }

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("AtmosphereAudioDirector: Could not find any valid Player object or 'NestedParent_Unpack' in the hierarchy!");
            }
        }

        SetupContinuousAmbiance();
        StartCoroutine(AudioDirectorLoop());
    }

    void SetupContinuousAmbiance()
    {
        if (continuousAmbianceClip == null) return;

        ambiance2DSource = gameObject.AddComponent<AudioSource>();
        ambiance2DSource.clip = continuousAmbianceClip;
        ambiance2DSource.volume = ambianceVolume;
        ambiance2DSource.spatialBlend = 0.0f; // 2D flat sound for background environment
        ambiance2DSource.loop = true;
        ambiance2DSource.playOnAwake = true;
        ambiance2DSource.Play();
    }

    IEnumerator AudioDirectorLoop()
    {
        yield return new WaitForSeconds(5f);

        while (true)
        {
            float delay = Random.Range(minSecondsBetweenScares, maxSecondsBetweenScares);
            yield return new WaitForSeconds(delay);

            // Double check reference safety loop in case hierarchy shifts drop the pointer
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("NestedParent_Unpack");
                if (playerObj != null) 
                {
                    playerTransform = playerObj.transform;
                }
            }

            if (playerTransform == null) continue;

            // 50/50 split between close and distant zones
            if (Random.value > 0.5f)
            {
                TriggerSpatialScare(closeScaresPool, minCloseDistance, maxCloseDistance, 1.0f, "Close", ref lastCloseIndex);
            }
            else
            {
                TriggerSpatialScare(distantScaresPool, minDistantRange, maxDistantRange, 0.85f, "Distant", ref lastDistantIndex);
            }
        }
    }

    void TriggerSpatialScare(AudioClip[] clipPool, float minDist, float maxDist, float targetVolume, string zoneDebugName, ref int lastIndex)
    {
        if (clipPool == null || clipPool.Length == 0) return;

        // 1. FORCED RANDOMIZATION: Loop until we get a different index than last time
        int randomIndex = lastIndex;
        if (clipPool.Length > 1)
        {
            while (randomIndex == lastIndex)
            {
                randomIndex = Random.Range(0, clipPool.Length);
            }
            lastIndex = randomIndex; 
        }
        else
        {
            randomIndex = 0; // Fallback if pool only has 1 sound
        }

        AudioClip selectedClip = clipPool[randomIndex];

        // 2. Generate directional offset
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minDist, maxDist);
        Vector3 targetOffset = new Vector3(randomDirection.x, 0.5f, randomDirection.y) * randomDistance;
        
        Vector3 rawCalculatedPosition = playerTransform.position + targetOffset;

        // 3. Grid Clamp
        float clampedX = Mathf.Clamp(rawCalculatedPosition.x, 2f, maxGridCoordinate - 2f);
        float clampedZ = Mathf.Clamp(rawCalculatedPosition.z, 2f, maxGridCoordinate - 2f);
        Vector3 finalGridPosition = new Vector3(clampedX, rawCalculatedPosition.y, clampedZ);

        // 4. Create the anchor object
        GameObject soundAnchor = new GameObject($"Acoustic_Stab_{zoneDebugName}_{selectedClip.name}");
        soundAnchor.transform.position = finalGridPosition;

        // 5. Build standard 3D playback configurations
        AudioSource spatialSource = soundAnchor.AddComponent<AudioSource>();
        spatialSource.clip = selectedClip;
        spatialSource.volume = targetVolume;
        spatialSource.spatialBlend = 1.0f; // 100% 3D spatial sound
        
        // Linear Rolloff so distance feels real relative to the player
        spatialSource.rolloffMode = AudioRolloffMode.Linear;
        spatialSource.minDistance = 1f; 
        spatialSource.maxDistance = maxDist + 10f; 

        spatialSource.Play();
        Destroy(soundAnchor, selectedClip.length + 0.5f);

        Debug.Log($"[{zoneDebugName}] Playing: {selectedClip.name} at distance: {Vector3.Distance(playerTransform.position, finalGridPosition):F1} meters.");
    }
}