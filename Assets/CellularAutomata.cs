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

////////// abstract cell class, cell categories classes and cell sub-categories classes

public abstract class Cell
{
    public CellularAutomata ca;
    public CellType type;
    public bool hasBeenUpdated;

    public Cell(CellularAutomata ca) { this.ca = ca; }
    public virtual void UpdateCell(int x, int y)
    {
        hasBeenUpdated= true;
    }
}

public abstract class StaticCell : Cell
{
    public StaticCell(CellularAutomata ca) : base(ca) { }
}

public abstract class DynamicCell : Cell
{
    public Vector2 momentum;
    public Vector2 deviation;

    public DynamicCell(CellularAutomata ca) : base(ca) { }
}

public abstract class Fluid : DynamicCell
{
    float volume;

    public Fluid(CellularAutomata ca) : base(ca) { }

    public override void UpdateCell(int x, int y)
    {
        if (hasBeenUpdated) return;
        hasBeenUpdated = true;

        int dir = Random.Range(0, 2) * 2 - 1;

        ca.grid[x, y] = null;
        if (y - 1 >= 0 && ca.grid[x, y - 1] == null)
        {
            ca.grid[x, y - 1] = this;
        }
        else if (y - 1 >= 0 && x - dir >= 0 && x - dir < ca.sizeX && ca.grid[x - dir, y - 1] == null)
        {
            ca.grid[x - dir, y - 1] = this;
        }
        else if (y - 1 >= 0 && x + dir >= 0 && x + dir < ca.sizeX && ca.grid[x + dir, y - 1] == null)
        {
            ca.grid[x + dir, y - 1] = this;
        }
        else if (x - dir >= 0 && x - dir < ca.sizeX && ca.grid[x - dir, y] == null)
        {
            ca.grid[x - dir, y] = this;
        }
        else if (x + dir >= 0 && x + dir < ca.sizeX && ca.grid[x + dir, y] == null)
        {
            ca.grid[x + dir, y] = this;
        }
        else
        {
            ca.grid[x, y] = this;
        }
    }
}

////////// cell types

public class Stone : StaticCell
{
    public Stone(CellularAutomata ca) : base(ca)
    {
        type = CellType.Stone;
    }
}

public class Water : Fluid
{
    public Water(CellularAutomata ca) : base(ca)
    {
        type = CellType.Water;
    }
}

////////// cellular automata grid class

public class CellularAutomata : MonoBehaviour
{
    public int sizeX, sizeY, scale = 1;
    public Cell[,] grid;
    public Dictionary<Vector2, float> gravitySources = new Dictionary<Vector2, float>();

    public GameObject cellPrefab;
    private Image[,] cellsUI;

    // Start is called before the first frame update
    private void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        grid = new Cell[sizeX, sizeY];
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
        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
                if (grid[x, y] != null) grid[x, y].hasBeenUpdated = false;
        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
                if (grid[x, y] != null) grid[x, y].UpdateCell(x, y);

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
