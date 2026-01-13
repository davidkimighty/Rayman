using Rayman;
using UnityEngine;

public class ShapeCountDebugger : DebugElement
{
    private ShapeObject[] shapes;

    private void Awake()
    {
        shapes = FindObjectsByType<ShapeObject>(FindObjectsSortMode.None);
    }

    public override string GetDebugMessage()
    {
        int sdfCount = 0;
        for (int i = 0; i < shapes.Length; i++)
            sdfCount += shapes[i].ShapeProviders.Count;
        return $"SDF {sdfCount,4}";
    }
}
