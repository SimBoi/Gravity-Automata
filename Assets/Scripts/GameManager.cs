﻿using Dummiesman;
using UnityEngine;
using UnityEngine.SceneManagement;
using SFB;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public CellularAutomata3D ca;
    public GameObject loadedObject = null;
    public MarchingCubesChunk objModel;
    public MarchingCubesChunk water;
    public MCTS ai = new MCTS();
    public GameObject envBounds;
    public GameObject LOAD;
    public GameObject GENERATE;
    public GameObject SIMULATE;
    public Text extractionInfo;
    public Text rolloutInfo;

    [HideInInspector]
    public bool simulate = false;
    [HideInInspector]
    public float simsPerSec = 5, simsPerRender = 1, rendersPerSec = 5;
    [HideInInspector]
    public float renderTimer = 0, simulateTimer = 0;

    private void Update()
    {
        if (simulate)
        {
            if (Input.GetMouseButtonUp(0))
            {
                Vector3 newGravity = envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
                if (ca.gravity != newGravity)
                {
                    ca.UpdateGravity(newGravity);
                }
            }

            if (simulateTimer >= 1 / simsPerSec)
            {
                ca.SimulateStep();
                simulateTimer -= 1 / simsPerSec;
                extractionInfo.text = "<b>Extracted: " + 100 * (1 - ca.totalVolume / ca.initialTotalVolume) + "%</b>";
            }
            else
            {
                simulateTimer += Time.deltaTime;
            }

            if (renderTimer >= 1 / rendersPerSec)
            {
                water.RenderChunks();
                renderTimer -= 1 / rendersPerSec;
            }
            else
            {
                renderTimer += Time.deltaTime;
            }
        }
    }

    public void LoadObj()
    {
        var extensions = new[] { new ExtensionFilter("Obj Files", "obj") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
        loadedObject = new OBJLoader().Load(paths[0]);
        loadedObject.transform.localScale = Vector3.one;
        loadedObject.layer = 6;

        foreach (Transform child in loadedObject.GetComponentsInChildren<Transform>())
        {
            if (child.GetComponent<MeshFilter>())
            {
                //child.gameObject.AddComponent<MeshCollider>();
                child.gameObject.layer = 7;
            }
        }

        // Bounds bounds = new Bounds(loadedObject.transform.position, Vector3.zero);
        // foreach (Renderer r in loadedObject.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        // float scale = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        // loadedObject.transform.localScale *= 0.9f * ca.size / scale;
        // loadedObject.AddComponent<BoxCollider>();
        // loadedObject.GetComponent<BoxCollider>().size = bounds.size;
        // loadedObject.GetComponent<BoxCollider>().center = bounds.center;

        LOAD.SetActive(false);
        GENERATE.SetActive(true);
    }

    public void SetSize(string size)
    {
        if (size.Length < 1) return;
        ca.size = int.Parse(size);
        objModel.transform.position = new Vector3(-ca.size / 2, -ca.size / 2, -ca.size / 2);
        water.transform.position = new Vector3(-ca.size / 2, -ca.size / 2, -ca.size / 2);
        envBounds.transform.localScale = new Vector3(ca.size, ca.size, ca.size);
    }

    public void SetSimulationsPerSec(string sps)
    {
        if (sps.Length < 1) return;
        simsPerSec = int.Parse(sps);
        rendersPerSec = simsPerSec / simsPerRender;
        ca.fps = simsPerSec;
    }

    public void SetSimulationsPerRender(string spr)
    {
        if (spr.Length < 1) return;
        simsPerRender = int.Parse(spr);
        rendersPerSec = simsPerSec / simsPerRender;
    }

    public void SetTerminalVelocity(string terminalVelocity)
    {
        if (terminalVelocity.Length < 1) return;
        ca.terminalVelocity = int.Parse(terminalVelocity);
    }

    public void GenerateEnvironment()
    {
        // initialize
        objModel.size = ca.size;
        water.size = ca.size;
        objModel.GenerateChunks();
        water.GenerateChunks();
        ca.GenerateEnv();

        // fill environment
        ApproximateAndFillObjModel();
        float extraction = ca.size;
        while (extraction >= ca.size)
        {
            float prevTotalVolume = ca.totalVolume;
            ca.SimulateStep();
            extraction = prevTotalVolume - ca.totalVolume;
        }
        ca.initialTotalVolume = ca.totalVolume;

        // render
        objModel.RenderChunks();
        water.RenderChunks();

        envBounds.layer = 6;
        objModel.transform.parent = envBounds.transform;
        water.transform.parent = envBounds.transform;

        // cleanup
        Destroy(envBounds.GetComponent<MeshRenderer>());
        Destroy(loadedObject);
        Destroy(objModel);
        loadedObject = null;
        objModel = null;

        GENERATE.SetActive(false);
        SIMULATE.SetActive(true);
        simulate = true;
    }

    public void CalculateBestStep()
    {
        ai.beginSearch(this);
        for (int i = 0; i < 100; i++)
        {
            ai.SearchStep();
        }

        Vector2 bestAction = ai.Decide();
        envBounds.transform.Rotate(0, bestAction.y, 0, Space.World);
        envBounds.transform.Rotate(bestAction.x, 0, 0, Space.World);
        Vector3 newGravity = envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        ca.UpdateGravity(newGravity);
    }

    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // approximate the .obj model using marching cubes and fill all the empty space with water
    private void ApproximateAndFillObjModel()
    {
        float offset = ca.size / 2;
        for (int x = 0; x < ca.size; x++)
        {
            for (int y = 0; y < ca.size; y++)
            {
                Vector3 origin = new Vector3(x - offset, y - offset, -offset);
                Vector3 direction = Vector3.forward;
                float maxDistance = ca.size;
                RaycastHit[] forwardHits = RaycastEverything(origin, direction, maxDistance, 1 << 7);

                origin.z = offset;
                direction = Vector3.back;
                RaycastHit[] backwardHits = RaycastEverything(origin, direction, maxDistance, 1 << 7);

                List<float> hits = new List<float>();
                foreach (RaycastHit hit in forwardHits) hits.Add(hit.point.z);
                foreach (RaycastHit hit in backwardHits) hits.Add(hit.point.z);
                hits.Sort();

                int i = 0;
                for (int z = 0; z < ca.size; z++)
                {
                    while (i < hits.Count && z - offset > hits[i]) i++;
                    bool inside = i % 2 == 1;
                    objModel.UpdateVoxel(new Vector3Int(x, y, z), inside ? -1 : 1);
                    water.UpdateVoxel(new Vector3Int(x, y, z), inside ? 1 : -1);
                    ca.grid[x, y, z].volume = inside ? -1f : 1f;
                    if (!inside) ca.totalVolume += 1f;
                }
            }
        }
    }

    private static RaycastHit[] RaycastEverything(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        direction = direction.normalized;
        float originalMaxDistance = maxDistance;

        List<RaycastHit> hits = new List<RaycastHit>();

        while (true)
        {
            RaycastHit hit;

            if (Physics.Raycast(origin, direction, out hit, maxDistance, layerMask, queryTriggerInteraction))
            {
                origin = hit.point + direction / 1000.0f;
                maxDistance -= hit.distance;

                hit.distance = originalMaxDistance - maxDistance;
                hits.Add(hit);

                maxDistance -= 0.001f;
            }
            else
            {
                return hits.ToArray();
            }
        }
    }
}
