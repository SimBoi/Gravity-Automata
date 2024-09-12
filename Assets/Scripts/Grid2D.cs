using System.Collections.Generic;
using UnityEngine;

public class Grid2D : MonoBehaviour
{
    public int size;
    public Color emptyColor;
    public Color barrierColor;
    public Color waterColor;
    public GameObject voxelPrefab;
    public GameObject[,] voxels;
    public float[,] values;
    public List<Vector2Int> needsUpdate = new List<Vector2Int>();

    public string[] envs = new string[]
    {
        "o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o o o o o o w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o o o o o o w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w o o o o o o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w o o o o o o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w o o w w w w w w w w w w w w w w o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o",
        "o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w o o o o o o o o o o o o o o o o o o o o o o w w w o o\r\no o w w w o o o o o o o o o o o o o o o o o o o o o o w w w o o\r\no o w w w o o w w w w w w w w w w w w w w w w w w o o w w w o o\r\no o w w w o o w w w w w w w w w w w w w w w w w w o o w w w o o\r\no o w w w o o w w w w w w w w w w w w w w w w w w o o w w w o o\r\no o w w w o o w w w w o o o o o o o o o o o w w w o o w w w o o\r\no o w w w o o w w w w o o o o o o o o o o o w w w o o w w w o o\r\no o w w w o o w w w w o o w w w w w w w o o w w w o o w w w o o\r\no o w w w o o w w w w o o w w w w w w w o o w w w o o w w w o o\r\no o w w w o o w w w w o o w w w w w w w o o w w w o o w w w o o\r\no o w w w o o w w w w o o w w w o o o o o o w w w o o w w w o o\r\no o w w w o o w w w w o o w w w o o o o o o w w w o o w w w o o\r\no o w w w o o w w w w o o w w w w w w w w w w w w o o w w w o o\r\no o w w w o o w w w w o o w w w w w w w w w w w w o o w w w o o\r\no o w w w o o w w w w o o w w w w w w w w w w w w o o w w w o o\r\no o w w w o o w w w w o o o o o o o o o o o o o o o o w w w o o\r\no o w w w o o w w w w o o o o o o o o o o o o o o o o w w w o o\r\no o w w w o o w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w o o w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w o o w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o w w w o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w . .\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o",
        "o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w o o w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w o o w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w w w w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w o o w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w o o w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w o o w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w o o w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w o o w w w w w w w o o\r\no o w w o o w w w w w w o o w w w w w w w o o w w w w w w w o o\r\no o w w o o o o o o o o o o w w w w w w w o o w w w w w w w o o\r\no o w w o o o o o o o o o o w w w w w w w o o w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w o o w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w o o w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w o o w w w w w w w o o\r\no o w w w w w w w o o w w w w w w w w w w o o w w w w w w w o o\r\no o w w w w w w w o o w w w w w w w w w w o o o o o o o o o o o\r\no o w w w w w w w o o w w w w w w w w w w o o o o o o o o o o o\r\no o w w w w w w w o o w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w o o w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w o o w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w o o w w w w w w w w w w w w w w w w w w w o o\r\no o o o o o o o o o o o o . . . . . . o o o o o o o o o o o o o\r\no o o o o o o o o o o o o . . . . . . o o o o o o o o o o o o o",
        "o o o o o o o o o o o o o . . . . . . o o o o o o o o o o o o o\r\no o o o o o o o o o o o o . . . . . . o o o o o o o o o o o o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w o o o o o o o o o o o o o o o o w w w w w w o o\r\no o w w w w w w o o o o o o o o o o o o o o o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w o o w w w w w w w w w w w w o o w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o w w w w w w w w w w w w w w w w w w w w w w w w w w w w o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o\r\no o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o o"
    };

    private void Update()
    {
        if (needsUpdate.Count > 0)
        {
            foreach (Vector2Int index in needsUpdate)
            {
                RenderVoxel(index);
            }
            needsUpdate.Clear();
        }
    }

    public void InitGrid()
    {
        voxels = new GameObject[size, size];
        values = new float[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = size - 1; y >= 0; y--)
            {
                GameObject voxel = Instantiate(voxelPrefab, Vector3.zero, Quaternion.Euler(Vector3.right * -90), transform);
                voxel.transform.localPosition = new Vector3(x - size / 2, y - size / 2, 0);
                voxels[x, y] = voxel;
                values[x, y] = 0;
                voxel.GetComponent<Renderer>().material.color = emptyColor;
            }
        }
    }

    public void LoadEnv(int level)
    {
        string envString = envs[level];
        envString = envString.Replace(" ", "").Replace("\r\n", "\n");
        string[] lines = envString.Split('\n');
        size = lines.Length;
        InitGrid();
        for (int y = 0; y < size; y++)
        {
            string line = lines[size - y - 1];
            for (int x = 0; x < size; x++)
            {
                char c = line[x];
                if (c == 'o')
                {
                    values[x, y] = -1;
                }
                else if (c == 'w')
                {
                    values[x, y] = 1;
                }
                else
                {
                    values[x, y] = 0;
                }
                RenderVoxel(new Vector2Int(x, y));
            }
        }
    }

    public void RenderVoxel(Vector2Int index)
    {
        GameObject voxel = voxels[index.x, index.y];
        if (values[index.x, index.y] == -1)
        {
            voxel.GetComponent<Renderer>().material.color = barrierColor;
        }
        else if (values[index.x, index.y] <= 0)
        {
            voxel.GetComponent<Renderer>().material.color = emptyColor;
        }
        else
        {
            voxel.GetComponent<Renderer>().material.color = waterColor * values[index.x, index.y];
        }
    }

    public void ReRenderAllVoxels()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                RenderVoxel(new Vector2Int(x, y));
            }
        }
        needsUpdate.Clear();
    }

    public void UpdateVoxel(Vector2Int index, float value)
    {
        if (values[index.x, index.y] == value) return;
        values[index.x, index.y] = value;
        needsUpdate.Add(index);
    }
}
