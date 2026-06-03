using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your _GridManager object here.")]
    public MazeGridGenerator gridGenerator;

    [Header("UI Setup")]
    [Tooltip("Drag your Map_Overlay_Panel here.")]
    public GameObject mapPanel;
    
    [Tooltip("Drag your Grid_Container here. Ensure it has a Grid Layout Group component!")]
    public Transform gridContainer;

    [Tooltip("Drag your disabled Room_Template Image object here.")]
    public GameObject roomPrefabTemplate;

    [Header("UI Variant Palette Configuration")]
    public Color blueRoomColor   = new Color(0.15f, 0.35f, 0.8f, 1f);
    public Color redRoomColor    = new Color(0.8f, 0.15f, 0.15f, 1f);
    public Color greenRoomColor  = new Color(0.15f, 0.65f, 0.25f, 1f);
    public Color pinkRoomColor   = new Color(0.85f, 0.3f, 0.6f, 1f);
    public Color yellowRoomColor = new Color(0.85f, 0.75f, 0.15f, 1f);
    
    [Tooltip("The background color for empty slots or abyss void spaces.")]
    public Color emptySpaceColor = new Color(0.06f, 0.06f, 0.06f, 0.85f);

    private bool isMapOpen = false;

    void Start()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
        if (roomPrefabTemplate != null) roomPrefabTemplate.SetActive(false);
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
    }

    public void GenerateProceduralMap()
    {
        if (gridGenerator == null || gridContainer == null || roomPrefabTemplate == null) return;

        // 1. Wipe old cells cleanly to draw the current live state
        foreach (Transform child in gridContainer)
        {
            if (child.gameObject == roomPrefabTemplate) continue;
            Destroy(child.gameObject);
        }

        GameObject[,] currentGrid = gridGenerator.GetLiveGrid();
        if (currentGrid == null) return;

        // 2. Loop through row layers from top down (3 down to 0) to match screen layout positions
        for (int row = 3; row >= 0; row--)
        {
            for (int col = 0; col < 4; col++)
            {
                // 3. Generate a distinct cell instance from the baseline template configuration
                GameObject newCell = Instantiate(roomPrefabTemplate, gridContainer);
                newCell.SetActive(true);
                newCell.name = $"UI_Cell_R{row}_C{col}";

                Image cellImage = newCell.GetComponent<Image>();
                if (cellImage != null)
                {
                    GameObject roomObject = currentGrid[row, col];

                    if (roomObject != null)
                    {
                        // 4. Extract the active runtime color tag string name assigned by the grid generator
                        string roomNameLower = roomObject.name.ToLower();

                        if (roomNameLower.Contains("blue"))        cellImage.color = blueRoomColor;
                        else if (roomNameLower.Contains("red"))    cellImage.color = redRoomColor;
                        else if (roomNameLower.Contains("green"))  cellImage.color = greenRoomColor;
                        else if (roomNameLower.Contains("pink"))   cellImage.color = pinkRoomColor;
                        else if (roomNameLower.Contains("yellow")) cellImage.color = yellowRoomColor;
                        else
                        {
                            // Fallback color if a room variant doesn't have an explicit color string signature
                            cellImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
                        }
                    }
                    else
                    {
                        // Match empty slot spaces to the dark void color theme configuration
                        cellImage.color = emptySpaceColor;
                    }
                }
            }
        }

        // 5. Instantly force the UI engine layout calculations to refresh positions on this frame execution
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainer.GetComponent<RectTransform>());
    }
}