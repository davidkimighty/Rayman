using Rayman;
using UnityEngine;

public enum DebugModeTypes { Normal, Hitmap, BoundingVolume, }

public class ShapeDebug : MonoBehaviour
{
    private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
    private static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

    [SerializeField] private ShapeObject shapeObject;
    [SerializeField] private DebugModeTypes debugMode = DebugModeTypes.Hitmap;
    [SerializeField] private int boundsDisplayThreshold = 300;

    private void Awake()
    {
        if (!shapeObject) return;
            
        if (shapeObject.Material)
            Setup(shapeObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!shapeObject)
            shapeObject = GetComponent<ShapeObject>();
            
        if (shapeObject)
            SetupShaderProperties(shapeObject.Material);
    }
#endif

    private void Setup(ShapeObject provider)
    {
        SetupShaderProperties(provider.Material);
    }
        
    private void SetupShaderProperties(Material material)
    {
        if (!material) return;
            
        material.SetInt(DebugModeId, (int)debugMode);
        material.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
    }
}