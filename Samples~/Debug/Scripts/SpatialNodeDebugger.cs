using Rayman;
using UnityEngine;

public class SpatialNodeDebugger : DebugElement
{
    private BvhAabbBufferProvider[] nodeBufferProviders;
    
    private void Awake()
    {
        nodeBufferProviders = FindObjectsByType<BvhAabbBufferProvider>(FindObjectsSortMode.None);
    }

    public override string GetDebugMessage()
    {
        int nodeCount = 0;
        for (int i = 0; i < nodeBufferProviders.Length; i++)
            nodeCount += nodeBufferProviders[i].DataCount;
        return $"Node {nodeCount, 4}";
    }
}
