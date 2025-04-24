using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Cel Shade")]
    public class CelShadeDataProvider : DataProvider
    {
        public static readonly int CelCountId = Shader.PropertyToID("_CelCount");
        public static readonly int CelSpreadId = Shader.PropertyToID("_CelSpread");
        public static readonly int CelSharpnessId = Shader.PropertyToID("_CelSharpness");
        public static readonly int SpecularSmoothnessId = Shader.PropertyToID("_SpecularSharpness");
        public static readonly int RimAmountId = Shader.PropertyToID("_RimAmount");
        public static readonly int RimSmoothnessId = Shader.PropertyToID("_RimSmoothness");
        public static readonly int BlendDiffuseId = Shader.PropertyToID("_BlendDiffuse");
        
        [Header("Cel Shade")]
        [Range(1, 10), SerializeField] private int celCount = 1;
        [Range(0f, 1f), SerializeField] private float celSpread = 1f;
        [SerializeField] private float celSharpness = 30f;
        [SerializeField] private float specularSharpness = 10f;
        [Range(0f, 1f), SerializeField] private float rimAmount = 0.2f;
        [Range(0f, 1f), SerializeField] private float rimSmoothness = 0.03f;
        [Range(0f, 1f), SerializeField] private float blendDiffuse = 0.9f;
        
        public override void ProvideData(ref Material material)
        {
            material.SetFloat(CelCountId, celCount);
            material.SetFloat(CelSpreadId, celSpread);
            material.SetFloat(CelSharpnessId, celSharpness);
            material.SetFloat(SpecularSmoothnessId, specularSharpness);
            material.SetFloat(RimAmountId, rimAmount);
            material.SetFloat(RimSmoothnessId, rimSmoothness);
            material.SetFloat(BlendDiffuseId, blendDiffuse);
        }
    }
}
