using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class CelColorShapeGroup : ColorShapeGroup
    {
        private static readonly int MainCelCountId = Shader.PropertyToID("_MainCelCount");
        private static readonly int AdditionalCelCountId = Shader.PropertyToID("_AdditionalCelCount");
        private static readonly int CelSpreadId = Shader.PropertyToID("_CelSpread");
        private static readonly int CelSharpnessId = Shader.PropertyToID("_CelSharpness");
        private static readonly int SpecularSharpnessId = Shader.PropertyToID("_SpecularSharpness");
        private static readonly int RimAmountId = Shader.PropertyToID("_RimAmount");
        private static readonly int RimSmoothnessId = Shader.PropertyToID("_RimSmoothness");
        private static readonly int BlendDiffuseId = Shader.PropertyToID("_BlendDiffuse");
        
        [Header("Cel Shade")]
        [Range(1, 10), SerializeField] private int mainCelCount = 1;
        [Range(1, 10), SerializeField] private int additionalCelCount = 1;
        [Range(0f, 1f), SerializeField] private float celSpread = 1f;
        [SerializeField] private float mainCelSharpness = 100f;
        [SerializeField] private float specularCelSharpness = 30f;
        [Range(0f, 1f), SerializeField] private float rimAmount = 0.75f;
        [Range(0f, 1f), SerializeField] private float rimSmoothness = 0.03f;
        [Range(0f, 1f), SerializeField] private float blendDiffuse = 0.9f;
        
        public override void SetupShaderProperties(ref Material material)
        {
            base.SetupShaderProperties(ref material);
            
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
