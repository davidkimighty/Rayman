using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Cel Shade")]
    public class CelShadeDataProvider : DataProvider
    {
        public static readonly int MainCelCountId = Shader.PropertyToID("_MainCelCount");
        public static readonly int AdditionalCelCountId = Shader.PropertyToID("_AdditionalCelCount");
        public static readonly int CelSpreadId = Shader.PropertyToID("_CelSpread");
        public static readonly int CelSharpnessId = Shader.PropertyToID("_CelSharpness");
        public static readonly int SpecularSharpnessId = Shader.PropertyToID("_SpecularSharpness");
        public static readonly int RimAmountId = Shader.PropertyToID("_RimAmount");
        public static readonly int RimSmoothnessId = Shader.PropertyToID("_RimSmoothness");
        public static readonly int BlendDiffuseId = Shader.PropertyToID("_BlendDiffuse");
        
        [Header("Cel Shade")]
        [Range(1, 10), SerializeField] private int mainCelCount = 1;
        [Range(1, 10), SerializeField] private int additionalCelCount = 1;
        [Range(0f, 1f), SerializeField] private float celSpread = 1f;
        [SerializeField] private float mainCelSharpness = 100f;
        [SerializeField] private float specularCelSharpness = 30f;
        [Range(0f, 1f), SerializeField] private float rimAmount = 0.75f;
        [Range(0f, 1f), SerializeField] private float rimSmoothness = 0.03f;
        [Range(0f, 1f), SerializeField] private float blendDiffuse = 0.9f;
        
        public override void ProvideShaderProperties(ref Material material)
        {
            material.SetFloat(MainCelCountId, mainCelCount);
            material.SetFloat(AdditionalCelCountId, additionalCelCount);
            material.SetFloat(CelSpreadId, celSpread);
            material.SetFloat(CelSharpnessId, mainCelSharpness);
            material.SetFloat(SpecularSharpnessId, specularCelSharpness);
            material.SetFloat(RimAmountId, rimAmount);
            material.SetFloat(RimSmoothnessId, rimSmoothness);
            material.SetFloat(BlendDiffuseId, blendDiffuse);
        }
    }
}
