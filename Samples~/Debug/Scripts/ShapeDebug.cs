using Rayman;
using UnityEngine;

public enum DebugModeTypes { Normal, Hitmap, BoundingVolume, }

public class ShapeDebug : MonoBehaviour
{
    private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
    private static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");

    [SerializeField] private RaymarchObject raymarchObject;
    [SerializeField] private DebugModeTypes debugMode = DebugModeTypes.Hitmap;
    [SerializeField] private int boundsDisplayThreshold = 300;

    private void OnEnable()
    {
        if (!raymarchObject) return;
            
        if (raymarchObject.Material)
            Setup(raymarchObject);
        
        raymarchObject.OnCreateMaterial += Setup;
    }

    private void OnDisable()
    {
        if (!raymarchObject) return;
            
        raymarchObject.OnCreateMaterial -= Setup;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!raymarchObject)
            raymarchObject = GetComponent<RaymarchObject>();
            
        if (raymarchObject)
            SetupShaderProperties(raymarchObject.Material);
    }
#endif

    private void Setup(IMaterialProvider provider)
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