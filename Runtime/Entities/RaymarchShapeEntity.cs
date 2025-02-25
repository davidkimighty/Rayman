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
    
    public class RaymarchShapeEntity : RaymarchEntity
    {
        [Header("Shape")]
        public Vector3 Size = Vector3.one * 0.5f;
        public Vector3 Pivot = Vector3.one * 0.5f;
        public Shapes Shape = Shapes.Sphere;
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend;
        [Range(0, 1f)] public float Roundness;
        public Color Color = Color.white;
        public Color GradientColor = Color.white;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Size = new Vector3(
                Mathf.Max(Size.x, 0),
                Mathf.Max(Size.y, 0),
                Mathf.Max(Size.z, 0));
            
            Pivot = new Vector3(
                Mathf.Clamp(Pivot.x, 0, 1),
                Mathf.Clamp(Pivot.y, 0, 1),
                Mathf.Clamp(Pivot.z, 0, 1));
        }
#endif
        
        public override T GetBounds<T>()
        {
            if (typeof(T) == typeof(Aabb))
            {
                Aabb aabb = new Aabb(transform, GetShapeSize(), GetScale(), Pivot);
                aabb = aabb.Expand(Blend + Roundness + ExpandBounds + 0.001f);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }

        public Vector3 GetShapeSize()
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
    public struct ShapeData : IRaymarchEntityData
    {
        public static readonly int Stride = sizeof(float) * 24 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;

        public void InitializeData(RaymarchEntity entity)
        {
            RaymarchShapeEntity shape = entity as RaymarchShapeEntity;
            if (shape == null) return;
            
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
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorShapeData : IRaymarchEntityData
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

        public void InitializeData(RaymarchEntity entity)
        {
            RaymarchShapeEntity shape = entity as RaymarchShapeEntity;
            if (shape == null) return;
            
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