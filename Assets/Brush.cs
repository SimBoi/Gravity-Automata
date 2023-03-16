using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Brush : MonoBehaviour
{
    public float radius = 1f;
    public CellType fillType = CellType.Stone;
    private CellularAutomata ca;

    public Vector3 mousePosition;

    private void Start()
    {
        ca = GetComponent<CellularAutomata>();
    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = Input.mousePosition;
        mousePosition = ca.transform.InverseTransformPoint(mousePosition);
        mousePosition.x += ca.sizeX/2 + 0.5f;
        mousePosition.y += ca.sizeY/2 + 0.5f;
        if (mousePosition.x > ca.sizeX || mousePosition.x < 0 || mousePosition.y > ca.sizeY || mousePosition.y < 0) return;
        Vector3Int cellIndex = Vector3Int.FloorToInt(mousePosition);

        if(Input.GetKeyDown("1")) fillType = CellType.Empty;
        if(Input.GetKeyDown("2")) fillType = CellType.Stone;
        if(Input.GetKeyDown("3")) fillType = CellType.Water;

        CellType currentType = CellType.Empty;
        if (ca.grid[cellIndex.x, cellIndex.y] != null) currentType = ca.grid[cellIndex.x, cellIndex.y].type;

        if (fillType != currentType && Input.GetMouseButton(0))
        {
            if(fillType == CellType.Stone) ca.grid[cellIndex.x, cellIndex.y] = new Stone(ca);
            if(fillType == CellType.Water) ca.grid[cellIndex.x, cellIndex.y] = new Water(ca);
            if(fillType == CellType.Empty) ca.grid[cellIndex.x, cellIndex.y] = null;
        }
    }
}