using Rayman;
using UnityEngine;

public class ShapeCountDebugger : DebugElement
{
    private BufferProvider<ShapeProvider>[] shapeBufferProviders;
    private void Awake()
    {
        shapeBufferProviders = FindObjectsByType<BufferProvider<ShapeProvider>>(FindObjectsSortMode.None);
    }

    public override string GetDebugMessage()
    {
        int sdfCount = 0;
        for (int i = 0; i < shapeBufferProviders.Length; i++)
            sdfCount += shapeBufferProviders[i].DataCount;
        return $"SDF {sdfCount,4}";
    }
}
