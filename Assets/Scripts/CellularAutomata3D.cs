using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

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
        horizontalStartPoints = new Vector3Int[3 * size];
        down = new Vector3Int[size];
        horizontal = new Vector3Int[3 * size / 2, size];
        shuffledIndexes = new int[(3 * size / 2) * size][];
        for (int i = 0; i < (3 * size / 2) * size; i++) shuffledIndexes[i] = new int[2];
    }

    public void ShuffleIndexes()
    {
        int totalSize = (3 * size / 2) * size;
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
            shuffledIndexes[i][0] = shuffle[i] % (3 * size / 2);
            shuffledIndexes[i][1] = shuffle[i] / (3 * size / 2);
        }
    }

    public void GenerateLines(Vector3 downDir)
    {
        downDir = downDir.normalized;

        // avoid multiple start point planes in 45 degees tilt situations
        if (downDir.x == downDir.y) downDir.y += epsilon;
        if (downDir.x == downDir.z) downDir.z += epsilon;
        if (downDir.y == downDir.z) downDir.z += epsilon;

        //////// vertical traversing ////////

        this.downDir = downDir;
        down = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(downDir * 2 * size)).GetRange(0, size).ToArray();

        Vector3 perpendicularNormal, downNormalDir = GeneratePlaneStartPoints(size, downDir, ref verticalStartPoints, out perpendicularNormal);

        //////// horizontal traversing ////////

        // get horizontal plane axis
        Vector3 iDir, jDir;
        iDir = Vector3.Cross(downDir, perpendicularNormal).normalized;
        jDir = Vector3.Cross(downDir, iDir).normalized;
        Vector3 iPlaneCenter, jPlaneCenter;
        Vector3 iNormalDir = GetPlaneNormal(size, iDir, out iPlaneCenter).normalized;
        Vector3 jNormalDir = GetPlaneNormal(size, jDir, out jPlaneCenter).normalized;

        // make sure the start points include the lowest point
        if (Vector3.Dot(downDir, iNormalDir) > 0)
        {
            iDir *= -1;
            iPlaneCenter += iNormalDir * (size - 1);
            iNormalDir *= -1;
        }
        if (Vector3.Dot(downDir, jNormalDir) > 0)
        {
            jDir *= -1;
            jPlaneCenter += jNormalDir * (size - 1);
            jNormalDir *= -1;
        }

        // create start points
        Vector3 horizontalStartPointsDir = Vector3.Cross(iNormalDir, jNormalDir).normalized;
        if (Vector3.Dot(downDir, horizontalStartPointsDir) > 0) horizontalStartPointsDir *= -1;
        Vector3 horizontalStartPointsOrigin = iPlaneCenter - (jNormalDir + horizontalStartPointsDir) * (size / 2 - 0.5f);
        for (int i = 0; i < 3 * size; i++)
        {
            horizontalStartPoints[i] = CellularVector3D.Round(horizontalStartPointsOrigin + i * horizontalStartPointsDir);
        }

        // create the horizontal plane
        Vector3Int[] iVec = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(iDir * size * 3)).GetRange(0, 3 * size / 2).ToArray();
        Vector3Int[] jVec = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(jDir * size * 2)).GetRange(0, size).ToArray();
        for (int i = 0; i < 3 * size / 2; i++)
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
        for (int i = 0; i < 3 * size / 2; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < 3 * size; k++)
                {
                    Vector3Int point = horizontalStartPoints[k] + horizontal[i, j];
                    if (InRange(point)) pointIndexOnHorizontalPath[point] = new int[] {i, j};
                }
            }
        }
    }

    private static Vector3 GeneratePlaneStartPoints(int size, Vector3 dir, ref Vector3Int[,] startPoints, out Vector3 perpendicularNormal) // returns normal to the start points plane
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
        perpendicularNormal = iOvershootDir;
        return normalDir;
    }

    private static Vector3 GetPlaneNormal(int size, Vector3 dir, out Vector3 planeCenter)
    {
        Vector3 normalDir;
        if (Vector3.Angle(Vector3.down, dir) <= 45f)
        {
            normalDir = Vector3.down;
            planeCenter = new Vector3(size / 2 - 0.5f, size - 1, size / 2 - 0.5f);
        }
        else if (Vector3.Angle(Vector3.up, dir) <= 45f)
        {
            normalDir = Vector3.up;
            planeCenter = new Vector3(size / 2 - 0.5f, 0, size / 2 - 0.5f);
        }
        else if (Vector3.Angle(Vector3.left, dir) <= 45f)
        {
            normalDir = Vector3.left;
            planeCenter = new Vector3(size - 1, size / 2 - 0.5f, size / 2 - 0.5f);
        }
        else if (Vector3.Angle(Vector3.right, dir) <= 45f)
        {
            normalDir = Vector3.right;
            planeCenter = new Vector3(0, size / 2 - 0.5f, size / 2 - 0.5f);
        }
        else if (Vector3.Angle(Vector3.forward, dir) <= 45f)
        {
            normalDir = Vector3.forward;
            planeCenter = new Vector3(size / 2 - 0.5f, size / 2 - 0.5f, 0);
        }
        else
        {
            normalDir = Vector3.back;
            planeCenter = new Vector3(size / 2 - 0.5f, size / 2 - 0.5f, size - 1);
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
        if (horizontalTargetIndex[0] < 0 || horizontalTargetIndex[0] >= 3 * size / 2) return point;
        if (horizontalTargetIndex[1] < 0 || horizontalTargetIndex[1] >= size) return point;
        Vector3Int horizontalVec = horizontal[horizontalTargetIndex[0], horizontalTargetIndex[1]] - horizontal[horizontalIndex[0], horizontalIndex[1]];

        return point + verticalVec + horizontalVec;
    }

    public List<Vector3Int> GetVerticalPath(Vector3Int start, int maxLength, out bool outOfBounds)
    {
        outOfBounds = false;
        List<Vector3Int> path = new List<Vector3Int>();
        int startIndex = (int)pointIndexOnDownPath[start];
        if (maxLength > 0) // up
        {
            int length = Mathf.Min(maxLength, startIndex + 1);
            if (length < maxLength) outOfBounds = true;
            for (int i = 0; i < length; i++)
            {
                Vector3Int diff = down[startIndex - i] - down[startIndex];
                Vector3Int point = start + diff;
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
                Vector3Int diff = down[startIndex + i] - down[startIndex];
                Vector3Int point = start + diff;
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

    public bool InRange(Vector3Int coords)
    {
        return InRange(coords.x, coords.y, coords.z);
    }

    public bool InRange(int x, int y, int z)
    {
        return x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size;
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

    public void ApplyForces(CellularAutomata3D ca)
    {
        momentum += ca.gravity.magnitude * (1 / ca.fps);
        momentum = Mathf.Clamp(momentum, 1, ca.terminalVelocity);
    }

    public void SimulateCell(CellularAutomata3D ca, Vector3Int p)
    {
        if (hasBeenSimulated) return;
        hasBeenSimulated = true;

        float prevVolume = volume;

        // apply acceletation due to gravity
        ApplyForces(ca);

        // flow into neighboring cells
        FlowDown(ca, p);
        if (volume > 0) FlowDiagonally(ca, p);

        // update marching cubes
        if (prevVolume != volume) ca.water.UpdateVoxel(p, volume <= 0 ? 1 : -volume);

        if (volume <= 0) volume = 0f;
    }

    public void FlowDown(CellularAutomata3D ca, Vector3Int start)
    {
        // get the fall path from point start with the current momentum
        bool outOfBounds = false;
        List<Vector3Int> fallPath = ca.traversingLines.GetVerticalPath(start, -(int)momentum - 1, out outOfBounds);
        fallPath.RemoveAt(0);

        // check the farthest distance the cell can fall down(momentum direction) to using the bresenham fall line
        int farthestPoint = -1; // the farthest point
        bool isFarthestPointFluid = false;
        for (int i = 0; i < fallPath.Count; i++)
        {
            Vector3Int p = fallPath[i];
            if (ca.InRange(p))
            {
                if (ca.grid[p.x, p.y, p.z].volume == 0f) isFarthestPointFluid = false;
                else if (ca.grid[p.x, p.y, p.z].volume > 0f) isFarthestPointFluid = true;
                else break;
                farthestPoint = i;
            }
            else break;
        }

        // flow out of bounds
        if (outOfBounds && farthestPoint == fallPath.Count - 1)
        {
            FlowOutOfBounds(ca);
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
            Vector3Int p = fallPath[i];
            if (ca.grid[p.x, p.y, p.z].volume == 0f) FlowToEmptyCell(ca, p, volume);
            else FlowToFluidCell(ca, p, volume);
            if (volume <= 0f) return;
        }
    }

    public void FlowDiagonally(CellularAutomata3D ca, Vector3Int start)
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
            if (flowTo[i] == start || !ca.InRange(flowTo[i]) || ca.grid[flowTo[i].x, flowTo[i].y, flowTo[i].z].volume == -1f)
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
                if (ca.grid[p.x, p.y, p.z].volume == 0f) FlowToEmptyCell(ca, p, split);
                else FlowToFluidCell(ca, p, split);
                if (ca.grid[p.x, p.y, p.z].volume >= ca.grid[p.x, p.y, p.z].capacity)
                {
                    flowTo.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public float FlowToEmptyCell(CellularAutomata3D ca, Vector3Int p, float maxFlow)
    {
        float transfer = Mathf.Min(1f, maxFlow);
        volume -= transfer;
        ca.grid[p.x, p.y, p.z].volume = transfer;
        ca.grid[p.x, p.y, p.z].hasBeenSimulated = true;
        ca.grid[p.x, p.y, p.z].momentum = momentum;
        ca.water.UpdateVoxel(p, -transfer);
        return transfer;
    }

    public float FlowToFluidCell(CellularAutomata3D ca, Vector3Int p, float maxFlow, bool overflow = false)
    {
        float transfer = maxFlow;
        if (!overflow) transfer = Mathf.Min(ca.grid[p.x, p.y, p.z].capacity - ca.grid[p.x, p.y, p.z].volume, maxFlow);
        if (transfer <= 0) return 0;
        ca.grid[p.x, p.y, p.z].volume += transfer;
        ca.water.UpdateVoxel(p, -ca.grid[p.x, p.y, p.z].volume);
        volume -= transfer;
        return transfer;
    }

    public void FlowOutOfBounds(CellularAutomata3D ca)
    {
        ca.totalVolume -= volume;
        volume = 0f;
    }
}

public class CellularAutomata3D : MonoBehaviour
{
    public int size;
    public float fps = 30;
    [HideInInspector]
    public Cell[,,] grid;
    public Vector3 gravity; // relative to the local grid
    public TraversingLines traversingLines;
    public MarchingCubesChunk water;
    public float totalVolume;

    public int terminalVelocity = 3;
    public float maxVolume = 3f;
    public float compression = 0.15f;
    public float minFlow = 0.1f;

    public void GenerateEnv()
    {
        grid = new Cell[size, size, size];
        traversingLines = new TraversingLines(size);
        UpdateGravity(gravity);
        totalVolume = 0f;
    }

    public void UpdateGravity(Vector3 newDir)
    {
        gravity = newDir;
        traversingLines.GenerateLines(gravity);
    }

    public void SimulateStep()
    {
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                for (int z = 0; z < size; z++)
                    if (grid[x, y, z].volume > 0f) grid[x, y, z].hasBeenSimulated = false;

        // update compression
        Parallel.For(0, 2 * size, i =>
        {
            for (int j = 0; j < 2 * size; j++)
            {
                float nextVolume = 1f;
                for (int k = 0; k < size; k++)
                {
                    Vector3Int point = traversingLines.verticalStartPoints[i, j] + traversingLines.down[k];
                    if (!InRange(point) || grid[point.x, point.y, point.z].volume <= 0f)
                    {
                        nextVolume = 1f;
                        continue;
                    }
                    grid[point.x, point.y, point.z].capacity = nextVolume;
                    nextVolume += compression;
                }
            }
        });

        // simulate
        traversingLines.ShuffleIndexes();
        int layers = (3 * size) / terminalVelocity;
        if ((3 * size) % terminalVelocity != 0) layers += 1;
        int evenLayers = layers % 2 == 0 ? layers / 2 : layers / 2 + 1;
        int oddLayers = layers / 2;
        Parallel.For(0, evenLayers, t =>
        {
            for (int k = 0; k < terminalVelocity; k++)
            {
                foreach (int[] index in traversingLines.shuffledIndexes)
                {
                    Vector3Int point = traversingLines.horizontalStartPoints[k + t * 2 * terminalVelocity] + traversingLines.horizontal[index[0], index[1]];
                    if (!InRange(point)) continue;
                    if (grid[point.x, point.y, point.z].volume > 0f) grid[point.x, point.y, point.z].SimulateCell(this, point);
                }
            }
        });
        Parallel.For(0, oddLayers, t =>
        {
            for (int k = 0; k < terminalVelocity; k++)
            {
                foreach (int[] index in traversingLines.shuffledIndexes)
                {
                    Vector3Int point = traversingLines.horizontalStartPoints[k + (t * 2 + 1) * terminalVelocity] + traversingLines.horizontal[index[0], index[1]];
                    if (!InRange(point)) continue;
                    if (grid[point.x, point.y, point.z].volume > 0f) grid[point.x, point.y, point.z].SimulateCell(this, point);
                }
            }
        });

        // balance water volume on horizontally adjacent cells and flow sideways if possible
        Parallel.For(0, 3 * size, k =>
        {
            Hashtable visited = new Hashtable();
            for (int i = 0; i < 3 * size / 2; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Vector3Int point = traversingLines.horizontalStartPoints[k] + traversingLines.horizontal[i, j];
                    if (visited.ContainsKey(point)) continue;

                    // get and balance water body
                    if (InRange(point) && grid[point.x, point.y, point.z].volume > 0f && grid[point.x, point.y, point.z].momentum == 0f)
                    {
                        List<Vector3Int> waterBody = new List<Vector3Int>();
                        List<Vector3Int> silhouette = new List<Vector3Int>();
                        GetAdjacentCellsRecursive(visited, waterBody, silhouette, point);
                        if (waterBody.Count > 0) BalanceAdjacentCells(waterBody, silhouette);
                    }
                }
            }
        });

        // flow up excess volume
        Parallel.For(0, 2 * size, i =>
        {
            for (int j = 0; j < 2 * size; j++)
            {
                for (int k = 1; k < size; k++)
                {
                    Vector3Int point = traversingLines.verticalStartPoints[i, j] + traversingLines.down[k];
                    if (InRange(point) && grid[point.x, point.y, point.z].volume > 0f)
                    {
                        if (grid[point.x, point.y, point.z].volume <= grid[point.x, point.y, point.z].capacity) continue;

                        Vector3Int upCell = traversingLines.verticalStartPoints[i, j] + traversingLines.down[k - 1];
                        if (!InRange(upCell)) continue;

                        if (grid[upCell.x, upCell.y, upCell.z].volume == 0f) grid[point.x, point.y, point.z].FlowToEmptyCell(this, upCell, grid[point.x, point.y, point.z].volume - grid[point.x, point.y, point.z].capacity);
                        else if (grid[upCell.x, upCell.y, upCell.z].volume > 0f)
                        {
                            float transfer = Mathf.Min(grid[point.x, point.y, point.z].volume - grid[point.x, point.y, point.z].capacity, maxVolume - grid[upCell.x, upCell.y, upCell.z].volume);
                            grid[point.x, point.y, point.z].FlowToFluidCell(this, upCell, transfer, true);
                        }
                    }
                }
            }
        });
    }

    private void GetAdjacentCellsRecursive(Hashtable visited, List<Vector3Int> waterBody, List<Vector3Int> silhouette, Vector3Int p)
    {
        if (!InRange(p) || visited.ContainsKey(p)) return;
        visited[p] = true;

        if (grid[p.x, p.y, p.z].volume == 0f)
        {
            silhouette.Add(p);
        }
        else if (grid[p.x, p.y, p.z].volume > 0f && grid[p.x, p.y, p.z].momentum == 0f)
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
        float bodyVolume = 0;
        foreach (Vector3Int p in waterBody) bodyVolume += grid[p.x, p.y, p.z].volume;
        float split = bodyVolume / waterBody.Count;

        // flow to empty cells on the silhouette
        if (split > minFlow)
        {
            foreach (Vector3Int p in silhouette)
            {
                grid[p.x, p.y, p.z].capacity = 1f;
                grid[p.x, p.y, p.z].momentum = 0f;
            }
            waterBody.AddRange(silhouette);
        }
        split = bodyVolume / waterBody.Count;

        // split total volume between all adjacent cells
        foreach (Vector3Int p in waterBody)
        {
            grid[p.x, p.y, p.z].volume = split;
            water.UpdateVoxel(p, -grid[p.x, p.y, p.z].volume);
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
}