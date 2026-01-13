using Rayman;
using UnityEngine;

public class SpatialNodeDebugger : DebugElement
{
    private ShapeObject[] shapes;

    private void Awake()
    {
        shapes = FindObjectsByType<ShapeObject>(FindObjectsSortMode.None);
    }

    public override string GetDebugMessage()
    {
        int nodeCount = 0;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i].IsInitialized)
                nodeCount += shapes[i].NodeCount;
        }
        return $"Node {nodeCount, 4}";
    }
}
