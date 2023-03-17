using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Brush : MonoBehaviour
{
    public int size = 1;
    public CellType fillType = CellType.Stone;

    private CellularAutomata ca;

    private void Start()
    {
        ca = GetComponent<CellularAutomata>();
    }

    // Update is called once per frame
    void Update()
    {
        // get selected cell index
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = ca.transform.InverseTransformPoint(mousePosition);
        mousePosition.x += ca.sizeX / 2 + 0.5f;
        mousePosition.y += ca.sizeY / 2 + 0.5f;
        Vector3Int cellIndex = Vector3Int.FloorToInt(mousePosition);

        // get user input
        if (Input.GetKeyDown("1")) size = 1;
        if (Input.GetKeyDown("2")) size = 2;
        if (Input.GetKeyDown("3")) size = 4;
        if (Input.GetKeyDown("4")) size = 8;
        if (Input.GetKeyDown("5")) size = 16;
        if (Input.GetKeyDown("q")) fillType = CellType.Empty;
        if (Input.GetKeyDown("w")) fillType = CellType.Stone;
        if (Input.GetKeyDown("e")) fillType = CellType.Water;

        // fill a pixelated circle with selected cell type
        if (Input.GetMouseButton(0))
        {
            int radius = size - 1;
            for (int x = -radius + cellIndex.x; x <= radius + cellIndex.x; x++)
            {
                int yRange = (int)Mathf.Sqrt(radius - Mathf.Pow(x - cellIndex.x, 2));
                for (int y = -yRange + cellIndex.y; y <= yRange + cellIndex.y; y++)
                {
                    if (x >= ca.sizeX || x < 0 || y >= ca.sizeY || y < 0) continue;

                    CellType currentType = CellType.Empty;
                    if (ca.grid[x, y] != null) currentType = ca.grid[x, y].type;

                    if (fillType == currentType) continue;

                    if (fillType == CellType.Stone) ca.grid[x, y] = new Stone(ca);
                    if (fillType == CellType.Water) ca.grid[x, y] = new Water(ca);
                    if (fillType == CellType.Empty) ca.grid[x, y] = null;
                }
            }
        }
    }
}