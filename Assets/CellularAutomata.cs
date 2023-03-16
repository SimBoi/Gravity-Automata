using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CellType
{
    Empty,
    Stone,
    Water
}

public abstract class Cell
{
    public CellularAutomata ca;
    public CellType type;

    public Cell(CellularAutomata ca) { this.ca = ca; }
    public virtual void UpdateCell(int x, int y)
    {
        ca.newGrid[x, y] = ca.grid[x, y];
    }
}

public class Stone : Cell
{
    public Stone(CellularAutomata ca) : base(ca)
    {
        type = CellType.Stone;
    }
}

public class Water : Cell
{
    public Water(CellularAutomata ca) : base(ca)
    {
        type = CellType.Water;
    }

    public override void UpdateCell(int x, int y)
    {
        if (y - 1 >= 0 && ca.grid[x, y - 1] == null)
        {
            ca.newGrid[x, y - 1] = this;
        }
        else
        {
            base.UpdateCell(x, y);
        }
    }
}

public class CellularAutomata : MonoBehaviour
{
    public int sizeX, sizeY, scale = 1;
    public Cell[,] grid;
    public Cell[,] newGrid;
    public float[,] volume;
    public Dictionary<Vector2, float> gravitySources = new Dictionary<Vector2, float>();

    public GameObject cellPrefab;
    private Image[,] cellsUI;

    // Start is called before the first frame update
    private void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        grid = new Cell[sizeX, sizeY];
        newGrid = new Cell[sizeX, sizeY];
        cellsUI = new Image[sizeX, sizeY];
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = new Vector3(x - sizeX/2, y - sizeY/2, 0);
                cellsUI[x, y] = cell.GetComponent<Image>();
            }
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        newGrid = new Cell[sizeX, sizeY];
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                if (grid[x, y] != null) grid[x, y].UpdateCell(x, y);
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                grid[x, y] = newGrid[x, y];

        RenderGrid();
    }

    private void RenderGrid()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                if (grid[x, y] == null)
                    cellsUI[x, y].color = Color.black;
                else if (grid[x, y].type == CellType.Stone)
                    cellsUI[x, y].color = Color.gray;
                else if (grid[x, y].type == CellType.Water)
                    cellsUI[x, y].color = Color.blue;
            }
        }
    }
}
