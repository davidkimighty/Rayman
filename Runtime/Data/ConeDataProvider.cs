using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Cone")]
    public class ConeDataProvider : DataProvider
    {
        public static readonly int EpsilonId = Shader.PropertyToID("_Epsilon");
        public static readonly int PassCountId = Shader.PropertyToID("_PassCount");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");
        public static readonly int TangentHalfFovId = Shader.PropertyToID("_TangentHalfFov");
        public static readonly int ConeSubdivisionId = Shader.PropertyToID("_ConeSubdivision");

        public float Epsilon = 0.001f;
        public int PassCount = 4;
        public int MaxSteps = 64;
        public float MaxRayDistance = 100f;
        public int ShadowMaxSteps = 16;
        public float ShadowMaxRayDistance = 30f;
        public float ConeSubdivision = 4f;
        
        public override void ProvideData(ref Material material)
        {
            material.SetFloat(EpsilonId, Epsilon);
            material.SetInt(PassCountId, PassCount);
            material.SetInt(MaxStepsId, MaxSteps);
            material.SetFloat(MaxDistanceId, MaxRayDistance);
            material.SetInt(ShadowMaxStepsId, ShadowMaxSteps);
            material.SetFloat(ShadowMaxDistanceId, ShadowMaxRayDistance);
            material.SetFloat(TangentHalfFovId, Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad));
            material.SetFloat(ConeSubdivisionId, ConeSubdivision);
        }
    }
}
