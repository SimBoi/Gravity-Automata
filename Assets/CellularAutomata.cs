using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public enum CellType
{
    Empty,
    Stone,
    Water
}

public class TraversingLines
{
    private int size;

    public Vector2Int[] verticalStartPoints;
    public Vector2Int[] horizontalStartPoints;
    public Vector2 downDir;
    public Vector2Int[] down;
    public Vector2Int[] right;
    public Hashtable pointIndexOnDownPath = new Hashtable();
    public Hashtable pointIndexOnRightPath = new Hashtable();

    public int[] shuffledIndexes;

    public TraversingLines(int size)
    {
        this.size = size;
        verticalStartPoints = new Vector2Int[2 * size];
        horizontalStartPoints = new Vector2Int[2 * size];
        down = new Vector2Int[size];
        right = new Vector2Int[size];
        shuffledIndexes = new int[size];
    }

    public void ShuffleIndexes()
    {
        for (int i = 0; i < size; i++) shuffledIndexes[i] = i;
        for (int i = size; i > 1; i--)
        {
            int p = Random.Range(0, i);
            int tmp = shuffledIndexes[i - 1];
            shuffledIndexes[i - 1] = shuffledIndexes[p];
            shuffledIndexes[p] = tmp;
        }
    }

    public void GenerateLines(Vector2 downDir)
    {
        // vertical traversing
        downDir = downDir.normalized;
        this.downDir = downDir;
        down = CellularVector.Bresenham(Vector2Int.zero, CellularVector.Round(downDir * size * 1.5f)).GetRange(0, size).ToArray();

        Vector2 downNormalDir = GenerateStartPoints(size, downDir, ref verticalStartPoints);

        // horizontal traversing
        Vector2 rightDir = Vector2.Perpendicular(downDir).normalized;
        if (Vector2.Dot(downNormalDir, rightDir) < 0) rightDir *= -1;
        right = CellularVector.Bresenham(Vector2Int.zero, Vector2Int.FloorToInt(rightDir * size * 1.5f)).GetRange(0, size).ToArray();
        GenerateStartPoints(size, rightDir, ref horizontalStartPoints);
        if (downDir.x == downDir.y)
        {
            System.Array.Reverse(horizontalStartPoints);
        }

        // fill point index on path hashtables
        pointIndexOnDownPath.Clear();
        for (int i = 0; i < 2 * size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector2Int point = verticalStartPoints[i] + down[j];
                if (InRange(point)) pointIndexOnDownPath[point] = j;
            }
        }
        pointIndexOnRightPath.Clear();
        for (int i = 0; i < 2 * size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector2Int point = horizontalStartPoints[i] + right[j];
                if (InRange(point)) pointIndexOnRightPath[point] = j;
            }
        }
    }

    private static Vector2 GenerateStartPoints(int size, Vector2 dir, ref Vector2Int[] startPoints) // returns normal to the start points plane
    {
        Vector2 normalDir;
        Vector2 planeCenter;
        if (Vector2.Angle(Vector2.down, dir) <= 45f)
        {
            normalDir = Vector2.down;
            planeCenter = new Vector2Int(size / 2, size - 1);
        }
        else if (Vector2.Angle(Vector2.up, dir) <= 45f)
        {
            normalDir = Vector2.up;
            planeCenter = new Vector2(size / 2, 0);
        }
        else if (Vector2.Angle(Vector2.left, dir) <= 45f)
        {
            normalDir = Vector2.left;
            planeCenter = new Vector2(size - 1, size / 2);
        }
        else
        {
            normalDir = Vector2.right;
            planeCenter = new Vector2(0, size / 2);
        }
        Vector2 overshootDir = Vector2.Perpendicular(normalDir).normalized;
        if (Vector2.Dot(overshootDir, dir) > 0) overshootDir *= -1;
        for (int i = 0; i < 2 * size; i++)
        {
            startPoints[i] = CellularVector.Round(planeCenter + (i - size / 2) * overshootDir);
        }
        return normalDir;
    }

    public Vector2Int GetNeightborPoint(Vector2Int point, int verticalDiff, int horizontalDiff)
    {
        int downIndex = (int)pointIndexOnDownPath[point];
        int rightIndex = (int)pointIndexOnRightPath[point];
        int downTargetIndex = Mathf.Clamp(downIndex - verticalDiff, 0, size - 1);
        int rightTargetIndex = Mathf.Clamp(rightIndex + horizontalDiff, 0, size - 1);
        Vector2Int verticalVec = down[downTargetIndex] - down[downIndex];
        Vector2Int horizontalVec = right[rightTargetIndex] - right[rightIndex];
        return point + verticalVec + horizontalVec;
    }

    public List<Vector2Int> GetVerticalPath(Vector2Int start, int maxLength)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int startIndex = (int)pointIndexOnDownPath[start];
        if (maxLength > 0) // up
        {
            int length = Mathf.Min(maxLength, startIndex + 1);
            for (int i = 0; i < length; i++)
            {
                Vector2Int diff = down[startIndex - i] - down[startIndex];
                Vector2Int point = start + diff;
                if (!InRange(point)) break;
                path.Add(point);
            }
        }
        else // down
        {
            int length = Mathf.Min(-maxLength, size - startIndex);
            for (int i = 0; i < length; i++)
            {
                Vector2Int diff = down[startIndex + i] - down[startIndex];
                Vector2Int point = start + diff;
                if (!InRange(point)) break;
                path.Add(point);
            }
        }
        return path;
    }

    public bool InRange(Vector2Int coords)
    {
        return InRange(coords.x, coords.y);
    }

    public bool InRange(int x, int y)
    {
        return x >= 0 && x < size && y >= 0 && y < size;
    }
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
    public float maxVolume;
    public const float defaultMaxVolume = 1;
    public const float compression = 0.15f;
    public const float minFlow = 0.1f;

    public Fluid(CellularAutomata ca, float volume = 1) : base(ca)
    {
        this.volume = volume;
        maxVolume = defaultMaxVolume;
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

        ca.grid[x, y] = null;

        // flow into neighboring cells
        FlowDown(start);
        if (volume <= 0) return;
        FlowDiagonally(start, down, right);
        if (volume <= 0) return;
        FlowSideways(start, -down, right);
        if (volume <= 0) return;
        FlowUp(start, -down);

        // keep the remaining volume in the current cell, otherwise update compression for cells below
        if (volume > 0)
        {
            ca.grid[x, y] = this;
        }
        else
        {
            Vector2Int downCell = CellularVector.Round(start + down);
            ((Fluid)ca.grid[downCell.x, downCell.y]).UpdateCompression(downCell);
        }
    }

    public void UpdateCompression(Vector2Int p)
    {
        Vector2Int upCell = ca.traversingLines.GetNeightborPoint(p, 1, 0);
        if (p != upCell && ca.GetCellType(upCell) == type) maxVolume = ((Fluid)ca.grid[upCell.x, upCell.y]).maxVolume + compression;
        else maxVolume = defaultMaxVolume;

        List<Vector2Int> downPath = ca.traversingLines.GetVerticalPath(p, -ca.size);
        downPath.RemoveAt(0);
        for (int i = 0; i < downPath.Count; i++)
        {
            if (ca.GetCellType(downPath[i]) == type) ((Fluid)ca.grid[downPath[i].x, downPath[i].y]).maxVolume = maxVolume + (i + 1) * compression;
            else return;
        }
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
                if (pCell.volume >= pCell.maxVolume)
                {
                    flowTo.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void FlowSideways(Vector2Int start, Vector2 up, Vector2 right)
    {
        if (volume < minFlow) return;

        List<Vector2Int> flowTo = new List<Vector2Int>
        {
            CellularVector.Round(start + right),
            CellularVector.Round(start - right)
        };

        float totalVolume = volume;

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
            else if (flowToType == type)
            {
                totalVolume += ((Fluid)ca.grid[flowTo[i].x, flowTo[i].y]).volume;
            }
        }

        if (flowTo.Count == 0) return;

        float split = totalVolume / (flowTo.Count + 1);
        for (int i = 0; i < flowTo.Count; i++)
        {
            Vector2Int p = flowTo[i];
            if (ca.grid[p.x, p.y] == null) FlowToEmptyCell(p, split);
            else ((Fluid)ca.grid[p.x, p.y]).volume = split;
        }
        volume = split;
    }

    public void FlowUp(Vector2Int start, Vector2 up)
    {
        if (volume <= maxVolume) return;

        Vector2Int upCell = CellularVector.Round(start + up);

        if (ca.InRange(upCell))
        {
            CellType upCellType = ca.GetCellType(upCell);
            if (upCellType == CellType.Empty)
            {
                FlowToEmptyCell(upCell, volume - maxVolume);
                maxVolume += compression;
            }
            else if (upCellType == type) FlowToFluidCell(upCell, volume - maxVolume, true);
        }
    }

    public float FlowToEmptyCell(Vector2Int p, float maxFlow)
    {
        float transfer = Mathf.Min(defaultMaxVolume, maxFlow);
        volume -= transfer;
        Fluid newCell = (Fluid)NewCell(new object[] { ca, transfer });
        newCell.hasBeenUpdated = true;
        newCell.momentum = momentum;
        ca.grid[p.x, p.y] = newCell;
        newCell.UpdateCompression(p);
        return transfer;
    }

    public float FlowToFluidCell(Vector2Int p, float maxFlow, bool overflow = false)
    {
        Fluid pCell = (Fluid)ca.grid[p.x, p.y];
        float transfer = maxFlow;
        if (!overflow) transfer = Mathf.Min(pCell.maxVolume - pCell.volume, maxFlow);
        if (transfer <= 0) return 0;
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
    public Water(CellularAutomata ca, float volume = 1) : base(ca, volume)
    {
        type = CellType.Water;
    }

    public override Cell NewCell(object[] argv)
    {
        return new Water((CellularAutomata)argv[0], (float)argv[1]);
    }
}

////////// cellular automata grid class

public class CellularAutomata : MonoBehaviour
{
    public int size, scale = 1, fps = 30, maxPathSize = 20;
    public Cell[,] grid;
    public Vector2 gravity; // relative to the local grid

    public TraversingLines traversingLines;

    public GameObject cellPrefab;
    private Image[,] cellsUI;

    // Start is called before the first frame update
    private void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        grid = new Cell[size, size];
        cellsUI = new Image[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = new Vector3(x - size/2, y - size/2, 0);
                cellsUI[x, y] = cell.GetComponent<Image>();
            }
        }

        traversingLines = new TraversingLines(size);
        UpdateGravity();
    }
    public bool needregenerate = false;

    private void UpdateGravity()
    {
        traversingLines.GenerateLines(gravity);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (needregenerate)
        {
            needregenerate = false;
            UpdateGravity();
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (grid[x, y] != null) grid[x, y].hasBeenUpdated = false;

        traversingLines.ShuffleIndexes();
        for (int i = 0; i < 2 * size; i++)
        {
            foreach (int j in traversingLines.shuffledIndexes)
            {
                Vector2Int point = traversingLines.horizontalStartPoints[i] + traversingLines.right[j];
                if (!InRange(point)) continue;
                if (grid[point.x, point.y] != null) grid[point.x, point.y].UpdateCell(point.x, point.y);
            }
        }

        RenderGrid();
    }

    private void RenderGrid()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (grid[x, y] == null)
                    cellsUI[x, y].color = UnityEngine.Color.black;
                else if (grid[x, y].type == CellType.Stone)
                    cellsUI[x, y].color = UnityEngine.Color.gray;
                else if (grid[x, y].type == CellType.Water)
                    cellsUI[x, y].color = UnityEngine.Color.blue;
            }
        }
    }

    public bool InRange(Vector2Int coords)
    {
        return traversingLines.InRange(coords);
    }

    public bool InRange(int x, int y)
    {
        return traversingLines.InRange(x, y);
    }

    public CellType GetCellType(Vector2Int p)
    {
        return !InRange(p) || grid[p.x, p.y] == null ? CellType.Empty : grid[p.x, p.y].type;
    }
}