using System;
using System.Collections.Generic;
using UnityEngine;

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
    public float bestPathExtractedWater;
    public int rolloutCount;

    public void beginSearch(GameManager gameManager)
    {
        this.gameManager = gameManager;
        rootSnapshot = new CellularAutomataSnapshot(gameManager);
        bestAction = Vector2.zero;
        bestPathLength = 6;
        bestPathExtractedWater = 0;
        rolloutCount = 0;
    }

    public void SearchStep()
    {
        rootSnapshot.RestoreSnapshot(gameManager, false);

        // rollout
        Vector2 firstAction = Vector2.zero;
        for (int i = 1; i <= bestPathLength; i++)
        {
            Vector2 nextAction = RandomAction();
            if (i == 1) firstAction = nextAction;
            gameManager.envBounds.transform.Rotate(0, nextAction.y, 0, Space.World);
            gameManager.envBounds.transform.Rotate(nextAction.x, 0, 0, Space.World);
            Vector3 newGravity = gameManager.envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
            gameManager.ca.UpdateGravity(newGravity);
            // simulate 5 seconds after each action
            float sps = gameManager.simsPerSec == 0 ? 5 : gameManager.simsPerSec;
            for (int j = 0; j < sps * 5; j++)
            {
                gameManager.ca.SimulateStep();
            }
            // update best path
            float extractedWater = 1 - gameManager.ca.totalVolume / gameManager.ca.initialTotalVolume;
            if (extractedWater > 0.95f)
            {
                bestPathLength = i;
                bestAction = firstAction;
                bestPathExtractedWater = extractedWater;
            }
            else if (bestPathLength == i && extractedWater > bestPathExtractedWater)
            {
                bestAction = firstAction;
                bestPathExtractedWater = extractedWater;
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
        float u = UnityEngine.Random.Range(0f, 1f);
        float v = UnityEngine.Random.Range(0f, 1f);
        float theta = Mathf.Acos(2 * u - 1);
        float phi = 2 * Mathf.PI * v;
        theta *= Mathf.Rad2Deg;
        phi *= Mathf.Rad2Deg;
        return new Vector2(theta, phi);
    }
}

public struct RolloutResult
{
    public float extractedWaterPercentage;
    public int depth;

    public RolloutResult(float extractedWaterPercentage, int depth)
    {
        this.extractedWaterPercentage = extractedWaterPercentage;
        this.depth = depth;
    }
}

public class MCTSNode
{
    // state representation
    public CellularAutomataSnapshot caSnapshot;
    public int depth;

    // Tree structure
    public MCTSNode parent;
    public List<MCTSNode> children;

    // MCTS statistics
    public int visits;
    public float eval;
    public RolloutResult bestRollout;

    public MCTSNode(GameManager gameManager, MCTSNode parent = null)
    {
        caSnapshot = new CellularAutomataSnapshot(gameManager);
        depth = 0;
        this.parent = parent;
        children = new List<MCTSNode>();
        visits = 0;
        eval = 0;
        bestRollout = new RolloutResult(0, int.MaxValue);
    }
}

public class MCTS : AI
{
    public GameManager gameManager;
    public MCTSNode rootNode { get; private set; }
    private int maxDepth = 6;

    //////////////////////////////// weights and parameters for tuning ////////////////////////////////

    // exploration vs exploitation
    public float shouldExpandNewChildUCTThreshold = 0.5f; // weight in range [0, 1]
    public float selectionExplorationWeight = Mathf.Sqrt(2); // weight in range {0, 0.2, 0.4, 0.6, 0.8, 1, 1.4, 1.8, 2.2, 3, 4, 5, 6, 8, 10, 14, 18, 25, 50, 75, 100, 200, 500, 1000, 5000, 10000}
    public float expansionExplorationWeight = 0.3f * Mathf.Sqrt(2); // weight in range {0, 0.2, 0.4, 0.6, 0.8, 1, 1.4, 1.8, 2.2, 3, 4, 5, 6, 8, 10, 14, 18, 25, 50, 75, 100, 200, 500, 1000, 5000, 10000}
    public float expansionDepthWeight = 0.5f; // weight in range {0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.7, 0.9, 1.2, 1.5, 1.9, 2.5}

    // evaluation, weights should sum to 1
    public float evalWeightDepth = 0.1f;
    public float evalWeightExtractedWater = 0.3f;
    public float evalWeightRollout = 0.3f;
    public float evalWeightAverageChildren = 0.1f;
    public float evalWeightMaxChild = 0.2f;

    //////////////////////////////// Initialization and main loop ////////////////////////////////

    public void beginSearch(GameManager gameManager)
    {
        this.gameManager = gameManager;
        rootNode = new MCTSNode(gameManager);
    }

    public void SearchStep()
    {
        // Select a leaf node
        MCTSNode node = Select();

        // Expand a new node if max depth has not been reached, leave one depth level for the rollout phase
        if (node.depth < maxDepth - 1) node = Expand(node);

        // Rollout from the new node
        RolloutResult rolloutResult = Rollout(node);

        // Use the result of the rollout to update the evaluation of the new node and its ancestors
        BackPropagate(node, rolloutResult);
    }

    //////////////////////////////// Selection Phase ////////////////////////////////

    private MCTSNode Select()
    {
        // Select a leaf node
        MCTSNode selectedNode = rootNode;
        while (selectedNode.children.Count > 0)
        {
            MCTSNode newNode = SelectChildNode(selectedNode);
            if (newNode == selectedNode) break;
            selectedNode = newNode;
        }
        return selectedNode;
    }

    private MCTSNode SelectChildNode(MCTSNode parent)
    {
        // Decide whether to expand a new child (selecting the parent will expand a new child for it) or select an existing child
        if (ShouldExpandNewChild(parent))
        {
            return parent;
        }
        else
        {
            // Select an existing child based on UCT values
            MCTSNode selectedChild = null;
            float maxUCTValue = float.MinValue;

            foreach (var child in parent.children)
            {
                float uctValue = UCT(child, selectionExplorationWeight);
                if (uctValue > maxUCTValue)
                {
                    maxUCTValue = uctValue;
                    selectedChild = child;
                }
            }

            // Return the selected child for further exploration
            return selectedChild;
        }
    }

    private bool ShouldExpandNewChild(MCTSNode parent)
    {
        if (parent.children.Count == 0) throw new Exception("parent has no children to select from");

        // Calculate the average UCT value of existing children
        float averageUCTValue = 0;
        foreach (var child in parent.children) averageUCTValue += UCT(child, expansionExplorationWeight);
        averageUCTValue /= parent.children.Count;

        // Calculate the depth factor, encouraging expansion of nodes at shallower depths
        float depthFactor = expansionDepthWeight * (maxDepth - parent.depth) / maxDepth;

        // Decide based on the average UCT value and the depth of the parent node
        return (averageUCTValue - depthFactor) < shouldExpandNewChildUCTThreshold;
    }

    private float UCT(MCTSNode node, float explorationWeight)
    {
        float value = node.eval + explorationWeight * Mathf.Sqrt(Mathf.Log(node.parent.visits, Mathf.Exp(1)) / node.visits);
        float maxValue = 1 + explorationWeight * Mathf.Sqrt(Mathf.Log(node.parent.visits, Mathf.Exp(1)));
        return value / maxValue; // normalize to [0, 1]
    }

    //////////////////////////////// Expansion Phase ////////////////////////////////

    private MCTSNode Expand(MCTSNode parent)
    {
        // Get the state represented by the parent node
        parent.caSnapshot.RestoreSnapshot(gameManager, false);

        // Rollout the game for one step
        RolloutStep(10);

        // create a child node for the resulting state
        MCTSNode child = new(gameManager, parent)
        {
            depth = parent.depth + 1
        };
        parent.children.Add(child);
        return child;
    }

    //////////////////////////////// Rollout Phase ////////////////////////////////

    private RolloutResult Rollout(MCTSNode leafNode)
    {
        // Get the current state represented by the node
        leafNode.caSnapshot.RestoreSnapshot(gameManager, false);

        // Rollout the game until the maximum depth is reached or the extracted water exceeds 95%
        int depth;
        for (depth = leafNode.depth; depth < maxDepth; depth++)
        {
            RolloutStep(5);

            // Finish the rollout if the remaining water is less than 5%
            if (gameManager.ca.totalVolume / gameManager.ca.initialTotalVolume < 0.05f) break;
        }

        return new RolloutResult(1.0f - gameManager.ca.totalVolume / gameManager.ca.initialTotalVolume, depth);
    }

    public void RolloutStep(int secondsToSimulate)
    {
        // Apply a random rotation
        Vector2 action = RandomAction();
        gameManager.envBounds.transform.Rotate(0, action.y, 0, Space.World);
        gameManager.envBounds.transform.Rotate(action.x, 0, 0, Space.World);
        Vector3 newGravity = gameManager.envBounds.transform.InverseTransformDirection(Vector3.down).normalized * 10;
        gameManager.ca.UpdateGravity(newGravity);

        // simulate the game for some time
        float sps = gameManager.simsPerSec == 0 ? 5 : gameManager.simsPerSec;
        for (int i = 0; i < sps * secondsToSimulate; i++)
        {
            gameManager.ca.SimulateStep();
        }
    }

    private Vector2 RandomAction()
    {
        float u = UnityEngine.Random.Range(0f, 1f);
        float v = UnityEngine.Random.Range(0f, 1f);
        float theta = Mathf.Acos(2 * u - 1);
        float phi = 2 * Mathf.PI * v;
        theta *= Mathf.Rad2Deg;
        phi *= Mathf.Rad2Deg;
        return new Vector2(theta, phi);
    }

    //////////////////////////////// Backpropagation Phase ////////////////////////////////

    private void BackPropagate(MCTSNode leafNode, RolloutResult rolloutResult)
    {
        // Update the best rollout result if the new result is better for the leaf node and its ancestors
        for (MCTSNode currentNode = leafNode; currentNode != null; currentNode = currentNode.parent)
        {
            if ((rolloutResult.depth < currentNode.bestRollout.depth) ||
                (rolloutResult.depth == currentNode.bestRollout.depth &&
                rolloutResult.extractedWaterPercentage > currentNode.bestRollout.extractedWaterPercentage))
            {
                currentNode.bestRollout = rolloutResult;
            }
        }

        // Update the visits and evaluation of the leaf node and its ancestors
        for (MCTSNode currentNode = leafNode; currentNode != null; currentNode = currentNode.parent)
        {
            currentNode.visits++;
            currentNode.eval = EvaluateNode(currentNode);
        }
    }

    private float EvaluateNode(MCTSNode node)
    {
        // Penalize higher depth, encourage shallower solutions
        float depthScore = 1.0f - (float)node.depth / maxDepth;

        // Use the existing extracted water percentage
        float extractedWaterPercentage = (rootNode.caSnapshot.totalVolume - node.caSnapshot.totalVolume) / rootNode.caSnapshot.totalVolume;

        // Use the evaluation of child nodes
        float averageChildrenValue = 0.0f;
        float maxChildValue = 0.0f;
        if (node.children.Count > 0)
        {
            maxChildValue = float.MinValue;
            foreach (var child in node.children)
            {
                float childValue = child.eval;
                averageChildrenValue += childValue;
                if (childValue > maxChildValue) maxChildValue = childValue;
            }
            averageChildrenValue /= node.children.Count;
        }

        // Use the best rollout result
        float rolloutFactor = node.bestRollout.extractedWaterPercentage * (1.0f - (float)node.bestRollout.depth / maxDepth);

        node.eval = evalWeightDepth * depthScore +
                    evalWeightExtractedWater * extractedWaterPercentage +
                    evalWeightRollout * rolloutFactor +
                    evalWeightAverageChildren * averageChildrenValue +
                    evalWeightMaxChild * maxChildValue;

        return node.eval;
    }

    //////////////////////////////// Decision Phase ////////////////////////////////

    public Vector2 PeakDecision()
    {
        // Select the best action based on the evaluation of the children of the root node
        float maxEval = float.MinValue;
        Vector2 bestAction = Vector2.zero;
        for (int i = 0; i < rootNode.children.Count; i++)
        {
            float eval = rootNode.children[i].eval;
            if (eval > maxEval)
            {
                maxEval = eval;
                bestAction = rootNode.children[i].caSnapshot.rotation.eulerAngles;
            }
        }
        return bestAction;
    }

    public Vector2 Decide()
    {
        // restore the original state
        rootNode.caSnapshot.RestoreSnapshot(gameManager, true);

        return PeakDecision();
    }
}
