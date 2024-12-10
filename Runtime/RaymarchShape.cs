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
        Cone,
        CappedCone,
    }

    public enum Combinations
    {
        Union,
        Subtract,
        Intersect
    }

    public enum Operations
    {
        None,
        Twist,
        Bend,
    }

    public class RaymarchShape : MonoBehaviour, IBoundsSource
    {
        [Serializable]
        public class Operation
        {
            public Operations Type;
            public float Amount;

            public bool Enabled => Type != Operations.None;
        }
        
        [Serializable]
        public class Setting
        {
            public Shapes Type;
            public Vector3 Size;
            [Range(0, 1f)] public float Roundness;
            public Combinations Combination;
            public float Smoothness;
            public Color Color;
            [ColorUsage(true, true)] public Color EmissionColor;
            [Range(0, 1f)] public float EmissionIntensity;
            public Operation Operation;
        }

        [SerializeField] private Setting settings;

        public Setting Settings => settings;

        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (typeof(T) == typeof(AABB))
            {
                Vector3 size = settings.Size;
                AABB aabb = settings.Type switch
                {
                    Shapes.Sphere => GetAABB(size),
                    Shapes.Octahedron => GetAABB(size),
                    Shapes.Capsule => GetRotatedCapsuleAABB(size),
                    _ => GetRotatedAABB(size)
                };
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }

        private AABB GetAABB(Vector3 size)
        {
            size = Vector3.one * size.x;
            return new AABB
            {
                Min = transform.position - size,
                Max = transform.position + size
            };
        }

        private AABB GetRotatedAABB(Vector3 size)
        {
            Vector3 center = transform.position;
            Vector3 right = transform.right * size.x;
            Vector3 up = transform.up * size.y;
            Vector3 forward = transform.forward * size.z;

            Vector3 extent = new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z)
            );

            Vector3 min = center - extent;
            Vector3 max = center + extent;
            return new AABB(min, max);
        }
        
        private AABB GetRotatedCapsuleAABB(Vector3 size)
        {
            float radius = size.x;
            size.x *= 0.5f;
            Vector3 min = new Vector3(-size.x, 0, -size.x);
            Vector3 max = new Vector3(size.x, size.y / 2 + radius, size.x);

            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(min.x, min.y, min.z); // bottom back left
            corners[1] = new Vector3(min.x, min.y, max.z); // bottom back right
            corners[2] = new Vector3(max.x, min.y, min.z); // bottom front left
            corners[3] = new Vector3(max.x, min.y, max.z); // bottom front right
            
            corners[4] = new Vector3(min.x, max.y, min.z); // top back left
            corners[5] = new Vector3(min.x, max.y, max.z); // top back right
            corners[6] = new Vector3(max.x, max.y, min.z); // top front left
            corners[7] = new Vector3(max.x, max.y, max.z); // top front right

            Vector3 minWS = Vector3.positiveInfinity;
            Vector3 maxWS = Vector3.negativeInfinity;
            for (var i = 0; i < corners.Length; i++)
            {
                Vector3 cornerWS = transform.TransformPoint(corners[i]);
                minWS = Vector3.Min(minWS, cornerWS);
                maxWS = Vector3.Max(maxWS, cornerWS);
            }
            
            Vector3 rotatedSize = maxWS - minWS;
            if (rotatedSize.y < size.x * 2f)
                rotatedSize.y = size.x * 2;
            return new AABB
            {
                Min = transform.position - rotatedSize,
                Max = transform.position + rotatedSize
            };
        }
    }
}