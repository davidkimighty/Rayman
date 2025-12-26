using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Color")]
    public class ColorDataProvider : MaterialDataProvider
    {
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int GradientColorId = Shader.PropertyToID("_GradientColor");
        public static readonly int GradientScaleId = Shader.PropertyToID("_GradientScaleY");
        public static readonly int GradientOffsetId = Shader.PropertyToID("_GradientOffsetY");
        public static readonly int GradientAngleId = Shader.PropertyToID("_GradientAngle");

        [SerializeField] private Color Color;
        [SerializeField] private Color GradientColor;
        [SerializeField] private float GradientScaleY;
        [SerializeField] private float GradientOffsetY;
        [SerializeField] private float GradientAngle;
        
        public override void ProvideData(ref Material material)
        {
            material.SetColor(ColorId, Color);
            material.SetColor(GradientColorId, GradientColor);
            material.SetFloat(GradientScaleId, GradientScaleY);
            material.SetFloat(GradientOffsetId, GradientOffsetY);
            material.SetFloat(GradientAngleId, GradientAngle);
        }
    }
}
