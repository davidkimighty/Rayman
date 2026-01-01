using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Cel Shade")]
    public class CelShadeDataProvider : MaterialDataProvider
    {
        public static readonly int CelTexId = Shader.PropertyToID("_CelTex");
        public static readonly int CelTexScaleId = Shader.PropertyToID("_CelTexScale");
        
        public static readonly int CelTexRangeId = Shader.PropertyToID("_CelTexRange");
        public static readonly int CelCountId = Shader.PropertyToID("_CelCount");
        public static readonly int CelSpreadId = Shader.PropertyToID("_CelSpread");
        public static readonly int CelSmoothId = Shader.PropertyToID("_CelSmooth");
        public static readonly int BlendDiffuseId = Shader.PropertyToID("_BlendDiffuse");
        
        public static readonly int SpecIntensityId = Shader.PropertyToID("_SpecIntensity");
        public static readonly int SpecTexRangeId = Shader.PropertyToID("_SpecTexRange");
        public static readonly int SpecCelSpreadId = Shader.PropertyToID("_SpecCelSpread");
        public static readonly int SpecSmoothId = Shader.PropertyToID("_SpecSmooth");
        
        public static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
        public static readonly int RimTexRangeId = Shader.PropertyToID("_RimTexRange");
        public static readonly int RimCelSpreadId = Shader.PropertyToID("_RimCelSpread");
        public static readonly int RimSmoothId = Shader.PropertyToID("_RimSmooth");

        [Header("Cel Shade")]
        [SerializeField] private Texture2D celTexture;
        [SerializeField] private float celTextureScale = 3f;
        [Range(0f, 1f), SerializeField] private float celTextureRange = 0.5f;
        [Range(1, 10), SerializeField] private float celCount = 1f;
        [Range(0f, 1f), SerializeField] private float celSpread = 1f;
        [Range(0f, 1f), SerializeField] private float celSmooth = 1f;
        [Range(0f, 1f), SerializeField] private float blendDiffuse = 0.7f;
        
        [Range(0f, 10f), SerializeField] private float specIntensity = 1f;
        [Range(0f, 1f), SerializeField] private float specTextureRange = 0.5f;
        [Range(0f, 1f), SerializeField] private float specCelSpread = 1f;
        [Range(0f, 1f), SerializeField] private float specSmooth = 1f;
        
        [Range(0f, 10f), SerializeField] private float rimIntensity = 1f;
        [Range(0f, 1f), SerializeField] private float rimTexRange = 0.3f;
        [Range(0f, 1f), SerializeField] private float rimCelSpread = 1f;
        [Range(0f, 1f), SerializeField] private float rimSmoothness = 1f;
        
        public override void ProvideData(ref Material material)
        {
            material.SetTexture(CelTexId, celTexture);
            material.SetFloat(CelTexScaleId, celTextureScale);
            
            material.SetFloat(CelTexRangeId, celTextureRange);
            material.SetFloat(CelCountId, celCount);
            material.SetFloat(CelSpreadId, celSpread);
            material.SetFloat(CelSmoothId, celSmooth);
            material.SetFloat(BlendDiffuseId, blendDiffuse);
            
            material.SetFloat(SpecIntensityId, specIntensity);
            material.SetFloat(SpecTexRangeId, specTextureRange);
            material.SetFloat(SpecCelSpreadId, specCelSpread);
            material.SetFloat(SpecSmoothId, specSmooth);
            
            material.SetFloat(RimIntensityId, rimIntensity);
            material.SetFloat(RimTexRangeId, rimTexRange);
            material.SetFloat(RimCelSpreadId, rimCelSpread);
            material.SetFloat(RimSmoothId, rimSmoothness);
        }
    }
}
