using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public void Start()
    {
        voxels = new GameObject[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject voxel = Instantiate(voxelPrefab, new Vector3(x, y, 0), Quaternion.identity);
                voxels[x, y] = voxel;
                voxel.GetComponent<Renderer>().material.color = emptyColor;
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
    }

    public void UpdateVoxel(Vector2Int index, float value)
    {
        if (values[index.x, index.y] == value) return;
        values[index.x, index.y] = value;
        RenderVoxel(index);
    }
}
