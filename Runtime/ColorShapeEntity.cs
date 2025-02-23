using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class ColorShapeEntity : RaymarchShapeEntity
    {
        [Header("Color")]
        public Color Color;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorShapeData
    {
        public static readonly int Stride = sizeof(float) * 28 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;
        public Vector4 Color;

        public ColorShapeData(ColorShapeEntity shape)
        {
            Type = (int)shape.Shape;
            Transform = shape.UseLossyScale ? shape.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(shape.transform.position, shape.transform.rotation, Vector3.one).inverse;
            Size = shape.Size;
            Pivot = shape.Pivot;
            Operation = (int)shape.Operation;
            Blend = shape.Blend;
            Roundness = shape.Roundness;
            Color = shape.Color;
        }
    }
}
