using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/PBR")]
    public class PbrDataProvider : MaterialDataProvider
    {
        public static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        public static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        public static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        public static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        
        [SerializeField] private Texture baseMap;
        [Range(0f, 1f), SerializeField] private float metallic;
        [Range(0f, 1f), SerializeField] private float smoothness = 0.5f;
        [ColorUsage(true, true), SerializeField] private Color emissionColor;
        
        public override void ProvideData(ref Material material)
        {
            if (material == null) return;
            
            material.SetTexture(BaseMapId, baseMap);
            material.SetFloat(MetallicId, metallic);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetColor(EmissionColorId, emissionColor);
        }
    }
}
