using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

public class traversingtest : MonoBehaviour
{
    public bool reset;
    public Vector2 gravity;
    private Vector2 prevGravity = Vector2.zero;
    public int size = 100;
    public int scale;

    public GameObject cellPrefab;
    private Image[,] cellsUI;

    private TraversingLines tl;
    private Vector2Int[] traversingPoints;
    private int i = 0;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        cellsUI = new Image[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = new Vector3(x - size / 2, y - size / 2, 0);
                cellsUI[x, y] = cell.GetComponent<Image>();
            }
        }
        tl = new TraversingLines(size);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (gravity == Vector2.zero) return;

        if (reset || prevGravity != gravity || i >= traversingPoints.Length)
        {
            reset = false;
            Reset();
            tl.GenerateLines(gravity);
        }

        Vector2Int point = traversingPoints[i];
        cellsUI[point.x, point.y].color = UnityEngine.Color.white;
        i++;

        prevGravity = gravity;
    }

    private void Reset()
    {
        i = 0;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                cellsUI[x, y].color = UnityEngine.Color.black;

        List<Vector2Int> points = new List<Vector2Int>();
        tl.ShuffleIndexes();
        for (int i = 0; i < 2 * size; i++)
        {
            foreach (int j in tl.shuffledIndexes)
            {
                Vector2Int point = tl.horizontalStartPoints[i] + tl.horizontal[j];
                if (!InRange(point)) continue;
                points.Add(point);
            }
        }
        traversingPoints = points.ToArray();
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
