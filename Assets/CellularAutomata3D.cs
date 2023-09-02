using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CellType
{
    Empty,
    Stone,
    Water
}

public class TraversingLines
{
    private int size;

    public Vector3Int[,] verticalStartPoints;
    public Vector3Int[] horizontalStartPoints;
    public Vector3 downDir;
    public Vector3Int[] down;
    public Vector3Int[] right;
    public Hashtable pointIndexOnDownPath = new Hashtable();
    public Hashtable pointIndexOnRightPath = new Hashtable();

    public int[] shuffledIndexes;

    public TraversingLines(int size)
    {
        this.size = size;
        verticalStartPoints = new Vector3Int[2 * size, 2 * size];
        horizontalStartPoints = new Vector3Int[2 * size];
        down = new Vector3Int[size];
        right = new Vector3Int[size];
        shuffledIndexes = new int[size * size];
    }

    public void ShuffleIndexes()
    {
        /*for (int i = 0; i < size; i++) shuffledIndexes[i] = i;
        for (int i = size; i > 1; i--)
        {
            int p = Random.Range(0, i);
            int tmp = shuffledIndexes[i - 1];
            shuffledIndexes[i - 1] = shuffledIndexes[p];
            shuffledIndexes[p] = tmp;
        }*/
    }

    public void GenerateLines(Vector3 downDir)
    {
        // vertical traversing
        downDir = downDir.normalized;
        this.downDir = downDir;
        down = CellularVector3D.Bresenham3D(Vector3Int.zero, CellularVector3D.Round(downDir * size * 1.5f)).GetRange(0, size).ToArray();

        Vector2 downNormalDir = GeneratePlaneStartPoints(size, downDir, ref verticalStartPoints);

        // horizontal traversing
        Vector3 iDir = rightDir;
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

    private static Vector2 GeneratePlaneStartPoints(int size, Vector2 dir, ref Vector3Int[,] startPoints) // returns normal to the start points plane
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
                startPoints[i, j] = CellularVector3D.Round(planeCenter + (i - size / 2) * overshootDir);
            }
        }
        return normalDir;
    }

    // returns the same point if the target point is out of bounds
    public Vector2Int GetNeightborPoint(Vector2Int point, int verticalDiff, int horizontalDiff)
    {
        int downIndex = (int)pointIndexOnDownPath[point];
        int rightIndex = (int)pointIndexOnRightPath[point];
        int downTargetIndex = downIndex - verticalDiff;
        int rightTargetIndex = rightIndex + horizontalDiff;
        if (downTargetIndex < 0 || downTargetIndex >= size) return point;
        if (rightTargetIndex < 0 || rightTargetIndex >= size) return point;
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

public class CellularAutomata3D : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
