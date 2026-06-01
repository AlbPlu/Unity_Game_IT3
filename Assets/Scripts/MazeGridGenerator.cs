using UnityEngine;
using System.Collections.Generic;

public class MazeGridGenerator : MonoBehaviour
{
    [Header("Room Setup")]
    [Tooltip("Drop your 5 colorful variants here!")]
    public GameObject[] roomPrefabs;
    
    [Tooltip("Adjust this until the rooms snap seamlessly side-by-side!")]
    public float roomSize = 11f;

    [Header("Outer Perimeter Wall")]
    [Tooltip("Create a plain dark or gray material so the boundary wall blends into the background.")]
    public Material perimeterWallMaterial;
    [Tooltip("How high should the outer boundary wall be? Make it match or exceed your room heights.")]
    public float wallHeight = 8f;
    [Tooltip("Thickness of the outer border wall.")]
    public float wallThickness = 1f;

    private const int gridRows = 4;
    private const int gridCols = 4;
    private const int fixedEmptyCount = 2; 

    private GameObject[,] spawnedGrid = new GameObject[gridRows, gridCols];
    private HashSet<Vector2Int> emptySlotIndices = new HashSet<Vector2Int>();

    void Start()
    {
        if (roomPrefabs == null || roomPrefabs.Length != 5)
        {
            Debug.LogError("Please assign exactly 5 Room Variant Prefabs in the Inspector!");
            return;
        }

        GeneratePuzzleGrid();
        BuildOuterPerimeter();
    }

    void GeneratePuzzleGrid()
    {
        // 1. Pick exactly 2 unique empty slots
        while (emptySlotIndices.Count < fixedEmptyCount)
        {
            int randomRow = Random.Range(0, gridRows);
            int randomCol = Random.Range(0, gridCols);
            emptySlotIndices.Add(new Vector2Int(randomRow, randomCol));
        }

        // 2. Create and shuffle our complete 14-card deck
        List<GameObject> roomDeck = new List<GameObject>();
        int activeSlotsCount = (gridRows * gridCols) - fixedEmptyCount; 

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

        // 3. Spawn the rooms
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
                instance.name = $"Room_{row}_{col}_{colorTag}";

                spawnedGrid[row, col] = instance;
            }
        }
    }

// Automates spawning 4 border walls that clip perfectly through the outer doorways on all 4 sides
    void BuildOuterPerimeter()
    {
        // Total physical footprint of your 4x4 matrix (4 * roomSize)
        float totalSize = gridCols * roomSize; 
        float halfSize = totalSize / 2f;

        // The exact mathematical center of the grid coordinates
        float gridCenter = halfSize - (roomSize / 2f); 

        // Create a parent container to hold our border walls cleanly in the hierarchy
        GameObject borderParent = new GameObject("Outer_Perimeter_Border");
        borderParent.transform.SetParent(this.transform);


        // North Wall (Far Top Edge) - Clips exactly through the top row's back doors
        Vector3 northPos = new Vector3(gridCenter, wallHeight / 2f, totalSize - (roomSize / 2f));
        CreateStaticWall("North_Border_Wall", northPos, new Vector3(totalSize, wallHeight, wallThickness), borderParent.transform);

        // South Wall (Near Bottom Edge) - Clips exactly through the bottom row's front doors
        Vector3 southPos = new Vector3(gridCenter, wallHeight / 2f, -(roomSize / 2f));
        CreateStaticWall("South_Border_Wall", southPos, new Vector3(totalSize, wallHeight, wallThickness), borderParent.transform);

        // East Wall (Far Right Edge) - Clips exactly through the right row's side doors
        Vector3 eastPos = new Vector3(totalSize - (roomSize / 2f), wallHeight / 2f, gridCenter);
        CreateStaticWall("East_Border_Wall", eastPos, new Vector3(wallThickness, wallHeight, totalSize), borderParent.transform);

        // West Wall (Near Left Edge) - Clips exactly through the left row's side doors
        Vector3 westPos = new Vector3(-(roomSize / 2f), wallHeight / 2f, gridCenter);
        CreateStaticWall("West_Border_Wall", westPos, new Vector3(wallThickness, wallHeight, totalSize), borderParent.transform);
    }

    // Dynamic helper to construct a single wall segment
    void CreateStaticWall(string wallName, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;

        // Apply our custom perimeter wall material if assigned
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