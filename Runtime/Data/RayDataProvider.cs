using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Ray")]
    public class RayDataProvider : DataProvider
    {
        public static readonly int EpsilonMinId = Shader.PropertyToID("_EpsilonMin");
        public static readonly int EpsilonMaxId = Shader.PropertyToID("_EpsilonMax");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");
        
        [SerializeField] private float epsilonMin = 0.001f;
        [SerializeField] private float epsilonMax = 0.01f;
        [SerializeField] private int maxSteps = 64;
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private int shadowMaxSteps = 16;
        [SerializeField] private float shadowMaxRayDistance = 30f;
        
        public override void ProvideShaderProperties(ref Material material)
        {
            material.SetFloat(EpsilonMinId, epsilonMin);
            material.SetFloat(EpsilonMaxId, epsilonMax);
            material.SetInt(MaxStepsId, maxSteps);
            material.SetFloat(MaxDistanceId, maxRayDistance);
            material.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            material.SetFloat(ShadowMaxDistanceId, shadowMaxRayDistance);
        }
    }
}
