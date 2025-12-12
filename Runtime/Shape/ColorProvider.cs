using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class ColorProvider : VisualProvider
    {
        public Color Color;
        public Color GradientColor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorData
    {
        public Vector4 Color;
        public Vector4 GradientColor;

        public ColorData(ColorProvider provider)
        {
            Color = provider.Color;
            GradientColor = provider.GradientColor;
        }
    }
}
