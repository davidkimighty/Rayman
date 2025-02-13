using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public enum Shapes
    {
        Sphere,
        Ellipsoid,
        Box,
        Octahedron,
        Capsule,
        Cylinder,
        Torus,
        CappedTorus,
        Link,
        CappedCone,
    }
    
#if UNITY_EDITOR
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
    
    public class RaymarchShape : RaymarchEntity
    {
        [Header("Shape")]
        public Shapes Shape = Shapes.Sphere;
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend;
        [Range(0, 1f)] public float Roundness;

        public override T GetBounds<T>()
        {
            if (typeof(T) == typeof(AABB))
            {
                AABB aabb = AABB.GetBounds(transform, GetShapeSize(), GetScale(), Pivot);
                aabb = aabb.Expand(Blend + Roundness + ExpandBounds + 0.001f);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }

        protected Vector3 GetShapeSize()
        {
            switch (Shape)
            {
                case Shapes.Sphere:
                case Shapes.Octahedron:
                    return Vector3.one * Size.x;
                case Shapes.Capsule:
                    return new Vector3(Size.x, (Size.y + Size.x) * 0.5f + Size.x * 0.5f, Size.x);
                case Shapes.Cylinder:
                    return new Vector3(Size.x, Size.y, Size.x);
                case Shapes.Torus:
                    return Vector3.one * (Size.x + Size.y);
                case Shapes.CappedTorus:
                    return Vector3.one * (Size.y + Size.z);
                case Shapes.Link:
                    float xz = Size.x + Size.z;
                    return new Vector3(xz, Size.y + xz, xz);
                case Shapes.CappedCone:
                    float max = Mathf.Max(Size.x, Size.z);
                    return new Vector3(max, Size.y, max);
                default:
                    return Size;
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData
    {
        public static readonly int Stride = sizeof(float) * 24 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;

        public ShapeData(RaymarchShape shape)
        {
            Type = (int)shape.Shape;
            Transform = shape.UseLossyScale ? shape.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(shape.transform.position, shape.transform.rotation, Vector3.one).inverse;
            Size = shape.Size;
            Pivot = shape.Pivot;
            Operation = (int)shape.Operation;
            Blend = shape.Blend;
            Roundness = shape.Roundness;
        }
    }
}