using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your _GridManager object here.")]
    public MazeGridGenerator gridGenerator;

    [Tooltip("Drag your active moving Main Camera here so the map can calculate your position.")]
    public Transform playerCamera;

    [Header("UI Setup")]
    [Tooltip("Drag your Map_Overlay_Panel here.")]
    public GameObject mapPanel;
    
    [Tooltip("Drag your Grid_Container here.")]
    public Transform gridContainer;

    [Tooltip("Drag your disabled Room_Template Image object here.")]
    public GameObject roomPrefabTemplate;

    [Header("UI Variant Palette Configuration")]
    public Color blueRoomColor   = new Color(0.15f, 0.35f, 0.8f, 1f);
    public Color redRoomColor    = new Color(0.8f, 0.15f, 0.15f, 1f);
    public Color greenRoomColor  = new Color(0.15f, 0.65f, 0.25f, 1f);
    public Color pinkRoomColor   = new Color(0.85f, 0.3f, 0.6f, 1f);
    public Color yellowRoomColor = new Color(0.85f, 0.75f, 0.15f, 1f);
    
    [Header("Visibility Tones")]
    [Tooltip("The color for empty slots that are right next to the player.")]
    public Color adjacentEmptyColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    
    [Tooltip("The color for unrevealed rooms completely outside the player's immediate view.")]
    public Color fogOfWarHiddenColor = new Color(0.02f, 0.02f, 0.02f, 1f);

    private bool isMapOpen = false;

    void Start()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomPrefabTemplate != null) roomPrefabTemplate.SetActive(false);

        // Auto-assign camera fallback if left unassigned
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isMapOpen = !isMapOpen;
            
            if (mapPanel != null) mapPanel.SetActive(isMapOpen);

            if (isMapOpen)
            {
                GenerateProceduralMap();
            }
        }

        // Keep updating live so if the maze shuffles while the map is open, it reflects instantly
        if (isMapOpen)
        {
            GenerateProceduralMap();
        }
    }

    public void GenerateProceduralMap()
    {
        if (gridGenerator == null || gridContainer == null || roomPrefabTemplate == null || playerCamera == null) return;

        // 1. Calculate the player's live grid coordinate from the camera position
        float roomSize = gridGenerator.roomSize;
        int playerCol = Mathf.RoundToInt(playerCamera.position.x / roomSize);
        int playerRow = Mathf.RoundToInt(playerCamera.position.z / roomSize);

        // 2. Wipe old cells to cleanly re-draw the proximity layout frame
        foreach (Transform child in gridContainer)
        {
            if (child.gameObject == roomPrefabTemplate) continue;
            Destroy(child.gameObject);
        }

        GameObject[,] currentGrid = gridGenerator.GetLiveGrid();
        if (currentGrid == null) return;

        // 3. Loop through grid from top UI row down to bottom
        for (int row = 3; row >= 0; row--)
        {
            for (int col = 0; col < 4; col++)
            {
                // Create a clean cell box from our template
                GameObject newCell = Instantiate(roomPrefabTemplate, gridContainer);
                newCell.SetActive(true);

                Image cellImage = newCell.GetComponent<Image>();
                if (cellImage != null)
                {
                    // 4. Calculate grid distance away from the player's current room
                    int rowDistance = Mathf.Abs(row - playerRow);
                    int colDistance = Mathf.Abs(col - playerCol);

                    // Check if this box is the current room OR a direct perpendicular neighbor 
                    // (North, South, East, West—meaning total distance change equals 1)
                    bool isCurrentRoom = (row == playerRow && col == playerCol);
                    bool isDirectNeighbor = (rowDistance + colDistance == 1);

                    if (isCurrentRoom || isDirectNeighbor)
                    {
                        // Visually reveal what is actually here!
                        GameObject roomObject = currentGrid[row, col];

                        if (roomObject != null)
                        {
                            // It's a valid room structure -> apply its distinctive color tag
                            string roomNameLower = roomObject.name.ToLower();

                            if (roomNameLower.Contains("blue"))        cellImage.color = blueRoomColor;
                            else if (roomNameLower.Contains("red"))    cellImage.color = redRoomColor;
                            else if (roomNameLower.Contains("green"))  cellImage.color = greenRoomColor;
                            else if (roomNameLower.Contains("pink"))   cellImage.color = pinkRoomColor;
                            else if (roomNameLower.Contains("yellow")) cellImage.color = yellowRoomColor;
                            else                                       cellImage.color = Color.gray;
                        }
                        else
                        {
                            // It's an empty gap right next to the player -> light up as a dark gray path anomaly
                            cellImage.color = adjacentEmptyColor;
                        }
                    }
                    else
                    {
                        // Completely outside the player's localized awareness sphere -> hide in pure dark fog
                        cellImage.color = fogOfWarHiddenColor;
                    }
                }
            }
        }

        // 5. Force UI to instantly snap elements to alignment shapes
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainer.GetComponent<RectTransform>());
    }
}