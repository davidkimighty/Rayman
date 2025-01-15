using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class RaymarchShapeColor : RaymarchShape
    {
        [Header("Color")]
        public Color Color;
        [ColorUsage(true, true)] public Color EmissionColor;
        [Range(0, 1f)] public float EmissionIntensity;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeColorData
    {
        public static readonly int Stride = sizeof(float) * 33 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Smoothness;
        public float Roundness;
        public Vector4 Color;
        public Vector4 EmissionColor;
        public float EmissionIntensity;

        public ShapeColorData(RaymarchShapeColor shape)
        {
            Type = (int)shape.Shape;
            Transform = shape.UseLossyScale ? shape.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(shape.transform.position, shape.transform.rotation, Vector3.one).inverse;
            Size = shape.Size;
            Pivot = shape.Pivot;
            Operation = (int)shape.Operation;
            Smoothness = shape.Smoothness;
            Roundness = shape.Roundness;
            Color = shape.Color;
            EmissionColor = shape.EmissionColor;
            EmissionIntensity = shape.EmissionIntensity;
        }
    }
}
