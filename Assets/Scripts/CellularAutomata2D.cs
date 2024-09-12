using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class TraversingLines
{
    private int size;
    private static float epsilon = 1e-4f;

    public Vector2Int[] verticalStartPoints;
    public Vector2Int[] horizontalStartPoints;
    public Vector2 downDir;
    public Vector2Int[] down;
    public Vector2Int[] horizontal;
    public Hashtable pointIndexOnDownPath = new Hashtable();
    public Hashtable pointIndexOnHorizontalPath = new Hashtable();

    public int[] shuffledIndexes;

    public TraversingLines(int size)
    {
        this.size = size;
        verticalStartPoints = new Vector2Int[2 * size];
        horizontalStartPoints = new Vector2Int[2 * size];
        down = new Vector2Int[size];
        horizontal = new Vector2Int[size];
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
        downDir = downDir.normalized;

        // avoid multiple start point planes in 45 degees tilt situations
        if (downDir.x == downDir.y) downDir.y += epsilon;


        //////// vertical traversing ////////

        this.downDir = downDir;
        down = CellularVector2D.Bresenham(Vector2Int.zero, CellularVector2D.Round(downDir * 2 * size)).GetRange(0, size).ToArray();

        Vector2 downNormalDir = GenerateStartPoints(size, downDir, ref verticalStartPoints);

        //////// horizontal traversing ////////

        Vector2 horizontalDir = Vector2.Perpendicular(downDir).normalized;
        if (Vector2.Dot(downNormalDir, horizontalDir) < 0) horizontalDir *= -1;
        horizontal = CellularVector2D.Bresenham(Vector2Int.zero, CellularVector2D.Round(horizontalDir * size * 1.5f)).GetRange(0, size).ToArray();
        GenerateStartPoints(size, horizontalDir, ref horizontalStartPoints);

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
        pointIndexOnHorizontalPath.Clear();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < 2 * size; j++)
            {
                Vector2Int point = horizontalStartPoints[j] + horizontal[i];
                if (InRange(point)) pointIndexOnHorizontalPath[point] = i;
            }
        }

        if (pointIndexOnDownPath.Count != size * size)
            Debug.LogError("Error: not all points are in the range of the vertical traversing lines");
        if (pointIndexOnHorizontalPath.Count != size * size)
            Debug.LogError("Error: not all points are in the range of the horizontal traversing lines");
    }

    private static Vector2 GenerateStartPoints(int size, Vector2 dir, ref Vector2Int[] startPoints) // returns normal to the start points plane
    {
        Vector2 normalDir;
        Vector2 overshootDir;
        Vector2 planeCenter;

        // find the edge with the inside normal closest to the direction of the gravity
        List<float> angles = new List<float>() {
            Vector2.Angle(Vector2.down, dir),
            Vector2.Angle(Vector2.up, dir),
            Vector2.Angle(Vector2.left, dir),
            Vector2.Angle(Vector2.right, dir)
        };
        int minIndex = 0;
        for (int i = 1; i < angles.Count; i++)
            if (angles[i] < angles[minIndex])
                minIndex = i;

        if (minIndex == 0)
        {
            normalDir = Vector2.down;
            overshootDir = Vector2.right;
            planeCenter = new Vector2(size / 2, size - 1);
        }
        else if (minIndex == 1)
        {
            normalDir = Vector2.up;
            overshootDir = Vector2.right;
            planeCenter = new Vector2(size / 2, 0);
        }
        else if (minIndex == 2)
        {
            normalDir = Vector2.left;
            overshootDir = Vector2.up;
            planeCenter = new Vector2(size - 1, size / 2);
        }
        else
        {
            normalDir = Vector2.right;
            overshootDir = Vector2.up;
            planeCenter = new Vector2(0, size / 2);
        }
        if (Vector2.Dot(overshootDir, dir) > 0) overshootDir *= -1;
        for (int i = 0; i < 2 * size; i++)
        {
            startPoints[i] = CellularVector2D.Round(planeCenter + (i - size / 2) * overshootDir);
        }
        return normalDir;
    }

    // returns the same point if the target point is out of bounds of the traversing line (not necessarily out of bounds of the matrix)
    public Vector2Int GetNeightborPoint(Vector2Int point, int verticalDiff, int horizontalDiff)
    {
        int downIndex = (int)pointIndexOnDownPath[point];
        int downTargetIndex = downIndex - verticalDiff;
        if (downTargetIndex < 0 || downTargetIndex >= size) return point;
        Vector2Int verticalVec = down[downTargetIndex] - down[downIndex];

        int horizontalIndex = (int)pointIndexOnHorizontalPath[point];
        int horizontalTargetIndex = horizontalIndex + horizontalDiff;
        if (horizontalTargetIndex < 0 || horizontalTargetIndex >= size) return point;
        Vector2Int horizontalVec = horizontal[horizontalTargetIndex] - horizontal[horizontalIndex];

        return point + verticalVec + horizontalVec;
    }

    public List<Vector2Int> GetVerticalPath(Vector2Int start, int maxLength, out bool outOfBounds)
    {
        outOfBounds = false;
        List<Vector2Int> path = new List<Vector2Int>();
        int startIndex = (int)pointIndexOnDownPath[start];
        if (maxLength > 0) // up
        {
            int length = Mathf.Min(maxLength, startIndex + 1);
            if (length < maxLength) outOfBounds = true;
            for (int i = 0; i < length; i++)
            {
                Vector2Int diff = down[startIndex - i] - down[startIndex];
                Vector2Int point = start + diff;
                if (!InRange(point))
                {
                    outOfBounds = true;
                    break;
                }
                path.Add(point);
            }
        }
        else // down
        {
            int length = Mathf.Min(-maxLength, size - startIndex);
            if (length < -maxLength) outOfBounds = true;
            for (int i = 0; i < length; i++)
            {
                Vector2Int diff = down[startIndex + i] - down[startIndex];
                Vector2Int point = start + diff;
                if (!InRange(point))
                {
                    outOfBounds = true;
                    break;
                }
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

public struct Cell
{
    public bool hasBeenSimulated;
    public float volume; // 0 = empty cell, -1 = stone cell, >0 = water cell
    public float capacity;
    public float momentum;

    public Cell(float volume = 0f, float capacity = 1f, float momentum = 0f)
    {
        hasBeenSimulated = true;
        this.volume = volume;
        this.capacity = capacity;
        this.momentum = momentum;
    }

    public void ApplyForces(CellularAutomata2D ca)
    {
        momentum += ca.gravity.magnitude * (1 / ca.fps);
        momentum = Mathf.Clamp(momentum, 1, ca.terminalVelocity);
    }

    public void SimulateCell(CellularAutomata2D ca, Vector2Int p)
    {
        if (hasBeenSimulated) return;
        hasBeenSimulated = true;

        // flow out of bounds if on the edge
        if (p.x == 0 || p.x == ca.size - 1 || p.y == 0 || p.y == ca.size - 1)
        {
            FlowOutOfBounds(ca);
            ca.grid2d.UpdateVoxel(p, volume);
            return;
        }

        float prevVolume = volume;

        // apply acceletation due to gravity
        ApplyForces(ca);

        // flow into neighboring cells
        FlowDown(ca, p);
        if (volume > 0) FlowDiagonally(ca, p);

        // update voxel
        if (prevVolume != volume) ca.grid2d.UpdateVoxel(p, volume);

        if (volume <= 0) volume = 0f;
    }

    public void FlowDown(CellularAutomata2D ca, Vector2Int start)
    {
        // get the fall path from point start with the current momentum
        bool outOfBounds = false;
        List<Vector2Int> fallPath = ca.traversingLines.GetVerticalPath(start, -(int)momentum - 1, out outOfBounds);
        fallPath.RemoveAt(0);

        // check the farthest distance the cell can fall down(momentum direction) to using the bresenham fall line
        int farthestPoint = -1; // the farthest point
        bool isFarthestPointFluid = false;
        for (int i = 0; i < fallPath.Count; i++)
        {
            Vector2Int p = fallPath[i];
            if (ca.InRange(p))
            {
                if (ca.grid[p.x, p.y].volume == 0f) isFarthestPointFluid = false;
                else if (ca.grid[p.x, p.y].volume > 0f) isFarthestPointFluid = true;
                else break;
                farthestPoint = i;
            }
            else break;
        }

        // flow out of bounds
        if (outOfBounds && farthestPoint == fallPath.Count - 1)
        {
            FlowOutOfBounds(ca);
            ca.grid2d.UpdateVoxel(start, volume);
            return;
        }

        // reset the momentum if the cell hit the ground, otherwise update the deviation vector
        if (fallPath.Count < (int)momentum || isFarthestPointFluid || farthestPoint != fallPath.Count - 1)
        {
            momentum = 0f;
        }

        // flow down to the cells on the fallPath starting from the farthest fall point
        for (int i = farthestPoint; i >= 0; i--)
        {
            Vector2Int p = fallPath[i];
            if (ca.grid[p.x, p.y].volume == 0f) FlowToEmptyCell(ca, p, volume);
            else FlowToFluidCell(ca, p, volume);
            if (volume <= 0f) return;
        }
    }

    public void FlowDiagonally(CellularAutomata2D ca, Vector2Int start)
    {
        List<Vector2Int> flowTo = new List<Vector2Int>
        {
            ca.traversingLines.GetNeightborPoint(start, -1, 1),
            ca.traversingLines.GetNeightborPoint(start, -1, -1)
        };

        for (int i = 0; i < flowTo.Count; i++)
        {
            if (flowTo[i] == start || !ca.InRange(flowTo[i]) || ca.grid[flowTo[i].x, flowTo[i].y].volume == -1f)
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
                if (ca.grid[p.x, p.y].volume == 0f) FlowToEmptyCell(ca, p, split);
                else FlowToFluidCell(ca, p, split);
                if (ca.grid[p.x, p.y].volume >= ca.grid[p.x, p.y].capacity)
                {
                    flowTo.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public float FlowToEmptyCell(CellularAutomata2D ca, Vector2Int p, float maxFlow)
    {
        float transfer = Mathf.Min(1f, maxFlow);
        volume -= transfer;
        ca.grid[p.x, p.y].volume = transfer;
        ca.grid[p.x, p.y].hasBeenSimulated = true;
        ca.grid[p.x, p.y].momentum = momentum;
        ca.grid2d.UpdateVoxel(p, transfer);
        return transfer;
    }

    public float FlowToFluidCell(CellularAutomata2D ca, Vector2Int p, float maxFlow, bool overflow = false)
    {
        float transfer = maxFlow;
        if (!overflow) transfer = Mathf.Min(ca.grid[p.x, p.y].capacity - ca.grid[p.x, p.y].volume, maxFlow);
        if (transfer <= 0) return 0;
        ca.grid[p.x, p.y].volume += transfer;
        ca.grid2d.UpdateVoxel(p, ca.grid[p.x, p.y].volume);
        volume -= transfer;
        return transfer;
    }

    public void FlowOutOfBounds(CellularAutomata2D ca)
    {
        ca.totalVolume -= volume;
        volume = 0f;
    }
}

public class CellularAutomata2D : MonoBehaviour
{
    public int size;
    public float fps = 30;
    [HideInInspector]
    public Cell[,] grid;
    public Vector2 gravity; // relative to the local grid
    public TraversingLines traversingLines;
    public Grid2D grid2d;
    public float initialTotalVolume;
    public float totalVolume;

    public int terminalVelocity = 3;
    public float maxVolume = 3f;
    public float compression = 0.15f;
    public float minFlow = 0.1f;

    public void GenerateEnv()
    {
        grid = new Cell[size, size];
        traversingLines = new TraversingLines(size);
        UpdateGravity(gravity);
        totalVolume = 0f;
        initialTotalVolume = 0f;
    }

    public void UpdateGravity(Vector2 newDir)
    {
        gravity = newDir;
        traversingLines.GenerateLines(gravity);
    }

    public void SimulateStep()
    {
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                for (int z = 0; z < size; z++)
                    if (grid[x, y].volume > 0f) grid[x, y].hasBeenSimulated = false;

        // update compression
        Parallel.For(0, 2 * size, i =>
        {
            float nextVolume = 1f;
            for (int j = 0; j < size; j++)
            {
                Vector2Int point = traversingLines.verticalStartPoints[i] + traversingLines.down[j];
                if (!InRange(point) || grid[point.x, point.y].volume <= 0f)
                {
                    nextVolume = 1f;
                    continue;
                }
                grid[point.x, point.y].capacity = nextVolume;
                nextVolume += compression;
            }
        });

        // simulate
        traversingLines.ShuffleIndexes();
        int layers = (2 * size) / terminalVelocity;
        if ((2 * size) % terminalVelocity != 0) layers += 1;
        int evenLayers = layers % 2 == 0 ? layers / 2 : layers / 2 + 1;
        int oddLayers = layers / 2;
        bool[,] visited = new bool[size, size];
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                visited[i, j] = false;
        Parallel.For(0, evenLayers, t =>
        {
            for (int k = 0; k < terminalVelocity; k++)
            {
                foreach (int i in traversingLines.shuffledIndexes)
                {
                    int startPointIndex = k + t * 2 * terminalVelocity;
                    if (startPointIndex >= traversingLines.horizontalStartPoints.Length) continue;
                    Vector2Int point = traversingLines.horizontalStartPoints[startPointIndex] + traversingLines.horizontal[i];
                    if (!InRange(point)) continue;
                    if (grid[point.x, point.y].volume > 0f) grid[point.x, point.y].SimulateCell(this, point);
                    visited[point.x, point.y] = true;
                }
            }
        });
        Parallel.For(0, oddLayers, t =>
        {
            for (int k = 0; k < terminalVelocity; k++)
            {
                if (k + (t * 2 + 1) * terminalVelocity >= 2 * size) break;
                foreach (int i in traversingLines.shuffledIndexes)
                {
                    Vector2Int point = traversingLines.horizontalStartPoints[k + (t * 2 + 1) * terminalVelocity] + traversingLines.horizontal[i];
                    if (!InRange(point)) continue;
                    if (grid[point.x, point.y].volume > 0f) grid[point.x, point.y].SimulateCell(this, point);
                    visited[point.x, point.y] = true;
                }
            }
        });

        // balance water volume on horizontally adjacent cells and flow sideways if possible
        Parallel.For(0, 2 * size, k =>
        {
            Hashtable visited = new Hashtable();
            for (int i = 0; i < size; i++)
            {
                Vector2Int point = traversingLines.horizontalStartPoints[k] + traversingLines.horizontal[i];
                if (visited.ContainsKey(point)) continue;

                // get and balance water body
                if (InRange(point) && grid[point.x, point.y].volume > 0f && grid[point.x, point.y].momentum == 0f)
                {
                    List<Vector2Int> waterBody = new List<Vector2Int>();
                    List<Vector2Int> silhouette = new List<Vector2Int>();
                    GetAdjacentCellsBfs(visited, waterBody, silhouette, point);
                    if (waterBody.Count > 0) BalanceAdjacentCells(waterBody, silhouette);
                }
            }
        });

        // flow up excess volume
        Parallel.For(0, 2 * size, i =>
        {
            for (int k = 1; k < size; k++)
            {
                Vector2Int point = traversingLines.verticalStartPoints[i] + traversingLines.down[k];
                if (InRange(point) && grid[point.x, point.y].volume > 0f)
                {
                    if (grid[point.x, point.y].volume <= grid[point.x, point.y].capacity) continue;

                    Vector2Int upCell = traversingLines.verticalStartPoints[i] + traversingLines.down[k - 1];
                    if (!InRange(upCell)) continue;

                    if (grid[upCell.x, upCell.y].volume == 0f) grid[point.x, point.y].FlowToEmptyCell(this, upCell, grid[point.x, point.y].volume - grid[point.x, point.y].capacity);
                    else if (grid[upCell.x, upCell.y].volume > 0f)
                    {
                        float transfer = Mathf.Min(grid[point.x, point.y].volume - grid[point.x, point.y].capacity, maxVolume - grid[upCell.x, upCell.y].volume);
                        grid[point.x, point.y].FlowToFluidCell(this, upCell, transfer, true);
                    }
                }
            }
        });
    }

    private void GetAdjacentCellsBfs(Hashtable visited, List<Vector2Int> waterBody, List<Vector2Int> silhouette, Vector2Int s)
    {
        if (!InRange(s)) return;

        LinkedList<Vector2Int> queue = new LinkedList<Vector2Int>();
        visited[s] = true;
        queue.AddLast(s);

        while (queue.Any())
        {
            Vector2Int p = queue.First();
            queue.RemoveFirst();

            if (grid[p.x, p.y].volume == 0f)
            {
                silhouette.Add(p);
            }
            else if (grid[p.x, p.y].volume > 0f && grid[p.x, p.y].momentum == 0f)
            {
                waterBody.Add(p);

                List<Vector2Int> adjacencyList = new List<Vector2Int>
                {
                    traversingLines.GetNeightborPoint(p, 0, 1),
                    traversingLines.GetNeightborPoint(p, 0, -1)
                };
                foreach (Vector2Int adjacent in adjacencyList)
                {
                    if (InRange(adjacent) && adjacent != p && !visited.ContainsKey(adjacent))
                    {
                        visited[adjacent] = true;
                        queue.AddLast(adjacent);
                    }
                }
            }
        }
    }

    private void BalanceAdjacentCells(List<Vector2Int> waterBody, List<Vector2Int> silhouette)
    {
        // get total volume
        float bodyVolume = 0;
        foreach (Vector2Int p in waterBody) bodyVolume += grid[p.x, p.y].volume;
        float split = bodyVolume / waterBody.Count;

        // flow to empty cells on the silhouette
        if (split > minFlow)
        {
            foreach (Vector2Int p in silhouette)
            {
                grid[p.x, p.y].capacity = 1f;
                grid[p.x, p.y].momentum = 0f;
            }
            waterBody.AddRange(silhouette);
        }
        split = bodyVolume / waterBody.Count;

        // split total volume between all adjacent cells
        foreach (Vector2Int p in waterBody)
        {
            grid[p.x, p.y].volume = split;
            grid2d.UpdateVoxel(p, grid[p.x, p.y].volume);
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
}
