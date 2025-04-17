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
    
    public class ShapeProvider : MonoBehaviour, IBoundsProvider
    {
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
            if (typeof(T) == typeof(Aabb))
            {
                Aabb aabb = new Aabb(transform, GetShapeSize(Shape, Size), GetScale(), Pivot);
                aabb = aabb.Expand(Blend + Roundness + ExpandBounds + 0.001f);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }
        
        public Vector3 GetScale() => UseLossyScale ? transform.lossyScale : Vector3.one;
    }
    
    public interface IShapeProviderData
    {
        void InitializeData(ShapeProvider provider);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData : IShapeProviderData
    {
        public static readonly int Stride = sizeof(float) * 24 + sizeof(int) * 2;
        
        public int Type;
        public Matrix4x4 Transform;
        public Vector3 Size;
        public Vector3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;

        public void InitializeData(ShapeProvider provider)
        {
            if (!provider) return;
            
            Type = (int)provider.Shape;
            Transform = provider.UseLossyScale ? provider.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(provider.transform.position, provider.transform.rotation, Vector3.one).inverse;
            Size = provider.Size;
            Pivot = provider.Pivot;
            Operation = (int)provider.Operation;
            Blend = provider.Blend;
            Roundness = provider.Roundness;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorShapeData : IShapeProviderData
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

        public void InitializeData(ShapeProvider provider)
        {
            if (!provider) return;
            
            Type = (int)provider.Shape;
            Transform = provider.UseLossyScale ? provider.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(provider.transform.position, provider.transform.rotation, Vector3.one).inverse;
            Size = provider.Size;
            Pivot = provider.Pivot;
            Operation = (int)provider.Operation;
            Blend = provider.Blend;
            Roundness = provider.Roundness;
            Color = provider.Color;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GradientColorShapeData : IShapeProviderData
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

        public void InitializeData(ShapeProvider provider)
        {
            if (!provider) return;
            
            Type = (int)provider.Shape;
            Transform = provider.UseLossyScale ? provider.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(provider.transform.position, provider.transform.rotation, Vector3.one).inverse;
            Size = provider.Size;
            Pivot = provider.Pivot;
            Operation = (int)provider.Operation;
            Blend = provider.Blend;
            Roundness = provider.Roundness;
            Color = provider.Color;
            GradientColor = provider.GradientColor;
        }
    }
}