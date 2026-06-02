using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using StarterAssets; 

public class MazeGridGenerator : MonoBehaviour
{
    [Header("Room Setup")]
    public GameObject[] roomPrefabs;
    [Tooltip("The EXACT physical width/length of your room prefabs after scaling them up.")]
    public float roomSize = 22f;

    [Header("Outer Perimeter Wall")]
    public Material perimeterWallMaterial;
    public float wallHeight = 8f;
    public float wallThickness = 1f;

    [Header("Shifting Settings")]
    [Tooltip("How often (in seconds) does the maze shuffle?")]
    public float shiftInterval = 15f;
    [Tooltip("How long does the earthquake shake last?")]
    public float shakeDuration = 2f;
    [Tooltip("How violent is the camera shake?")]
    public float shakeMagnitude = 0.3f;

    private const int gridRows = 4;
    private const int gridCols = 4;
    
    private GameObject[,] spawnedGrid = new GameObject[gridRows, gridCols];
    private float shiftTimer;
    private bool isShifting = false;

    private GameObject playerInstance;
    private FirstPersonController starterAssetsMovement; 

    void Start()
    {
        if (roomPrefabs == null || roomPrefabs.Length != 5)
        {
            Debug.LogError("Please assign exactly 5 Room Variant Prefabs!");
            return;
        }

        GeneratePuzzleGrid();
        BuildOuterPerimeter();
        PositionPlayerSafely(); 
        
        shiftTimer = shiftInterval;
    }

    void Update()
    {
        shiftTimer -= Time.deltaTime;
        if (shiftTimer <= 0f && !isShifting)
        {
            StartCoroutine(ExecuteMazeShiftSequence());
            shiftTimer = shiftInterval; 
        }
    }

    void GeneratePuzzleGrid()
    {
        HashSet<Vector2Int> emptySlotIndices = new HashSet<Vector2Int>();
        while (emptySlotIndices.Count < 2)
        {
            int randomRow = Random.Range(0, gridRows);
            int randomCol = Random.Range(0, gridCols);
            emptySlotIndices.Add(new Vector2Int(randomRow, randomCol));
        }

        List<GameObject> roomDeck = new List<GameObject>();
        int activeSlotsCount = (gridRows * gridCols) - 2; 

        for (int i = 0; i < roomPrefabs.Length; i++)
        {
            roomDeck.Add(roomPrefabs[i]);
            roomDeck.Add(roomPrefabs[i]);
        }

        while (roomDeck.Count < activeSlotsCount)
        {
            roomDeck.Add(roomPrefabs[Random.Range(0, roomPrefabs.Length)]);
        }

        ShuffleList(roomDeck);

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Vector2Int currentCoord = new Vector2Int(row, col);

                if (emptySlotIndices.Contains(currentCoord))
                {
                    spawnedGrid[row, col] = null; 
                    continue;
                }

                float xPos = col * roomSize;
                float zPos = row * roomSize;
                Vector3 spawnPosition = new Vector3(xPos, 0f, zPos);

                GameObject finalPrefabChoice = PullSafeRoomFromDeck(row, col, roomDeck);

                int randomRotationIndex = Random.Range(0, 4); 
                float yRotationAngle = randomRotationIndex * 90f;
                Quaternion spawnRotation = Quaternion.Euler(0f, yRotationAngle, 0f);

                string colorTag = GetColorNameFromInstance(finalPrefabChoice.name);

                GameObject instance = Instantiate(finalPrefabChoice, spawnPosition, spawnRotation, this.transform);
                instance.name = $"Room_{colorTag}";

                spawnedGrid[row, col] = instance;
            }
        }
    }

    void PositionPlayerSafely()
    {
        playerInstance = GameObject.FindGameObjectWithTag("Player");
        if (playerInstance == null)
        {
            playerInstance = GameObject.Find("NestedParent_Unpack");
        }

        if (playerInstance == null)
        {
            Debug.LogError("FATAL: Could not find 'NestedParent_Unpack' or an object tagged 'Player' anywhere in the Hierarchy!");
            return;
        }

        starterAssetsMovement = playerInstance.GetComponentInChildren<FirstPersonController>();

        List<Vector2Int> safeSpawnCoordinates = new List<Vector2Int>();

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                if (spawnedGrid[row, col] == null) continue;

                bool isEdgeRoom = (row == 0 || row == gridRows - 1 || col == 0 || col == gridCols - 1);
                if (!isEdgeRoom) continue;

                bool touchesEmpty = false;
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (Vector2Int dir in directions)
                {
                    int checkRow = row + dir.x;
                    int checkCol = col + dir.y;
                    if (checkRow >= 0 && checkRow < gridRows && checkCol >= 0 && checkCol < gridCols)
                    {
                        if (spawnedGrid[checkRow, checkCol] == null)
                        {
                            touchesEmpty = true;
                            break;
                        }
                    }
                }

                if (!touchesEmpty) safeSpawnCoordinates.Add(new Vector2Int(row, col));
            }
        }

        if (safeSpawnCoordinates.Count == 0)
        {
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    if (spawnedGrid[row, col] != null && (row == 0 || col == 0)) 
                        safeSpawnCoordinates.Add(new Vector2Int(row, col));
                }
            }
        }

        Vector2Int chosenCoord = safeSpawnCoordinates[Random.Range(0, safeSpawnCoordinates.Count)];
        
        float spawnX = chosenCoord.y * roomSize;
        float spawnZ = chosenCoord.x * roomSize;
        Vector3 finalSpawnPos = new Vector3(spawnX, 1.2f, spawnZ); 

        if (chosenCoord.x == 0) 
            finalSpawnPos.z -= (roomSize * 0.46f);
        else if (chosenCoord.x == gridRows - 1) 
            finalSpawnPos.z += (roomSize * 0.46f);
        else if (chosenCoord.y == 0) 
            finalSpawnPos.x -= (roomSize * 0.46f);
        else if (chosenCoord.y == gridCols - 1) 
            finalSpawnPos.x += (roomSize * 0.46f);

        CharacterController cc = playerInstance.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerInstance.transform.position = finalSpawnPos;

        if (cc != null) cc.enabled = true;

        Debug.Log($"Starter Assets deployed safely to expanded edge block [{chosenCoord.x}, {chosenCoord.y}]");
    }

    IEnumerator ExecuteMazeShiftSequence()
    {
        isShifting = true;
        Debug.LogWarning("MAZE SHIFT INITIATED! Shaking world...");

        Camera mainCam = Camera.main;
        Vector3 originalCamPos = Vector3.zero;

        if (starterAssetsMovement != null) starterAssetsMovement.enabled = false;
        CharacterController cc = playerInstance?.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        Transform roomBeneathPlayer = null;
        if (playerInstance != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(playerInstance.transform.position, Vector3.down, out hit, 10f))
            {
                Transform current = hit.transform;
                while (current != null)
                {
                    if (current.name.StartsWith("Room_"))
                    {
                        roomBeneathPlayer = current;
                        break;
                    }
                    current = current.parent;
                }
            }

            if (roomBeneathPlayer != null)
            {
                playerInstance.transform.SetParent(roomBeneathPlayer, true);
                Debug.Log($"Player securely locked into {roomBeneathPlayer.name} structure for shifting.");
            }
        }

        if (mainCam != null) originalCamPos = mainCam.transform.localPosition;

        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            if (mainCam != null)
            {
                float xShake = Random.Range(-1f, 1f) * shakeMagnitude;
                float zShake = Random.Range(-1f, 1f) * shakeMagnitude;
                mainCam.transform.localPosition = originalCamPos + new Vector3(xShake, 0f, zShake);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mainCam != null) mainCam.transform.localPosition = originalCamPos; 

        // Sliding Step runs with full awareness of player position
        SlidingPuzzleStep();

        if (playerInstance != null)
        {
            playerInstance.transform.SetParent(null, true); 
        }

        if (starterAssetsMovement != null) starterAssetsMovement.enabled = true;
        if (cc != null) cc.enabled = true;

        isShifting = false;
        Debug.Log("Maze shift complete. Rooms locked.");
    }

    private GameObject lastMovedRoom = null;

    void SlidingPuzzleStep()
    {
        for (int step = 0; step < 2; step++)
        {
            List<Vector2Int> emptySlots = new List<Vector2Int>();
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    if (spawnedGrid[row, col] == null) emptySlots.Add(new Vector2Int(row, col));
                }
            }

            if (emptySlots.Count == 0) return;

            for (int i = emptySlots.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                Vector2Int temp = emptySlots[i];
                emptySlots[i] = emptySlots[rnd];
                emptySlots[rnd] = temp;
            }

            foreach (Vector2Int targetEmpty in emptySlots)
            {
                List<Vector2Int> eligibleNeighbors = new List<Vector2Int>();
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int checkPos = targetEmpty + dir;
                    if (checkPos.x >= 0 && checkPos.x < gridRows && checkPos.y >= 0 && checkPos.y < gridCols)
                    {
                        GameObject potentialRoom = spawnedGrid[checkPos.x, checkPos.y];
                        
                        // DOORWAY GUARD SAFETY CHECK:
                        // Skips shifting this room if the player is dangerously close to its center bounds (standing in its threshold)
                        if (potentialRoom != null && potentialRoom != lastMovedRoom)
                        {
                            if (playerInstance != null)
                            {
                                // Calculate distance in 2D horizontal space between player and the room's core position
                                float distToRoom = Vector2.Distance(
                                    new Vector2(playerInstance.transform.position.x, playerInstance.transform.position.z),
                                    new Vector2(potentialRoom.transform.position.x, potentialRoom.transform.position.z)
                                );

                                // If the player is on the edge/doorway of this room, protect them by locking this room down!
                                float safetyRadius = roomSize * 0.65f; 
                                if (distToRoom > (roomSize * 0.4f) && distToRoom < safetyRadius)
                                {
                                    continue; // Skip and try a different neighbor room layout
                                }
                            }

                            eligibleNeighbors.Add(checkPos);
                        }
                    }
                }

                if (eligibleNeighbors.Count == 0 && lastMovedRoom != null)
                {
                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int checkPos = targetEmpty + dir;
                        if (checkPos.x >= 0 && checkPos.x < gridRows && checkPos.y >= 0 && checkPos.y < gridCols)
                        {
                            if (spawnedGrid[checkPos.x, checkPos.y] != null) eligibleNeighbors.Add(checkPos);
                        }
                    }
                }

                if (eligibleNeighbors.Count > 0)
                {
                    Vector2Int chosenRoomCoord = eligibleNeighbors[Random.Range(0, eligibleNeighbors.Count)];
                    GameObject roomToMove = spawnedGrid[chosenRoomCoord.x, chosenRoomCoord.y];

                    if (roomToMove != null)
                    {
                        float newX = targetEmpty.y * roomSize;
                        float newZ = targetEmpty.x * roomSize;
                        
                        roomToMove.transform.position = new Vector3(newX, roomToMove.transform.position.y, newZ);

                        spawnedGrid[targetEmpty.x, targetEmpty.y] = roomToMove;
                        spawnedGrid[chosenRoomCoord.x, chosenRoomCoord.y] = null;

                        lastMovedRoom = roomToMove;
                    }
                }
            }
        }
        lastMovedRoom = null;
    }

    void BuildOuterPerimeter()
    {
        float totalSize = gridCols * roomSize; 
        float halfSize = totalSize / 2f;
        float centerOffset = halfSize - (roomSize / 2f); 

        GameObject borderParent = new GameObject("Outer_Perimeter_Border");
        borderParent.transform.SetParent(this.transform);

        Vector3 northPos = new Vector3(centerOffset, wallHeight / 2f, totalSize - (roomSize / 2f));
        CreateStaticWall("North_Border_Wall", northPos, new Vector3(totalSize, wallHeight, wallThickness), borderParent.transform);

        Vector3 southPos = new Vector3(centerOffset, wallHeight / 2f, -(roomSize / 2f));
        CreateStaticWall("South_Border_Wall", southPos, new Vector3(totalSize, wallHeight, wallThickness), borderParent.transform);

        Vector3 eastPos = new Vector3(totalSize - (roomSize / 2f), wallHeight / 2f, centerOffset);
        CreateStaticWall("East_Border_Wall", eastPos, new Vector3(wallThickness, wallHeight, totalSize), borderParent.transform);

        Vector3 westPos = new Vector3(-(roomSize / 2f), wallHeight / 2f, centerOffset);
        CreateStaticWall("West_Border_Wall", westPos, new Vector3(wallThickness, wallHeight, totalSize), borderParent.transform);
    }

    void CreateStaticWall(string wallName, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;

        if (perimeterWallMaterial != null)
        {
            wall.GetComponent<Renderer>().material = perimeterWallMaterial;
        }
    }

    GameObject PullSafeRoomFromDeck(int row, int col, List<GameObject> deck)
    {
        string leftColor = (col > 0 && spawnedGrid[row, col - 1] != null) ? GetColorNameFromInstance(spawnedGrid[row, col - 1].name) : "";
        string bottomColor = (row > 0 && spawnedGrid[row - 1, col] != null) ? GetColorNameFromInstance(spawnedGrid[row - 1, col].name) : "";

        for (int i = 0; i < deck.Count; i++)
        {
            string prefabName = deck[i].name.ToLower();
            if (!prefabName.Contains(leftColor) && !prefabName.Contains(bottomColor))
            {
                GameObject chosenRoom = deck[i];
                deck.RemoveAt(i);
                return chosenRoom;
            }
        }

        GameObject fallbackRoom = deck[0];
        deck.RemoveAt(0);
        return fallbackRoom;
    }

    void ShuffleList(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            GameObject temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    string GetColorNameFromInstance(string instanceName)
    {
        string nameLower = instanceName.ToLower();
        if (nameLower.Contains("blue")) return "blue";
        if (nameLower.Contains("red")) return "red";
        if (nameLower.Contains("green")) return "green";
        if (nameLower.Contains("pink")) return "pink";
        if (nameLower.Contains("yellow")) return "yellow";
        return "";
    }
}