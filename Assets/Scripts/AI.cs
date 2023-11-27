using System.Collections.Generic;
using UnityEngine;

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
    private Quaternion initialRotation;
    public Cell[,,] rootGridSnapshot;
    public Vector2 bestAction;
    public int bestPathLength;
    public int rolloutCount;

    public void beginSearch(GameManager gameManager)
    {
        this.gameManager = gameManager;
        initialRotation = gameManager.envBounds.transform.rotation;
        rootGridSnapshot = (Cell[,,])gameManager.ca.grid.Clone();
        bestAction = Vector2.zero;
        bestPathLength = 10;
        rolloutCount = 0;
    }

    public void SearchStep()
    {
        // reset the env
        gameManager.ca.grid = (Cell[,,])rootGridSnapshot.Clone();
        gameManager.envBounds.transform.rotation = initialRotation;

        // rollout
        for (int i = 0; i < bestPathLength; i++)
        {
            Vector2 nextAction = RandomAction();
            gameManager.envBounds.transform.Rotate(nextAction.x, nextAction.y, 0, Space.World);
            Vector3 newGravity = gameManager.envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
            gameManager.ca.UpdateGravity(newGravity);
            // simulate 5 steps after each action
            for (int j = 0; j < 5; j++)
            {
                gameManager.ca.SimulateStep();
            }
            // update best path
            ////////////////
        }
    }

    public Vector2 PeakDecision()
    {
        return bestAction;
    }

    public Vector2 Decide()
    {
        gameManager.envBounds.transform.rotation = initialRotation;
        return bestAction;
    }

    public Vector2 RandomAction()
    {
        float u = Random.Range(0, 1);
        float v = Random.Range(0, 1);
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
