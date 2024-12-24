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

    public enum Distortions
    {
        None,
        Twist,
        Bend,
    }

    public class RaymarchShape : MonoBehaviour, IBoundsSource
    {
        [Serializable]
        public class Distortion
        {
            public Distortions Type;
            public float Amount;

            public bool Enabled => Type != Distortions.None;
        }
        
        [Serializable]
        public class Setting
        {
            public Shapes Shape = Shapes.Sphere;
            public Vector3 Size = Vector3.one * 0.5f;
            public Vector3 Offset = Vector3.one * 0.5f;
            public bool UseLossyScale = true;
            public Operations Operation = Operations.Union;
            [Range(0, 1f)] public float Smoothness;
            [Range(0, 1f)] public float Roundness;
            public Color Color;
            [ColorUsage(true, true)] public Color EmissionColor;
            [Range(0, 1f)] public float EmissionIntensity;
            public Distortion Distortion;
            public float BoundsExpandSize;
        }

        private static readonly Vector3 Epsilon = Vector3.one * 0.001f;
        
        [SerializeField] private Setting settings;

        public Setting Settings => settings;

        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (typeof(T) == typeof(AABB))
            {
                Vector3 size = settings.Size + Vector3.one * (settings.Smoothness + settings.Roundness);
                AABB aabb = GetShapeAABB(settings.Shape, size);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }

        private AABB GetShapeAABB(Shapes shape, Vector3 size)
        {
            switch (shape)
            {
                case Shapes.Ellipsoid:
                case Shapes.Box:
                    return GetRotatedAABB(size);
                case Shapes.Capsule:
                    size = new Vector3(size.x, size.y * 0.5f + size.x, size.x);
                    return GetRotatedAABB(size);
                case Shapes.Cylinder:
                    size = new Vector3(size.x, size.y, size.x);
                    return GetRotatedAABB(size);
                case Shapes.Torus:
                case Shapes.CappedTorus:
                    float xy = size.x + size.y;
                    size = new Vector3(xy, size.y, xy);
                    return GetRotatedAABB(size);
                case Shapes.Link:
                    float xz = size.x + size.z;
                    size = new Vector3(xz, size.y + xz, xz);
                    return GetRotatedAABB(size);
                case Shapes.CappedCone:
                    float max = Mathf.Max(size.x, size.z);
                    size = new Vector3(max, size.y, max);
                    return GetRotatedAABB(size);
                default:
                    return GetAABB(size);
            }
        }

        private AABB GetAABB(Vector3 size)
        {
            Vector3 center = transform.position;
            size = Vector3.one * size.x + Epsilon;
            Vector3 offset = Vector3.Scale(size, (settings.Offset - Vector3.one * 0.5f) * 2f);
            
            Vector3 min = center - size + offset;
            Vector3 max = center + size + offset;
            return new AABB(min, max);
        }

        private AABB GetRotatedAABB(Vector3 size)
        {
            size += Epsilon;
            Vector3 center = transform.position;
            Vector3 right = transform.right * size.x;
            Vector3 up = transform.up * size.y;
            Vector3 forward = transform.forward * size.z;

            Vector3 extent = new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z)
            );
            Vector3 offset = Vector3.Scale(extent, (settings.Offset - Vector3.one * 0.5f) * 2f);
            
            Vector3 min = center - extent + offset;
            Vector3 max = center + extent + offset;
            return new AABB(min, max);
        }
    }
}