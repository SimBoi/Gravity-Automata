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
        hasBeenUpdated = true;
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

    public DynamicCell(CellularAutomata ca) : base(ca)
    {
        momentum = Vector2.zero;
        deviation = Vector2.zero;
    }

    public void ApplyForces()
    {
        momentum += ca.gravity * (1.0f / ca.fps);
    }
}

public abstract class Fluid : DynamicCell
{
    float volume;

    public Fluid(CellularAutomata ca, float volume = 1) : base(ca)
    {
        this.volume = volume;
    }

    public override void UpdateCell(int x, int y)
    {
        if (hasBeenUpdated) return;
        hasBeenUpdated = true;

        ApplyForces();

        ca.grid[x, y] = null;

        // get the fall path using the bresenham algorithm on the current momentum and deviation
        Vector2Int start = new Vector2Int(x, y);
        Vector2Int end = CellularVector.Round(start + deviation + momentum);
        List<Vector2Int> fallPath = start != end ? CellularVector.Bresenham(start, end) : new List<Vector2Int>() { start };

        // check the farthest distance the cell can fall down(momentum direction) to using the bresenham fall line
        fallPath.RemoveAt(0); // dont check the starting position
        Vector2Int fallPoint = start; // the farthest point
        foreach (Vector2Int p in fallPath)
        {
            if (ca.InRange(p) && ca.grid[p.x, p.y] == null) fallPoint = p;
            else break;
        }

        // reset the momentum and deviation if the cell hit the ground, otherwise update the deviation vector
        if (fallPoint == end)
        {
            deviation = start + deviation + momentum - end;
        }
        if (fallPoint != end)
        {
            momentum = Vector2.zero;
            deviation = Vector2.zero;
        }

        // fall down to the farthest fall point
        if (fallPoint != start)
        {
            ca.grid[fallPoint.x, fallPoint.y] = this;
        }
        // if the cell cant fall down, check the sides to flow to
        else
        {
            Vector2 down = momentum.normalized;
            Vector2 right = Vector2.Perpendicular(down).normalized;

            Vector2Int rightDiagonalCell = CellularVector.Round(start + down + right);
            Vector2Int leftDiagonalCell = CellularVector.Round(start + down - right);
            Vector2Int rightCell = CellularVector.Round(start + right);
            Vector2Int leftCell = CellularVector.Round(start - right);

            if (ca.InRange(rightDiagonalCell) && ca.grid[rightDiagonalCell.x, rightDiagonalCell.y] == null)
            {
                ca.grid[rightDiagonalCell.x, rightDiagonalCell.y] = this;
            }
            else if (ca.InRange(leftDiagonalCell) && ca.grid[leftDiagonalCell.x, leftDiagonalCell.y] == null)
            {
                ca.grid[leftDiagonalCell.x, leftDiagonalCell.y] = this;
            }
            else if (ca.InRange(rightCell) && ca.grid[rightCell.x, rightCell.y] == null)
            {
                ca.grid[rightCell.x, rightCell.y] = this;
            }
            else if (ca.InRange(leftCell) && ca.grid[leftCell.x, leftCell.y] == null)
            {
                ca.grid[leftCell.x, leftCell.y] = this;
            }
            // stay in place if the sides are also occupied
            else
            {
                ca.grid[x, y] = this;
            }
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
    public int sizeX, sizeY, scale = 1, fps = 30;
    public Cell[,] grid;
    public Vector2 gravity;
    //public Dictionary<Vector2, float> gravitySources = new Dictionary<Vector2, float>();

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
        int[] shuffledIndexes = new int[sizeX];
        for (int i = 0; i < sizeX; i++) shuffledIndexes[i] = i;
        for (int i = sizeX; i > 1; i--)
        {
            int p = Random.Range(0, i);
            int tmp = shuffledIndexes[i-1];
            shuffledIndexes[i-1] = shuffledIndexes[p];
            shuffledIndexes[p] = tmp;
        }

        for (int y = 0; y < sizeY; y++)
            for (int x = 0; x < sizeX; x++)
                if (grid[x, y] != null) grid[x, y].hasBeenUpdated = false;
        for (int y = 0; y < sizeY; y++)
            foreach (int x in shuffledIndexes)
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

    public bool InRange(Vector2Int coords)
    {
        return InRange(coords.x, coords.y);
    }

    public bool InRange(int x, int y)
    {
        return x >= 0 && x < sizeX && y >= 0 && y < sizeY;
    }
}
