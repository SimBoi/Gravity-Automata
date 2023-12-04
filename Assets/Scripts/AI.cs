using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

public struct CellularAutomataSnapshot
{
    public Cell[,,] grid;
    public float totalVolume;
    public Quaternion rotation;

    public CellularAutomataSnapshot(GameManager gameManager)
    {
        grid = (Cell[,,])gameManager.ca.grid.Clone();
        totalVolume = gameManager.ca.totalVolume;
        rotation = gameManager.envBounds.transform.rotation;
    }

    public void RestoreSnapshot(GameManager gameManager, bool rerender)
    {
        gameManager.ca.grid = (Cell[,,])grid.Clone();
        gameManager.ca.totalVolume = totalVolume;
        gameManager.envBounds.transform.rotation = rotation;
        Vector3 gravity = gameManager.envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        gameManager.ca.UpdateGravity(gravity);

        if (rerender)
        {
            for (int x = 0; x < gameManager.ca.size; x++)
                for (int y = 0; y < gameManager.ca.size; y++)
                    for (int z = 0; z < gameManager.ca.size; z++)
                        gameManager.water.UpdateVoxel(new Vector3Int(x, y, z), gameManager.ca.grid[x, y, z].volume <= 0 ? 1 : -gameManager.ca.grid[x, y, z].volume);
            gameManager.water.ReRenderAllChunks();
        }
    }
}

public interface AI
{
    public void beginSearch(GameManager gameManager);
    public void SearchStep();
    public Vector2 PeakDecision();
    public Vector2 Decide();
}

public class RandomRollouts : AI
{
    private GameManager gameManager;
    public CellularAutomataSnapshot rootSnapshot;
    public Vector2 bestAction;
    public int bestPathLength;
    public int rolloutCount;

    public void beginSearch(GameManager gameManager)
    {
        this.gameManager = gameManager;
        rootSnapshot = new CellularAutomataSnapshot(gameManager);
        bestAction = Vector2.zero;
        bestPathLength = 10;
        rolloutCount = 0;
    }

    public void SearchStep()
    {
        rootSnapshot.RestoreSnapshot(gameManager, false);
        
        // rollout
        Vector2 firstAction = Vector2.zero;
        for (int i = 1; i < bestPathLength; i++)
        {
            Vector2 nextAction = RandomAction();
            if (i == 1) firstAction = nextAction;
            gameManager.envBounds.transform.Rotate(nextAction.x, nextAction.y, 0, Space.World);
            Vector3 newGravity = gameManager.envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
            gameManager.ca.UpdateGravity(newGravity);
            // simulate t seconds after each action
            int t = 5;
            for (int j = 0; j < gameManager.simsPerSec * t; j++)
            {
                gameManager.ca.SimulateStep();
            }
            // update best path
            if (gameManager.ca.totalVolume / gameManager.ca.initialTotalVolume < 0.05f)
            {
                bestPathLength = i;
                bestAction = firstAction;
            }
        }
    }

    public Vector2 PeakDecision()
    {
        return bestAction;
    }

    public Vector2 Decide()
    {
        rootSnapshot.RestoreSnapshot(gameManager, true);
        return bestAction;
    }

    public Vector2 RandomAction()
    {
        float u = Random.Range(0f, 1f);
        float v = Random.Range(0f, 1f);
        float theta = Mathf.Acos(2 * u - 1);
        float phi = 2 * Mathf.PI * v;
        theta *= Mathf.Rad2Deg;
        phi *= Mathf.Rad2Deg;
        return new Vector2(theta, phi);
    }
}

public class IntelligentRollouts : AI
{
    public void beginSearch(GameManager gameManager)
    {

    }

    public void SearchStep()
    {

    }

    public Vector2 PeakDecision()
    {
        return Vector2.zero;
    }

    public Vector2 Decide()
    {
        return Vector2.zero;
    }
}

public class MCTSNode
{
    public Cell[,,] gridSnapshot;
    public float eval = 0;
    public int rollouts = 0;
    public List<MCTSNode> children = new List<MCTSNode>();
}

public class MCTS : AI
{
    private MCTSNode searchTree;
    private float c = Mathf.Sqrt(2);
    private int rollouts = 0;

    public void beginSearch(GameManager gameManager)
    {
        searchTree = new MCTSNode();
        searchTree.gridSnapshot = (Cell[,,])gameManager.ca.grid.Clone();
        rollouts = 0;
    }

    public void SearchStep()
    {
        Select();
        Rollout();
        BackPropagate();
    }

    public void Select()
    {

    }

    public void Rollout()
    {

    }

    public void BackPropagate()
    {

    }

    public Vector2 PeakDecision()
    {
        return Vector2.zero;
    }

    public Vector2 Decide()
    {
        return Vector2.zero;
    }

    public float UCB(MCTSNode node)
    {
        return node.eval / node.rollouts + c * Mathf.Sqrt(Mathf.Log(rollouts, Mathf.Exp(1)) / node.rollouts);
    }
}
