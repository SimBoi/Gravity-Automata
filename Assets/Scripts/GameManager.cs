using UnityEngine;
using UnityEngine.SceneManagement;
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
    public CellularAutomata2D ca;
    public Grid2D grid;
    public RandomRollouts randomAi;
    public MCTS mctsAi;
    public GreedyBestFirstSearch greedyAi;
    public GameObject LOAD;
    public GameObject SIMULATE;
    public Text extractionInfo;
    public Text rolloutInfo;

    [HideInInspector]
    public bool simulate = false, dynamicSimsPerSec = false;
    [HideInInspector]
    public float simsPerSec = 5;
    [HideInInspector]
    public float simulateTimer = 0;

    private void Start()
    {
        SetSize("32");
        SetSimulationsPerSec("0");
        SetTerminalVelocity("4");
    }

    private void Update()
    {
        if (simulate)
        {
            if (dynamicSimsPerSec)
            {
                ca.fps = (int)(1 / Time.deltaTime);
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
        }
    }

    public void SetSize(string size)
    {
        if (size.Length < 1) return;
        ca.size = int.Parse(size);
        grid.transform.position = new Vector3(-ca.size / 2, -ca.size / 2, 0);
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
            ca.fps = simsPerSec;
        }
    }

    public void SetTerminalVelocity(string terminalVelocity)
    {
        if (terminalVelocity.Length < 1) return;
        ca.terminalVelocity = int.Parse(terminalVelocity);
    }

    public void LoadEnvironment(int level)
    {
        ca.GenerateEnv();
        grid.LoadEnv(level);
        float totalVolume = 0;
        for (int x = 0; x < ca.size; x++)
        {
            for (int y = 0; y < ca.size; y++)
            {
                ca.grid[x, y].volume = grid.values[x, y];
                if (ca.grid[x, y].volume > 0)
                {
                    totalVolume += ca.grid[x, y].volume;
                }
            }
        }
        ca.initialTotalVolume = totalVolume;
        ca.totalVolume = totalVolume;

        LOAD.SetActive(false);
        SIMULATE.SetActive(true);
        simulate = true;

        randomAi = new RandomRollouts();
        mctsAi = new MCTS()
        {
            shouldExpandNewChildUCTThreshold = 0.7152f,
            selectionExplorationWeight = 0.2f,
            expansionExplorationWeight = 100.0f,
            expansionDepthWeight = 0.1f,
            evalWeightDepth = 0.0494f,
            evalWeightExtractedWater = 0.2990f,
            evalWeightRollout = 0.1948f,
            evalWeightAverageChildren = 0.2777f,
            evalWeightMaxChild = 0.1791f
        };
        greedyAi = new GreedyBestFirstSearch(this);
    }

    public void ManualRotation(string value)
    {
        if (value.Length < 1) return;
        grid.transform.Rotate(0, 0, float.Parse(value), Space.World);
        Vector3 newGravity3D = grid.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        Vector2 newGravity = new Vector2(newGravity3D.x, newGravity3D.y);
        ca.UpdateGravity(newGravity);
    }

    public void CalculateBestStepRandom()
    {
        randomAi.beginSearch(this);
        for (int i = 0; i < 100; i++) randomAi.SearchStep();
        float bestAction = randomAi.Decide();
        grid.transform.rotation = Quaternion.Euler(0, 0, bestAction);
        Vector3 newGravity3D = grid.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        Vector2 newGravity = new Vector2(newGravity3D.x, newGravity3D.y);
        ca.UpdateGravity(newGravity);
    }

    public void CalculateBestStepMCTS()
    {
        mctsAi.beginSearch(this);
        for (int i = 0; i < 100; i++) mctsAi.SearchStep();
        float bestAction = mctsAi.Decide();
        grid.transform.rotation = Quaternion.Euler(0, 0, bestAction);
        Vector3 newGravity3D = grid.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        Vector2 newGravity = new Vector2(newGravity3D.x, newGravity3D.y);
        ca.UpdateGravity(newGravity);
    }

    public void CalculateBestStepGreedy()
    {
        greedyAi.beginSearch(this);
        greedyAi.SearchStep();
        float bestAction = greedyAi.Decide();
        grid.transform.rotation = Quaternion.Euler(0, 0, bestAction);
        Vector3 newGravity3D = grid.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        Vector2 newGravity = new Vector2(newGravity3D.x, newGravity3D.y);
        ca.UpdateGravity(newGravity);
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
}
