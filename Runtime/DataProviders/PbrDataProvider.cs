using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/PBR Data Provider")]
    public class PbrDataProvider : RaymarchDataProvider
    {
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private float smoothness = 0.5f;
        [SerializeField] private float metallic;
        [SerializeField, ColorUsage(true, true)] private Color emissionColor = Color.black;
        
        public override void SetData(ref Material mat)
        {
            mat.SetFloat(SmoothnessId, smoothness);
            mat.SetFloat(MetallicId, metallic);
            mat.SetColor(EmissionColorId, emissionColor);
        }
    }
}