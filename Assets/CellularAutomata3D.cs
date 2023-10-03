using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.AI;

public enum CellType
{
    Empty,
    Stone,
    Water
}

public class TraversingLines
{
    private int size;
    private static float epsilon = 1e-4f;

    public Vector3Int[,] verticalStartPoints;
    public Vector3Int[] horizontalStartPoints;
    public Vector3 downDir;
    public Vector3Int[] down;
    public Vector3Int[,] horizontal;
    public Hashtable pointIndexOnDownPath = new Hashtable();
    public Hashtable pointIndexOnHorizontalPath = new Hashtable();

    public int[][] shuffledIndexes;

    public TraversingLines(int size)
    {
        this.size = size;
        verticalStartPoints = new Vector3Int[2 * size, 2 * size];
        horizontalStartPoints = new Vector3Int[2 * size];
        down = new Vector3Int[size];
        horizontal = new Vector3Int[size, size];
        shuffledIndexes = new int[size * size][];
        for (int i = 0; i < size * size; i++) shuffledIndexes[i] = new int[2];
    }

    public void ShuffleIndexes()
    {
        int totalSize = size * size;
        int[] shuffle = new int[totalSize];
        for (int i = 0; i < totalSize; i++) shuffle[i] = i;
        for (int i = totalSize; i > 1; i--)
        {
            int p = Random.Range(0, i);
            int tmp = shuffle[i - 1];
            shuffle[i - 1] = shuffle[p];
            shuffle[p] = tmp;
        }
        for (int i = 0; i < totalSize; i++)
        {
            shuffledIndexes[i][0] = shuffle[i] / size;
            shuffledIndexes[i][1] = shuffle[i] % size;
        }
    }

    public void GenerateLines(Vector3 downDir)
    {
        // avoid multiple start point planes in 45 degees tilt situations
        if (downDir.x == downDir.y) downDir.y += epsilon;
        if (downDir.x == downDir.z) downDir.z += epsilon;
        if (downDir.y == downDir.z) downDir.z += epsilon;

        //////// vertical traversing ////////

        downDir = downDir.normalized;
        this.downDir = downDir;
        down = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(downDir * size * 1.5f)).GetRange(0, size).ToArray();

        Vector3 downNormalDir = GeneratePlaneStartPoints(size, downDir, ref verticalStartPoints);

        //////// horizontal traversing ////////

        // get horizontal plane axis
        Vector3 iDir = Vector3.ProjectOnPlane(Vector3.right, -downDir).normalized;
        Vector3 jDir = Vector3.ProjectOnPlane(Vector3.forward, -downDir).normalized;
        Vector3 iPlaneCenter, jPlaneCenter;
        Vector3 iNormalDir = GetPlaneNormal(size, iDir, out iPlaneCenter).normalized;
        Vector3 jNormalDir = GetPlaneNormal(size, jDir, out jPlaneCenter).normalized;

        // make sure the start points include the lowest point
        if (Vector3.Dot(downDir, iNormalDir) > 0)
        {
            iDir *= -1;
            iPlaneCenter += iNormalDir * size;
            iNormalDir *= -1;
        }
        if (Vector3.Dot(downDir, jNormalDir) > 0)
        {
            jDir *= -1;
            jPlaneCenter += jNormalDir * size;
            jNormalDir *= -1;
        }

        // create start points
        Vector3 horizontalStartPointsDir = Vector3.Cross(iNormalDir, jNormalDir).normalized;
        if (Vector3.Dot(downDir, horizontalStartPointsDir) > 0) horizontalStartPointsDir *= -1;
        Vector3 horizontalStartPointsOrigin = iPlaneCenter - (jNormalDir + horizontalStartPointsDir) * (size / 2);
        for (int i = 0; i < 2 * size; i++)
        {
            horizontalStartPoints[i] = CellularVector3D.Round(horizontalStartPointsOrigin + i * horizontalStartPointsDir);
        }

        // create the horizontal plane
        Vector3Int[] iVec = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(iDir * size * 1.5f)).GetRange(0, size).ToArray();
        Vector3Int[] jVec = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(jDir * size * 1.5f)).GetRange(0, size).ToArray();
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                horizontal[i, j] = iVec[i] + jVec[j];

        // fill point index on path hashtables
        pointIndexOnDownPath.Clear();
        for (int i = 0; i < 2 * size; i++)
        {
            for (int j = 0; j < 2 * size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    Vector3Int point = verticalStartPoints[i, j] + down[k];
                    if (InRange(point)) pointIndexOnDownPath[point] = k;
                }
            }
        }
        pointIndexOnHorizontalPath.Clear();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < 2 * size; k++)
                {
                    Vector3Int point = horizontalStartPoints[k] + horizontal[i, j];
                    if (InRange(point)) pointIndexOnHorizontalPath[point] = new int[] {i, j};
                }
            }
        }
    }

    private static Vector3 GeneratePlaneStartPoints(int size, Vector3 dir, ref Vector3Int[,] startPoints) // returns normal to the start points plane
    {
        Vector3 normalDir;
        Vector3 iOvershootDir, jOvershootDir;
        Vector3 planeCenter;
        if (Vector3.Angle(Vector3.down, dir) <= 45f)
        {
            normalDir = Vector3.down;
            iOvershootDir = Vector3.right;
            jOvershootDir = Vector3.forward;
            planeCenter = new Vector3(size / 2, size - 1, size / 2);
        }
        else if (Vector3.Angle(Vector3.up, dir) <= 45f)
        {
            normalDir = Vector3.up;
            iOvershootDir = Vector3.right;
            jOvershootDir = Vector3.forward;
            planeCenter = new Vector3(size / 2, 0, size / 2);
        }
        else if (Vector3.Angle(Vector3.left, dir) <= 45f)
        {
            normalDir = Vector3.left;
            iOvershootDir = Vector3.up;
            jOvershootDir = Vector3.forward;
            planeCenter = new Vector3(size - 1, size / 2, size / 2);
        }
        else if (Vector3.Angle(Vector3.right, dir) <= 45f)
        {
            normalDir = Vector3.right;
            iOvershootDir = Vector3.up;
            jOvershootDir = Vector3.forward;
            planeCenter = new Vector3(0, size / 2, size / 2);
        }
        else if (Vector3.Angle(Vector3.forward, dir) <= 45f)
        {
            normalDir = Vector3.forward;
            iOvershootDir = Vector3.up;
            jOvershootDir = Vector3.right;
            planeCenter = new Vector3(size / 2, size / 2, 0);
        }
        else
        {
            normalDir = Vector3.back;
            iOvershootDir = Vector3.up;
            jOvershootDir = Vector3.right;
            planeCenter = new Vector3(size / 2, size / 2, size - 1);
        }
        if (Vector3.Dot(iOvershootDir, dir) > 0) iOvershootDir *= -1;
        if (Vector3.Dot(jOvershootDir, dir) > 0) jOvershootDir *= -1;
        for (int i = 0; i < 2 * size; i++)
        {
            for (int j = 0; j < 2 * size; j++)
            {
                startPoints[i, j] = CellularVector3D.Round(planeCenter + (i - size / 2) * iOvershootDir + (j - size / 2) * jOvershootDir);
            }
        }
        return normalDir;
    }

    private static Vector3 GetPlaneNormal(int size, Vector3 dir, out Vector3 planeCenter)
    {
        Vector3 normalDir;
        if (Vector3.Angle(Vector3.down, dir) <= 45f)
        {
            normalDir = Vector3.down;
            planeCenter = new Vector3(size / 2, size - 1, size / 2);
        }
        else if (Vector3.Angle(Vector3.up, dir) <= 45f)
        {
            normalDir = Vector3.up;
            planeCenter = new Vector3(size / 2, 0, size / 2);
        }
        else if (Vector3.Angle(Vector3.left, dir) <= 45f)
        {
            normalDir = Vector3.left;
            planeCenter = new Vector3(size - 1, size / 2, size / 2);
        }
        else if (Vector3.Angle(Vector3.right, dir) <= 45f)
        {
            normalDir = Vector3.right;
            planeCenter = new Vector3(0, size / 2, size / 2);
        }
        else if (Vector3.Angle(Vector3.forward, dir) <= 45f)
        {
            normalDir = Vector3.forward;
            planeCenter = new Vector3(size / 2, size / 2, 0);
        }
        else
        {
            normalDir = Vector3.back;
            planeCenter = new Vector3(size / 2, size / 2, size - 1);
        }
        return normalDir;
    }

    // returns the same point if the target point is out of bounds
    public Vector3Int GetNeightborPoint(Vector3Int point, int verticalDiff, int iDiff, int jDiff)
    {
        int downIndex = (int)pointIndexOnDownPath[point];
        int downTargetIndex = downIndex - verticalDiff;
        if (downTargetIndex < 0 || downTargetIndex >= size) return point;
        Vector3Int verticalVec = down[downTargetIndex] - down[downIndex];

        int[] horizontalIndex = (int[])pointIndexOnHorizontalPath[point];
        int[] horizontalTargetIndex = new int[] {horizontalIndex[0] + iDiff, horizontalIndex[1] + jDiff};
        if (horizontalTargetIndex[0] < 0 || horizontalTargetIndex[0] >= size) return point;
        if (horizontalTargetIndex[1] < 0 || horizontalTargetIndex[1] >= size) return point;
        Vector3Int horizontalVec = horizontal[horizontalTargetIndex[0], horizontalTargetIndex[1]] - horizontal[horizontalIndex[0], horizontalIndex[1]];

        return point + verticalVec + horizontalVec;
    }

    public List<Vector3Int> GetVerticalPath(Vector3Int start, int maxLength)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        int startIndex = (int)pointIndexOnDownPath[start];
        if (maxLength > 0) // up
        {
            int length = Mathf.Min(maxLength, startIndex + 1);
            for (int i = 0; i < length; i++)
            {
                Vector3Int diff = down[startIndex - i] - down[startIndex];
                Vector3Int point = start + diff;
                if (!InRange(point)) break;
                path.Add(point);
            }
        }
        else // down
        {
            int length = Mathf.Min(-maxLength, size - startIndex);
            for (int i = 0; i < length; i++)
            {
                Vector3Int diff = down[startIndex + i] - down[startIndex];
                Vector3Int point = start + diff;
                if (!InRange(point)) break;
                path.Add(point);
            }
        }
        return path;
    }

    public bool InRange(Vector3Int coords)
    {
        return InRange(coords.x, coords.y, coords.z);
    }

    public bool InRange(int x, int y, int z)
    {
        return x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size;
    }
}

////////// abstract cell class, cell categories classes and cell sub-categories classes

public abstract class Cell
{
    public CellularAutomata3D ca;
    public CellType type;
    public bool hasBeenSimulated;

    public Cell(CellularAutomata3D ca)
    {
        this.ca = ca;
        this.hasBeenSimulated = true;
    }

    public abstract Cell NewCell(object[] argv);

    public virtual void SimulateCell(Vector3Int p)
    {
        hasBeenSimulated = true;
    }
}

public abstract class StaticCell : Cell
{
    public StaticCell(CellularAutomata3D ca) : base(ca) { }
}

public abstract class DynamicCell : Cell
{
    public float momentum;

    public DynamicCell(CellularAutomata3D ca) : base(ca)
    {
        momentum = 0;
    }

    public void ApplyForces()
    {
        momentum += ca.gravity.magnitude * (1.0f / ca.fps);
        if (momentum < 1) momentum = 1;
    }
}

public abstract class Fluid : DynamicCell
{
    public float volume;
    public float capacity;
    public static float maxVolume = 3f;
    public static float defaultMaxVolume = 1f;
    public static float compression = 0.15f;
    public static float minFlow = 0.1f;

    public Fluid(CellularAutomata3D ca, float volume = 1f) : base(ca)
    {
        this.volume = volume;
        capacity = defaultMaxVolume;
    }

    public override void SimulateCell(Vector3Int p)
    {
        if (hasBeenSimulated) return;
        hasBeenSimulated = true;

        // apply acceletation due to gravity
        ApplyForces();

        // flow into neighboring cells
        FlowDown(p);
        if (volume > 0) FlowDiagonally(p);

        // remove empty cell
        if (volume <= 0) ca.grid[p.x, p.y, p.z] = null;
    }

    public void FlowDown(Vector3Int start)
    {
        // get the fall path from point start with the current momentum
        List<Vector3Int> fallPath = ca.traversingLines.GetVerticalPath(start, -(int)momentum - 1);
        fallPath.RemoveAt(0);

        // check the farthest distance the cell can fall down(momentum direction) to using the bresenham fall line
        int farthestPoint = -1; // the farthest point
        bool isFarthestPointFluid = false;
        for (int i = 0; i < fallPath.Count; i++)
        {
            Vector3Int p = fallPath[i];
            if (ca.InRange(p))
            {
                if (ca.grid[p.x, p.y, p.z] == null) isFarthestPointFluid = false;
                else if (ca.grid[p.x, p.y, p.z].type == type) isFarthestPointFluid = true;
                else break;
                farthestPoint = i;
            }
            else break;
        }

        // reset the momentum and deviation if the cell hit the ground, otherwise update the deviation vector
        if (fallPath.Count < (int)momentum || isFarthestPointFluid || farthestPoint != fallPath.Count - 1) momentum = 0;

        // flow down to the cells on the fallPath starting from the farthest fall point
        for (int i = farthestPoint; i >= 0; i--)
        {
            Vector3Int p = fallPath[i];
            if (ca.grid[p.x, p.y, p.z] == null) FlowToEmptyCell(p, volume);
            else FlowToFluidCell(p, volume);
            if (volume <= 0) return;
        }
    }

    public void FlowDiagonally(Vector3Int start)
    {
        List<Vector3Int> flowTo = new List<Vector3Int>
        {
            ca.traversingLines.GetNeightborPoint(start, -1, 1, 0),
            ca.traversingLines.GetNeightborPoint(start, -1, -1, 0),
            ca.traversingLines.GetNeightborPoint(start, -1, 0, 1),
            ca.traversingLines.GetNeightborPoint(start, -1, 0, -1)
        };

        for (int i = 0; i < flowTo.Count; i++)
        {
            CellType flowToType = ca.GetCellType(flowTo[i]);
            if (flowTo[i] == start || !ca.InRange(flowTo[i]) || (flowToType != CellType.Empty && flowToType != type))
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
                Vector3Int p = flowTo[i];
                if (ca.grid[p.x, p.y, p.z] == null) FlowToEmptyCell(p, split);
                else FlowToFluidCell(p, split);
                Fluid pCell = (Fluid)ca.grid[p.x, p.y, p.z];
                if (pCell.volume >= pCell.capacity)
                {
                    flowTo.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public float FlowToEmptyCell(Vector3Int p, float maxFlow)
    {
        float transfer = Mathf.Min(defaultMaxVolume, maxFlow);
        volume -= transfer;
        Fluid newCell = (Fluid)NewCell(new object[] { ca, transfer });
        newCell.hasBeenSimulated = true;
        newCell.momentum = momentum;
        ca.grid[p.x, p.y, p.z] = newCell;
        return transfer;
    }

    public float FlowToFluidCell(Vector3Int p, float maxFlow, bool overflow = false)
    {
        Fluid pCell = (Fluid)ca.grid[p.x, p.y, p.z];
        float transfer = maxFlow;
        if (!overflow) transfer = Mathf.Min(pCell.capacity - pCell.volume, maxFlow);
        if (transfer <= 0) return 0;
        pCell.volume += transfer;
        volume -= transfer;
        return transfer;
    }
}

////////// cell types

public class Stone : StaticCell
{
    public Stone(CellularAutomata3D ca) : base(ca)
    {
        type = CellType.Stone;
    }

    public override Cell NewCell(object[] argv)
    {
        return new Stone((CellularAutomata3D)argv[0]);
    }
}

public class Water : Fluid
{
    public Water(CellularAutomata3D ca, float volume = 1) : base(ca, volume)
    {
        type = CellType.Water;
    }

    public override Cell NewCell(object[] argv)
    {
        return new Water((CellularAutomata3D)argv[0], (float)argv[1]);
    }
}

////////// cellular automata grid class

public class CellularAutomata3D : MonoBehaviour
{
    public int size, scale = 1, fps = 30;
    public Cell[,,] grid;
    public Vector3 gravity; // relative to the local grid

    public TraversingLines traversingLines;

    public GameObject cellPrefab;
    private Renderer[,,] cellsUI;

    // Start is called before the first frame update
    private void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        grid = new Cell[size, size, size];
        cellsUI = new Renderer[size, size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    GameObject cell = Instantiate(cellPrefab, transform);
                    cell.transform.localPosition = new Vector3(x - size / 2, y - size / 2, z - size / 2);
                    cellsUI[x, y, z] = cell.GetComponent<Renderer>();
                }
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
                for (int z = 0; z > size; z++)
                    if (grid[x, y, z] != null) grid[x, y, z].hasBeenSimulated = false;

        // update compression
        for (int i = 0; i < 2 * size; i++)
        {
            for (int j = 0; j < 2 * size; j++)
            {
                float nextVolume = Fluid.defaultMaxVolume;
                for (int k = 0; k < size; k++)
                {
                    Vector3Int point = traversingLines.verticalStartPoints[i, j] + traversingLines.down[k];
                    if (GetCellType(point) != CellType.Water)
                    {
                        nextVolume = Fluid.defaultMaxVolume;
                        continue;
                    }
                    ((Fluid)grid[point.x, point.y, point.z]).capacity = nextVolume;
                    nextVolume += Fluid.compression;
                }
            }
        }

        // simulate
        traversingLines.ShuffleIndexes();
        for (int k = 0; k < 2 * size; k++)
        {
            foreach (int[] index in traversingLines.shuffledIndexes)
            {
                Vector3Int point = traversingLines.horizontalStartPoints[k] + traversingLines.horizontal[index[0], index[1]];
                if (!InRange(point)) continue;
                if (grid[point.x, point.y, point.z] != null) grid[point.x, point.y, point.z].SimulateCell(point);
            }
        }

        // balance water volume on horizontally adjacent cells and flow sideways if possible
        for (int k = 0; k < 2 * size; k++)
        {
            Hashtable visited = new Hashtable();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Vector3Int point = traversingLines.horizontalStartPoints[k] + traversingLines.horizontal[i, j];
                    if (visited.ContainsKey(point)) continue;

                    // get and balance water body
                    if (InRange(point) && GetCellType(point) == CellType.Water && ((Fluid)grid[point.x, point.y, point.z]).momentum == 0)
                    {
                        List<Vector3Int> waterBody = new List<Vector3Int>();
                        List<Vector3Int> silhouette = new List<Vector3Int>();
                        GetAdjacentCellsRecursive(visited, waterBody, silhouette, point);
                        if (waterBody.Count > 0) BalanceAdjacentCells(waterBody, silhouette);
                    }
                }
            }
        }

        // flow up excess volume
        for (int i = 0; i < 2 * size; i++)
        {
            for (int j = 0; j < 2 * size; j++)
            {
                for (int k = 1; k < size; k++)
                {
                    Vector3Int point = traversingLines.verticalStartPoints[i, j] + traversingLines.down[k];
                    if (GetCellType(point) == CellType.Water)
                    {
                        Fluid cell = (Fluid)grid[point.x, point.y, point.z];
                        if (cell.volume <= cell.capacity) continue;

                        Vector3Int upCell = traversingLines.verticalStartPoints[i, j] + traversingLines.down[k - 1];
                        if (!InRange(upCell)) continue;

                        CellType upCellType = GetCellType(upCell);
                        if (upCellType == CellType.Empty) cell.FlowToEmptyCell(upCell, cell.volume - cell.capacity);
                        else if (upCellType == CellType.Water)
                        {
                            float transfer = Mathf.Min(cell.volume - cell.capacity, Fluid.maxVolume - ((Fluid)grid[upCell.x, upCell.y, upCell.z]).volume);
                            cell.FlowToFluidCell(upCell, transfer, true);
                        }
                    }
                }
            }
        }

        RenderGrid();
    }

    private void GetAdjacentCellsRecursive(Hashtable visited, List<Vector3Int> waterBody, List<Vector3Int> silhouette, Vector3Int p)
    {
        if (!InRange(p) || visited.ContainsKey(p)) return;
        visited[p] = true;

        if (GetCellType(p) == CellType.Empty)
        {
            silhouette.Add(p);
        }
        else if (GetCellType(p) == CellType.Water && ((Fluid)grid[p.x, p.y, p.z]).momentum == 0)
        {
            waterBody.Add(p);

            Vector3Int adjacent;
            adjacent = traversingLines.GetNeightborPoint(p, 0, 1, 0);
            if (p != adjacent) GetAdjacentCellsRecursive(visited, waterBody, silhouette, adjacent);
            adjacent = traversingLines.GetNeightborPoint(p, 0, -1, 0);
            if (p != adjacent) GetAdjacentCellsRecursive(visited, waterBody, silhouette, adjacent);
            adjacent = traversingLines.GetNeightborPoint(p, 0, 0, 1);
            if (p != adjacent) GetAdjacentCellsRecursive(visited, waterBody, silhouette, adjacent);
            adjacent = traversingLines.GetNeightborPoint(p, 0, 0, -1);
            if (p != adjacent) GetAdjacentCellsRecursive(visited, waterBody, silhouette, adjacent);
        }
    }

    private void BalanceAdjacentCells(List<Vector3Int> waterBody, List<Vector3Int> silhouette)
    {
        // get total volume
        float totalVolume = 0;
        foreach (Vector3Int p in waterBody) totalVolume += ((Fluid)grid[p.x, p.y, p.z]).volume;
        float split = totalVolume / waterBody.Count;

        // flow to empty cells on the silhouette
        if (split > Fluid.minFlow)
        {
            foreach (Vector3Int p in silhouette) grid[p.x, p.y, p.z] = new Water(this);
            waterBody.AddRange(silhouette);
        }
        split = totalVolume / waterBody.Count;

        // split total volume between all adjacent cells
        foreach (Vector3Int p in waterBody) ((Fluid)grid[p.x, p.y, p.z]).volume = split;
    }

    private void RenderGrid()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (grid[x, y, z] == null)
                        cellsUI[x, y, z].material.color = UnityEngine.Color.black;
                    else if (grid[x, y, z].type == CellType.Stone)
                        cellsUI[x, y, z].material.color = UnityEngine.Color.gray;
                    else if (grid[x, y, z].type == CellType.Water)
                        cellsUI[x, y, z].material.color = new UnityEngine.Color(0.3f, 0.3f, 1f - ((Fluid)grid[x, y, z]).volume * 0.1f, 1f);
                }
            }
        }
    }

    public bool InRange(Vector3Int coords)
    {
        return traversingLines.InRange(coords);
    }

    public bool InRange(int x, int y, int z)
    {
        return traversingLines.InRange(x, y, z);
    }

    public CellType GetCellType(Vector3Int p)
    {
        return !InRange(p) || grid[p.x, p.y, p.z] == null ? CellType.Empty : grid[p.x, p.y, p.z].type;
    }
}