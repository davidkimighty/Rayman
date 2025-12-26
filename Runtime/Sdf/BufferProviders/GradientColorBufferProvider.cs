using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class GradientColorBufferProvider : ColorBufferProvider<GradientColorData>
    {
        public override void InitializeBuffer(ref Material material, VisualProvider[] dataProviders)
        {
            material.EnableKeyword("_GRADIENT_COLOR");
            base.InitializeBuffer(ref material, dataProviders);
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GradientColorData : IPopulateData<ColorProvider>
    {
        public float4 Color;
        public float4 GradientColor;
        public int UseGradient; // 4byte alignment

        public void Populate(ColorProvider provider)
        {
            Color = (Vector4)provider.Color;
            GradientColor = (Vector4)provider.GradientColor;
            UseGradient = provider.UseGradient ? 1 : 0;
        }
    }
}