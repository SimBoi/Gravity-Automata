using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TraversingTest : MonoBehaviour
{
    public bool reset;
    public Vector3 gravity;
    private Vector3 prevGravity = Vector3.zero;
    public int size = 100;
    public int scale;
    public int fps = 100;

    public GameObject cellPrefab;
    private MeshRenderer[,,] cellsUI;

    private TraversingLines tl;
    private Vector3Int[] traversingPoints;
    private int i = 0;

    //private List<MeshRenderer> startingPoints = new List<MeshRenderer>();

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        cellsUI = new MeshRenderer[size, size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    GameObject cell = Instantiate(cellPrefab, transform);
                    cell.transform.localPosition = new Vector3(x - size / 2, y - size / 2, z - size / 2);
                    cellsUI[x, y, z] = cell.GetComponent<MeshRenderer>();
                }
            }
        }
        tl = new TraversingLines(size);
    }

    void Update()
    {
        if (gravity == Vector3.zero) return;

        if (reset || prevGravity != gravity)
        {
            reset = false;
            Reset();
            tl.GenerateLines(gravity);
        }

        if (i >= traversingPoints.Length)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        if (cellsUI[x, y, z].enabled == false) Debug.Log("not all cubes are enabled!");
                    }
                }
            }
        }
        else
        {
            Vector3Int point = traversingPoints[i];
            cellsUI[point.x, point.y, point.z].enabled = true;
            i++;
        }

        prevGravity = gravity;
    }

    private void Reset()
    {
        i = 0;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                    cellsUI[x, y, z].enabled = false;

        List<Vector3Int> points = new List<Vector3Int>();
        tl.ShuffleIndexes();
        for (int k = 0; k < 3 * size; k++)
        {
            foreach (int[] index in tl.shuffledIndexes)
            {
                Vector3Int point = tl.horizontalStartPoints[k] + tl.horizontal[index[0], index[1]];
                if (!InRange(point)) continue;
                points.Add(point);
            }
        }
        traversingPoints = points.ToArray();

        /*foreach (MeshRenderer r in startingPoints) Destroy(r.gameObject);
        startingPoints.Clear();
        foreach (Vector3Int startp in tl.horizontalStartPoints)
        {
            foreach (Vector3Int p in tl.horizontal)
            {
                GameObject cell = Instantiate(cellPrefab, transform);
                cell.transform.localPosition = startp + p - new Vector3(size / 2, size / 2, size / 2);
                startingPoints.Add(cell.GetComponent<MeshRenderer>());
            }
        }*/
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