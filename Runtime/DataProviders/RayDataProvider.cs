using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Ray")]
    public class RayDataProvider : MaterialDataProvider
    {
        public static readonly int EpsilonMinId = Shader.PropertyToID("_EpsilonMin");
        public static readonly int EpsilonMaxId = Shader.PropertyToID("_EpsilonMax");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int DepthNormalMaxStepsId = Shader.PropertyToID("_DepthNormalMaxSteps");
        public static readonly int DepthNormalMaxDistanceId = Shader.PropertyToID("_DepthNormalMaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");
        
        public float EpsilonMin = 0.001f;
        public float EpsilonMax = 0.01f;
        public int MaxSteps = 64;
        public float MaxRayDistance = 100f;
        public int DepthNormalMaxSteps = 16;
        public float DepthNormalMaxRayDistance = 100f;
        public int ShadowMaxSteps = 16;
        public float ShadowMaxRayDistance = 30f;
        
        public override void ProvideData(ref Material material)
        {
            material.SetFloat(EpsilonMinId, EpsilonMin);
            material.SetFloat(EpsilonMaxId, EpsilonMax);
            material.SetInt(MaxStepsId, MaxSteps);
            material.SetFloat(MaxDistanceId, MaxRayDistance);
            material.SetInt(DepthNormalMaxStepsId, DepthNormalMaxSteps);
            material.SetFloat(DepthNormalMaxDistanceId, DepthNormalMaxRayDistance);
            material.SetInt(ShadowMaxStepsId, ShadowMaxSteps);
            material.SetFloat(ShadowMaxDistanceId, ShadowMaxRayDistance);
        }
    }
}