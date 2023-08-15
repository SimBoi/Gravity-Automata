using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class CellularVector3D : MonoBehaviour
{
    public static Vector3Int Round(Vector3 coords)
    {
        int x = Mathf.RoundToInt(coords.x);
        int y = Mathf.RoundToInt(coords.y);
        int z = Mathf.RoundToInt(coords.z);
        return new Vector3Int(x, y, z);
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

    public static List<Vector3Int> Bresenham3D(Vector3Int start, Vector3Int end)
    {
        int x1 = start.x;
        int y1 = start.y;
        int z1 = start.z;
        int x2 = end.x;
        int y2 = end.y;
        int z2 = end.z;
        List<Vector3Int> ListOfPoints = new List<Vector3Int> { start };
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int dz = Mathf.Abs(z2 - z1);
        int xs;
        int ys;
        int zs;
        if (x2 > x1)
            xs = 1;
        else
            xs = -1;
        if (y2 > y1)
            ys = 1;
        else
            ys = -1;
        if (z2 > z1)
            zs = 1;
        else
            zs = -1;

        // Driving axis is X-axis"
        if (dx >= dy && dx >= dz)
        {
            int p1 = 2 * dy - dx;
            int p2 = 2 * dz - dx;
            while (x1 != x2)
            {
                x1 += xs;
                if (p1 >= 0)
                {
                    y1 += ys;
                    p1 -= 2 * dx;
                }
                if (p2 >= 0)
                {
                    z1 += zs;
                    p2 -= 2 * dx;
                }
                p1 += 2 * dy;
                p2 += 2 * dz;
                ListOfPoints.Add(new Vector3Int(x1, y1, z1));
            }

            // Driving axis is Y-axis"
        }
        else if (dy >= dx && dy >= dz)
        {
            int p1 = 2 * dx - dy;
            int p2 = 2 * dz - dy;
            while (y1 != y2)
            {
                y1 += ys;
                if (p1 >= 0)
                {
                    x1 += xs;
                    p1 -= 2 * dy;
                }
                if (p2 >= 0)
                {
                    z1 += zs;
                    p2 -= 2 * dy;
                }
                p1 += 2 * dx;
                p2 += 2 * dz;
                ListOfPoints.Add(new Vector3Int(x1, y1, z1));
            }

            // Driving axis is Z-axis"
        }
        else
        {
            int p1 = 2 * dy - dz;
            int p2 = 2 * dx - dz;
            while (z1 != z2)
            {
                z1 += zs;
                if (p1 >= 0)
                {
                    y1 += ys;
                    p1 -= 2 * dz;
                }
                if (p2 >= 0)
                {
                    x1 += xs;
                    p2 -= 2 * dz;
                }
                p1 += 2 * dy;
                p2 += 2 * dx;
                ListOfPoints.Add(new Vector3Int(x1, y1, z1));
            }
        }
        return ListOfPoints;
    }
}
