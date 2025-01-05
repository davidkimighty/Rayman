using System;
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

    public enum Operations
    {
        Union,
        Subtract,
        Intersect
    }

    public class RaymarchShape : MonoBehaviour, IBoundsSource
    {
        [Serializable]
        public class Setting
        {
            public Shapes Shape = Shapes.Sphere;
            public Vector3 Size = Vector3.one * 0.5f;
            public Vector3 Pivot = Vector3.one * 0.5f;
            public bool UseLossyScale = true;
            public Operations Operation = Operations.Union;
            [Range(0, 1f)] public float Smoothness;
            [Range(0, 1f)] public float Roundness;
            [Range(0, 1f)] public float AdditionalExpandBounds;
            [Range(0, 1f)] public float UpdateBounds;
            public Color Color;
            [ColorUsage(true, true)] public Color EmissionColor;
            [Range(0, 1f)] public float EmissionIntensity;
        }

        private static readonly float Epsilon = 0.001f;
        
        [SerializeField] private Setting settings = new();

        public Setting Settings => settings;

        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            Vector3 scale = settings.UseLossyScale ? transform.lossyScale : Vector3.one;
            
            if (typeof(T) == typeof(AABB))
            {
                AABB aabb = GetShapeAABB(settings.Shape, settings.Size, scale);
                aabb = aabb.Expand(settings.Smoothness + settings.Roundness +
                                   settings.AdditionalExpandBounds + Epsilon);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }

        private AABB GetShapeAABB(Shapes shape, Vector3 size, Vector3 scale)
        {
            AABB aabb;
            switch (shape)
            {
                case Shapes.Ellipsoid:
                case Shapes.Box:
                    return GetAABB(size, scale);
                
                case Shapes.Capsule:
                    aabb = GetAABB(new Vector3(size.x, (size.y + size.x) * 0.5f , size.x), scale);
                    return aabb.Expand(new Vector3(0, size.x * 0.5f, 0));
                
                case Shapes.Cylinder:
                    return GetAABB(new Vector3(size.x, size.y, size.x), scale);
                
                case Shapes.Torus:
                    aabb = GetAABB(size.x + size.y, scale);
                    return aabb;
                
                case Shapes.CappedTorus:
                    aabb = GetAABB(size.y + size.z, scale);
                    return aabb;
                
                case Shapes.Link:
                    float xz = size.x + size.z;
                    return GetAABB(new Vector3(xz, size.y + xz, xz), scale);
                
                case Shapes.CappedCone:
                    float max = Mathf.Max(size.x, size.z);
                    aabb = GetAABB(new Vector3(max, size.y, max), scale);
                    return aabb;
                
                default:
                    return GetAABB(size.x, scale);
            }
        }

        private AABB GetAABB(float size, Vector3 scale)
        {
            float scaledSize = size * Mathf.Max(scale.x, scale.y, scale.z);
            Vector3 offset = (settings.Pivot - Vector3.one * 0.5f) * (2f * scaledSize);
            Vector3 rotatedOffset = transform.right * offset.x + transform.up * offset.y + transform.forward * offset.z;
            
            Vector3 extent = Vector3.one * scaledSize;
            Vector3 center = transform.position + rotatedOffset;
            
            Vector3 min = center - extent;
            Vector3 max = center + extent;
            return new AABB(min, max);
        }

        private AABB GetAABB(Vector3 size, Vector3 scale)
        {
            Vector3 right = Vector3.Scale(scale, transform.right) * size.x;
            Vector3 up = Vector3.Scale(scale, transform.up) * size.y;
            Vector3 forward = Vector3.Scale(scale, transform.forward) * size.z;

            Vector3 extent = new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z)
            );

            Vector3 offset = (settings.Pivot - Vector3.one * 0.5f) * 2f;
            Vector3 rotatedOffset = right * offset.x + up * offset.y + forward * offset.z;
            Vector3 center = transform.position + rotatedOffset;
            
            Vector3 min = center - extent;
            Vector3 max = center + extent;
            return new AABB(min, max);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            settings.Pivot = new Vector3(
                Mathf.Clamp(settings.Pivot.x, 0, 1),
                Mathf.Clamp(settings.Pivot.y, 0, 1),
                Mathf.Clamp(settings.Pivot.z, 0, 1));
        }
#endif
    }
}