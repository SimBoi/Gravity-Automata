using System.Collections.Generic;
using UnityEngine;

public class CellularVector2D : MonoBehaviour
{
    public static Vector2Int Round(Vector2 coords)
    {
        int x = Mathf.RoundToInt(coords.x);
        int y = Mathf.RoundToInt(coords.y);
        return new Vector2Int(x, y);
    }

    private static List<Vector2Int> BresenhamLowLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        int yi = dy < 0 ? -1 : 1;
        dy = dy * yi;

        int d = 2 * dy - dx;
        int east = 2 * dy;
        int northeast = 2 * (dy - dx);

        int x = start.x, y = start.y;
        while (x <= end.x)
        {
            line.Add(new Vector2Int(x, y));
            if (d > 0)
            {
                d += northeast;
                y += yi;
            }
            else
            {
                d += east;
            }
            x++;
        }

        return line;
    }

    private static List<Vector2Int> BresenhamHighLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        int xi = dx < 0 ? -1 : 1;
        dx = dx * xi;

        int d = 2 * dx - dy;
        int east = 2 * dx;
        int southeast = 2 * (dx - dy);

        int x = start.x, y = start.y;
        while (y <= end.y)
        {
            line.Add(new Vector2Int(x, y));
            if (d > 0)
            {
                d += southeast;
                x += xi;
            }
            else
            {
                d += east;
            }
            y++;
        }

        return line;
    }

    public static List<Vector2Int> Bresenham(Vector2Int start, Vector2Int end)
    {
        if (Mathf.Abs(end.x - start.x) > Mathf.Abs(end.y - start.y))
        {
            if (start.x < end.x)
            {
                return BresenhamLowLine(start, end);
            }
            else
            {
                List<Vector2Int> line = BresenhamLowLine(end, start);
                line.Reverse();
                return line;
            }
        }
        else
        {
            if (start.y < end.y)
            {
                return BresenhamHighLine(start, end);
            }
            else
            {
                List<Vector2Int> line = BresenhamHighLine(end, start);
                line.Reverse();
                return line;
            }
        }
    }

}
