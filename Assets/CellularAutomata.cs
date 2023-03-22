using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEditor;
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

    public Cell(CellularAutomata ca)
    {
        this.ca = ca;
    }

    public abstract Cell NewCell(object[] argv);

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
        if (momentum.magnitude < 1) momentum.Normalize();
    }
}

public abstract class Fluid : DynamicCell
{
    public float volume;
    public float maxCompressedVolume;
    public const float defaultMaxVolume = 1;
    public const float compression = 0.15f;
    public const float minFlow = 0.1f;

    public Fluid(CellularAutomata ca, float volume = 1) : base(ca)
    {
        this.volume = volume;
        maxCompressedVolume = defaultMaxVolume;
    }

    public override void UpdateCell(int x, int y)
    {
        if (hasBeenUpdated) return;
        hasBeenUpdated = true;

        ApplyForces();

        // calculate start point and direction vectors
        Vector2Int start = new Vector2Int(x, y);
        Vector2 down = momentum.normalized;
        Vector2 right = Vector2.Perpendicular(down).normalized;

        UpdateCompression(start, -down);

        ca.grid[x, y] = null;

        // flow into neighboring cells
        FlowDown(start);
        if (volume < minFlow) return;
        FlowDiagonally(start, down, right);
        if (volume < minFlow) return;
        FlowSideways(start, -down, right);
        if (volume < minFlow) return;
        FlowUp(start, -down);

        // keep the remaining volume in the current cell
        if (volume > 0) ca.grid[x, y] = this;
    }

    public void UpdateCompression(Vector2Int p, Vector2 up)
    {
        Vector2Int upCell = CellularVector.Round(p + up);
        if (ca.GetCellType(upCell) == type) maxCompressedVolume = ((Fluid)ca.grid[upCell.x, upCell.y]).maxCompressedVolume + compression;
    }

    public void FlowDown(Vector2Int start)
    {
        // get the fall path using the bresenham algorithm on the current momentum and deviation
        Vector2Int end = CellularVector.Round(start + deviation + momentum);
        List<Vector2Int> fallPath = start != end ? CellularVector.Bresenham(start, end) : new List<Vector2Int>() { start };

        // check the farthest distance the cell can fall down(momentum direction) to using the bresenham fall line
        int farthestPoint = 0; // the farthest point
        for (int i = 0; i < fallPath.Count; i++)
        {
            Vector2Int p = fallPath[i];
            if (ca.InRange(p) && (ca.grid[p.x, p.y] == null || ca.grid[p.x, p.y].type == type)) farthestPoint = i;
            else break;
        }

        // reset the momentum and deviation if the cell hit the ground, otherwise update the deviation vector
        if (farthestPoint == fallPath.Count - 1)
        {
            deviation = start + deviation + momentum - end;
        }
        else
        {
            momentum = Vector2.zero;
            deviation = Vector2.zero;
        }

        // flow down to the cells on the fallPath starting from the farthest fall point
        for (int i = farthestPoint; i > 0; i--)
        {
            Vector2Int p = fallPath[i];
            if (ca.grid[p.x, p.y] == null) FlowToEmptyCell(p, volume);
            else FlowToFluidCell(p, volume);
            if (volume <= 0) return;
        }
    }

    public void FlowDiagonally(Vector2Int start, Vector2 down, Vector2 right)
    {
        List<Vector2Int> flowTo = new List<Vector2Int>
        {
            CellularVector.Round(start + down + right),
            CellularVector.Round(start + down - right)
        };

        for (int i = 0; i < flowTo.Count; i++)
        {
            CellType flowToType = ca.GetCellType(flowTo[i]);
            if (!ca.InRange(flowTo[i]) || (flowToType != CellType.Empty && flowToType != type))
            {
                flowTo.RemoveAt(i);
                i--;
            }
        }

        while (flowTo.Count > 0 && volume > 0)
        {
            float split = volume / flowTo.Count;
            for (int i = 0; i < flowTo.Count; i++)
            {
                Vector2Int p = flowTo[i];
                if (ca.grid[p.x, p.y] == null) FlowToEmptyCell(p, split);
                else FlowToFluidCell(p, split);
                Fluid pCell = (Fluid)ca.grid[p.x, p.y];
                if (pCell.volume >= pCell.maxCompressedVolume)
                {
                    flowTo.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void FlowSideways(Vector2Int start, Vector2 up, Vector2 right)
    {
        List<Vector2Int> flowTo = new List<Vector2Int>
        {
            CellularVector.Round(start + right),
            CellularVector.Round(start - right)
        };

        for (int i = 0; i < flowTo.Count; i++)
        {
            CellType flowToType = ca.GetCellType(flowTo[i]);
            if (!ca.InRange(flowTo[i]) ||
                (flowToType != CellType.Empty && flowToType != type) ||
                (flowToType == type && ((Fluid)ca.grid[flowTo[i].x, flowTo[i].y]).volume >= volume))
            {
                flowTo.RemoveAt(i);
                i--;
            }
        }

        while (flowTo.Count > 0 && volume > minFlow)
        {
            float split = volume / (flowTo.Count + 1);
            for (int i = 0; i < flowTo.Count; i++)
            {
                Vector2Int p = flowTo[i];
                if (ca.grid[p.x, p.y] == null) FlowToEmptyCell(p, split);
                else FlowToFluidCell(p, split);
                Fluid pCell = (Fluid)ca.grid[p.x, p.y];
                if (pCell.volume == pCell.maxCompressedVolume)
                {
                    flowTo.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void FlowUp(Vector2Int start, Vector2 up)
    {

    }

    public float FlowToEmptyCell(Vector2Int p, float maxFlow)
    {
        float transfer = Mathf.Min(defaultMaxVolume, maxFlow);
        volume -= transfer;
        Fluid newCell = (Fluid)NewCell(new object[] { ca, transfer });
        newCell.hasBeenUpdated = true;
        newCell.momentum = momentum;
        ca.grid[p.x, p.y] = newCell;
        return transfer;
    }

    public float FlowToFluidCell(Vector2Int p, float maxFlow)
    {
        Fluid pCell = (Fluid)ca.grid[p.x, p.y];
        float transfer = Mathf.Min(pCell.maxCompressedVolume - pCell.volume, maxFlow);
        pCell.volume += transfer;
        volume -= transfer;
        return transfer;
    }
}

////////// cell types

public class Stone : StaticCell
{
    public Stone(CellularAutomata ca) : base(ca)
    {
        type = CellType.Stone;
    }

    public override Cell NewCell(object[] argv)
    {
        return new Stone((CellularAutomata)argv[0]);
    }
}

public class Water : Fluid
{
    public Water(CellularAutomata ca, float volume = 1) : base(ca)
    {
        type = CellType.Water;
        this.volume = volume;
    }

    public override Cell NewCell(object[] argv)
    {
        return new Water((CellularAutomata)argv[0], (float)argv[1]);
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

    public CellType GetCellType(Vector2Int p)
    {
        return !InRange(p) || grid[p.x, p.y] == null ? CellType.Empty : grid[p.x, p.y].type;
    }
}