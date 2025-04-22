using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public enum Operations
    {
        Union,
        Subtract,
        Intersect
    }
    
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
    
    public class ShapeProvider : MonoBehaviour, IBoundsProvider
    {
        public static Dictionary<Type, Delegate> BoundsGenerator = new();
        
        public Shapes Shape = Shapes.Sphere;
        public Vector3 Size = Vector3.one * 0.5f;
        public float ExpandBounds;
        public bool UseLossyScale = true;
        public Vector3 Pivot = Vector3.one * 0.5f;
        
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend;
        [Range(0, 1f)] public float Roundness;
        public Color Color = Color.white;
        public Color GradientColor = Color.white;

        static ShapeProvider()
        {
            BoundsGenerator.Add(typeof(Aabb), (Func<Transform, Vector3, Vector3, Vector3, Aabb>)Aabb.Create);
        }
        
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
        
        public static Vector3 GetShapeSize(Shapes shape, Vector3 size)
        {
            switch (shape)
            {
                case Shapes.Sphere:
                case Shapes.Octahedron:
                    return Vector3.one * size.x;
                
                case Shapes.Capsule:
                    return new Vector3(size.x, (size.y + size.x) * 0.5f + size.x * 0.5f, size.x);
                
                case Shapes.Cylinder:
                    return new Vector3(size.x, size.y, size.x);
                
                case Shapes.Torus:
                    return Vector3.one * (size.x + size.y);
                
                case Shapes.CappedTorus:
                    return Vector3.one * (size.y + size.z);
                
                case Shapes.Link:
                    float xz = size.x + size.z;
                    return new Vector3(xz, size.y + xz, xz);
                
                case Shapes.CappedCone:
                    float max = Mathf.Max(size.x, size.z);
                    return new Vector3(max, size.y, max);
                
                default:
                    return size;
            }
        }
        
        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (!BoundsGenerator.TryGetValue(typeof(T), out Delegate generator))
                throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
            
            var boundsGenerator = generator as Func<Transform, Vector3, Vector3, Vector3, T>;
            T bounds = boundsGenerator(transform, GetShapeSize(Shape, Size), GetScale(), Pivot);
            return bounds.Expand(Blend + Roundness + ExpandBounds + 0.001f);
        }
        
        public Vector3 GetScale() => UseLossyScale ? transform.lossyScale : Vector3.one;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData : ISetupFrom<ShapeProvider>
    {
        public static readonly int Stride = sizeof(float) * 24 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;

        public void SetupFrom(ShapeProvider data)
        {
            if (!data) return;
            
            Type = (int)data.Shape;
            Transform = data.UseLossyScale ? data.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(data.transform.position, data.transform.rotation, Vector3.one).inverse;
            Size = data.Size;
            Pivot = data.Pivot;
            Operation = (int)data.Operation;
            Blend = data.Blend;
            Roundness = data.Roundness;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorShapeData : ISetupFrom<ShapeProvider>
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

        public void SetupFrom(ShapeProvider data)
        {
            if (!data) return;
            
            Type = (int)data.Shape;
            Transform = data.UseLossyScale ? data.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(data.transform.position, data.transform.rotation, Vector3.one).inverse;
            Size = data.Size;
            Pivot = data.Pivot;
            Operation = (int)data.Operation;
            Blend = data.Blend;
            Roundness = data.Roundness;
            Color = data.Color;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GradientColorShapeData : ISetupFrom<ShapeProvider>
    {
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;
        public Vector4 Color;
        public Vector4 GradientColor;

        public void SetupFrom(ShapeProvider data)
        {
            if (!data) return;
            
            Type = (int)data.Shape;
            Transform = data.UseLossyScale ? data.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(data.transform.position, data.transform.rotation, Vector3.one).inverse;
            Size = data.Size;
            Pivot = data.Pivot;
            Operation = (int)data.Operation;
            Blend = data.Blend;
            Roundness = data.Roundness;
            Color = data.Color;
            GradientColor = data.GradientColor;
        }
    }
}