using Dummiesman;
using UnityEngine;
using UnityEngine.SceneManagement;
using SFB;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class HyperparameterSet
{
    public float shouldExpandNewChildUCTThreshold;
    public float selectionExplorationWeight;
    public float expansionExplorationWeight;
    public float expansionDepthWeight;
    public float evalWeightDepth;
    public float evalWeightExtractedWater;
    public float evalWeightRollout;
    public float evalWeightAverageChildren;
    public float evalWeightMaxChild;
}

[System.Serializable]
public class HyperparameterSetArray
{
    public HyperparameterSet[] sets;
}


public class GameManager : MonoBehaviour
{
    public CellularAutomata3D ca;
    public GameObject loadedObject = null;
    public MarchingCubesChunk objModel;
    public MarchingCubesChunk water;
    public RandomRollouts ai;
    public GameObject envBounds;
    public GameObject LOAD;
    public GameObject GENERATE;
    public GameObject SIMULATE;
    public Text extractionInfo;
    public Text rolloutInfo;
    public TextAsset hyperparametersJson;

    [HideInInspector]
    public bool simulate = false, dynamicSimsPerSec = false;
    [HideInInspector]
    public float simsPerSec = 5, simsPerRender = 1, rendersPerSec = 5;
    [HideInInspector]
    public float renderTimer = 0, simulateTimer = 0;

    public void Start()
    {
        LoadObj();

        SetSize("32");
        SetSimulationsPerSec("2");
        SetSimulationsPerRender("1");
        SetTerminalVelocity("5");
        GenerateEnvironment();

        CalculateBestStep();
    }

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

            if (dynamicSimsPerSec)
            {
                ca.fps = (int)(1 / Time.deltaTime);
                rendersPerSec = ca.fps / simsPerRender;
                ca.SimulateStep();
                extractionInfo.text = "<b>Extracted: " + 100 * (1 - ca.totalVolume / ca.initialTotalVolume) + "%</b>";
            }
            else
            {
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
        // var extensions = new[] { new ExtensionFilter("Obj Files", "obj") };
        // var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
        // loadedObject = new OBJLoader().Load(paths[0]);
        // loadedObject.transform.localScale = Vector3.one;
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
        if (simsPerSec == 0)
        {
            dynamicSimsPerSec = true;
        }
        else
        {
            dynamicSimsPerSec = false;
            rendersPerSec = simsPerSec / simsPerRender;
            ca.fps = simsPerSec;
        }
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
        int[] sets = { 1 };
        foreach (int iteration in sets)
        {
            /*float[] range1 = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f, 1.4f, 1.8f, 2.2f, 3f, 4f, 5f, 6f, 8f, 10f, 14f, 18f, 25f, 50f, 75f, 100f, 200f, 500f, 1000f, 5000f, 10000f };
            float[] range2 = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.7f, 0.9f, 1.2f, 1.5f, 1.9f, 2.5f };

            float shouldExpandNewChildUCTThreshold = Random.Range(0f, 1f); // weight in range [0, 1]
            float selectionExplorationWeight = range1[Random.Range(0, range1.Length)]; // weight in range1
            float expansionExplorationWeight = range1[Random.Range(0, range1.Length)]; // weight in range1
            float expansionDepthWeight = range2[Random.Range(0, range2.Length)]; // weight in range2

            // evaluation, weights should sum to 1
            float evalWeightDepth = Random.Range(0f, 1f);
            float evalWeightExtractedWater = Random.Range(0f, 1f);
            float evalWeightRollout = Random.Range(0f, 1f);
            float evalWeightAverageChildren = Random.Range(0f, 1f);
            float evalWeightMaxChild = Random.Range(0f, 1f);
            // normalize weights to sum to 1
            float sum = evalWeightDepth + evalWeightExtractedWater + evalWeightRollout + evalWeightAverageChildren + evalWeightMaxChild;
            evalWeightDepth /= sum;
            evalWeightExtractedWater /= sum;
            evalWeightRollout /= sum;
            evalWeightAverageChildren /= sum;
            evalWeightMaxChild /= sum;*/

            /*HyperparameterSetArray hyperparameterSets = JsonUtility.FromJson<HyperparameterSetArray>(hyperparametersJson.text);
            HyperparameterSet hyperparameters = hyperparameterSets.sets[iteration];
            float shouldExpandNewChildUCTThreshold = hyperparameters.shouldExpandNewChildUCTThreshold;
            float selectionExplorationWeight = hyperparameters.selectionExplorationWeight;
            float expansionExplorationWeight = hyperparameters.expansionExplorationWeight;
            float expansionDepthWeight = hyperparameters.expansionDepthWeight;
            float evalWeightDepth = hyperparameters.evalWeightDepth;
            float evalWeightExtractedWater = hyperparameters.evalWeightExtractedWater;
            float evalWeightRollout = hyperparameters.evalWeightRollout;
            float evalWeightAverageChildren = hyperparameters.evalWeightAverageChildren;
            float evalWeightMaxChild = hyperparameters.evalWeightMaxChild;

            ai = new MCTS()
            {
                shouldExpandNewChildUCTThreshold = shouldExpandNewChildUCTThreshold,
                selectionExplorationWeight = selectionExplorationWeight,
                expansionExplorationWeight = expansionExplorationWeight,
                expansionDepthWeight = expansionDepthWeight,
                evalWeightDepth = evalWeightDepth,
                evalWeightExtractedWater = evalWeightExtractedWater,
                evalWeightRollout = evalWeightRollout,
                evalWeightAverageChildren = evalWeightAverageChildren,
                evalWeightMaxChild = evalWeightMaxChild
            };*/
            ai = new RandomRollouts();
            ai.beginSearch(this);

            string path = Application.dataPath + $"/Results/random-1.txt";
            /*string path = Application.dataPath + $"/Results/{iteration}.txt";
            System.IO.File.WriteAllText(path,
                "shouldExpandNewChildUCTThreshold=" + shouldExpandNewChildUCTThreshold +
                "," + "selectionExplorationWeight=" + selectionExplorationWeight +
                "," + "expansionExplorationWeight=" + expansionExplorationWeight +
                "," + "expansionDepthWeight=" + expansionDepthWeight +
                "," + "evalWeightDepth=" + evalWeightDepth +
                "," + "evalWeightExtractedWater=" + evalWeightExtractedWater +
                "," + "evalWeightRollout=" + evalWeightRollout +
                "," + "evalWeightAverageChildren=" + evalWeightAverageChildren +
                "," + "evalWeightMaxChild=" + evalWeightMaxChild +
                "\n"
            );*/

            for (int i = 0; i < 1000; i++)
            {
                ai.SearchStep();

                // write the current search tree to a new line in the file
                System.IO.File.AppendAllText(path, ai.bestPathLength.ToString() + " - " + ai.bestPathExtractedWater.ToString() + "\n");
                /*string treeString = treeToString(ai.rootNode) + "\n";
                System.IO.File.AppendAllText(path, treeString);

                // check the search tree performance after 50 iterations
                if (i == 50)
                {
                    // check if the search tree is exploiting created nodes
                    bool exploited = false;
                    foreach (MCTSNode child in ai.rootNode.children)
                    {
                        if (child.visits > 1)
                        {
                            exploited = true;
                            break;
                        }
                    }
                    // check if the search tree is exploring new nodes at depth 1
                    bool explored = ai.rootNode.children.Count >= 5;

                    if (!exploited || !explored)
                    {
                        // delete the text file and continue to the next hyperparameter set
                        System.IO.File.Delete(path);
                        break;
                    }
                }*/
            }
            //ai.rootNode.caSnapshot.RestoreSnapshot(this, true);
            ai.rootSnapshot.RestoreSnapshot(this, true);

            // Vector2 bestAction = ai.Decide();
            // envBounds.transform.Rotate(0, bestAction.y, 0, Space.World);
            // envBounds.transform.Rotate(bestAction.x, 0, 0, Space.World);
            // Vector3 newGravity = envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
            // ca.UpdateGravity(newGravity);
        }
    }

    private string treeToString(MCTSNode node)
    {
        Vector3 rotation = node.caSnapshot.rotation.eulerAngles;
        string result =
            "volume=" + node.caSnapshot.totalVolume +
            ",rotation=(" + rotation.x + "," + rotation.y + "," + rotation.z + ")" +
            ",depth=" + node.depth +
            ",visits=" + node.visits +
            ",eval=" + node.eval +
            ",bestRolloutDepth=" + node.bestRollout.depth +
            ",bestRolloutExtractedPercentage=" + node.bestRollout.extractedWaterPercentage +
            ",children=[";
        foreach (MCTSNode child in node.children)
        {
            result += treeToString(child) + ",";
        }
        result += "]";
        return result;
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
