using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    [Serializable]
    public class Spline
    {
        public event Action<Spline, int> OnChange;
        
        public int KnotStartIndex;
        
        [SerializeField] private List<KnotProvider> knots;

        private bool isDirty = true;
        
        public List<KnotProvider> Knots => knots;
        
        public KnotProvider this[int index]
        {
            get => knots[index];
            set => SetKnot(index, value);
        }

        public void SetKnot(int index, KnotProvider value)
        {
            knots[index] = value;
            isDirty = true;
            OnChange?.Invoke(this, index);
        }

        public List<Segment> GetSegments()
        {
            List<Segment> segments = new();
            for (int i = 0; i < knots.Count - 1; i++)
                segments.Add(new Segment(knots[i], knots[i + 1]));
            return segments;
        }
    }
    
    [Serializable]
    public class Segment : IBoundsProvider
    {
        public static readonly Dictionary<Type, Delegate> BoundsFactory = new()
        {
            { typeof(Aabb), (Func<float3, float3, float3, float3, Aabb>)CreateAabb }
        };
        
        private KnotProvider k1;
        private KnotProvider k2;
        
        public Segment(KnotProvider k1, KnotProvider k2)
        {
            this.k1 = k1;
            this.k2 = k2;
        }
        
        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (!BoundsFactory.TryGetValue(typeof(T), out Delegate del))
                throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
            var factory = del as Func<float3, float3, float3, float3, T>;
        
            float3 p0 = k1.transform.position;
            float3 p1 = p0 + (float3)k1.TangentOut;
            float3 p3 = k2.transform.position;
            float3 p2 = p3 + (float3)k2.TangentIn;
            T bounds = factory(p0, p1, p2, p3);

            float radius = Mathf.Max(k1.Radius, k2.Radius);
            bounds = bounds.Expand(radius);
            return bounds;
        }
        
        private static Aabb CreateAabb(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            float3 c = -p0 + p1;
            float3 b = p0 - 2f * p1 + p2;
            float3 a = -p0 + 3f * p1 - 3f * p2 + p3;
            float3 g = math.sqrt(math.max(math.mul(b, b) - math.mul(a, c), 0f));

            float3 t1 = math.saturate((-b - g) / a);
            float3 t2 = math.saturate((-b + g) / a);

            float3 q1 = p0 + t1 * (3f * c + t1 * (3f * b + t1 * a));
            float3 q2 = p0 + t2 * (3f * c + t2 * (3f * b + t2 * a));

            float3 min = math.min(math.min(p0, p3), math.min(q1, q2));
            float3 max = math.max(math.max(p0, p3), math.max(q1, q2));
            return new Aabb { Min = min, Max = max };
        }
    }
}