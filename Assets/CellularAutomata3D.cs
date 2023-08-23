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

    public Vector2Int[] verticalStartPoints;
    public Vector2Int[] iAxisStartPoints;
    public Vector2Int[] jAxisStartPoints;
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
