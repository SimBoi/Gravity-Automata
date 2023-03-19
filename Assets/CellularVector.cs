using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CellularVector
{
    public static List<Vector2Int> Bresenham(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();

        ///////////////// to be impliminted

        // temporary implimintation
        if (Mathf.Abs(start.x - end.x) > Mathf.Abs(start.y - end.y))
        {
            bool reverse = false;
            if (start.x > end.x)
            {
                reverse = true;
                Vector2Int tmp = start;
                start = end;
                end = tmp;
            }

            float a = (end.y - start.y) / (end.x - start.x);
            float b = start.y - a * start.x;

            for (int x = start.x; x <= end.x; x++)
            {
                int y = Mathf.RoundToInt(a * x + b);
                line.Add(new Vector2Int(x, y));
            }

            if (reverse) line.Reverse();
        }
        else
        {
            bool reverse = false;
            if (start.y > end.y)
            {
                reverse = true;
                Vector2Int tmp = start;
                start = end;
                end = tmp;
            }

            float a = (end.x - start.x) / (end.y - start.y);
            float b = start.x - a * start.y;

            for (int y = start.y; y <= end.y; y++)
            {
                int x = Mathf.RoundToInt(a * y + b);
                line.Add(new Vector2Int(x, y));
            }

            if (reverse) line.Reverse();
        }

        return line;
    }

    public static Vector2Int Round(Vector2 coords)
    {
        int x = Mathf.RoundToInt(coords.x);
        int y = Mathf.RoundToInt(coords.y);
        return new Vector2Int(x, y);
    }
}
