using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public enum Lines
    {
        Segment,
        QuadraticBezier,
        CubicBezier,
    }
    
    public class LineElement : RaymarchElement
    {
        [Header("Line")]
        public Lines Line = Lines.Segment;
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend;
        public Vector2 Radius = Vector2.one * 0.1f;
        public List<Transform> Points = new();
        public Color Color = Color.white;
        public Color GradientColor = Color.white;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Radius = new Vector2(
                Mathf.Max(Radius.x, 0),
                Mathf.Max(Radius.y, 0));
        }
#endif
        
        public override T GetBounds<T>()
        {
            if (typeof(T) == typeof(Aabb))
            {
                Aabb aabb = new(transform.position, transform.position);
                foreach (Transform point in Points)
                    aabb = aabb.Include(point.position);
                aabb = aabb.Expand(Mathf.Max(Radius.x, Radius.y) + Blend + ExpandBounds + 0.001f);
                return (T)(object)aabb;
            }
            throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
        }
    }
    
    public interface ILineData
    {
        void InitializeData(LineElement line, int startIndex);
    }
    
    public interface IPointData
    {
        void InitializeData(Transform point);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct LineData : ILineData
    {
        public int Type;
        public Matrix4x4 Transform;
        public int Operation;
        public float Blend;
        public Vector4 Radius;
        public int PointStartIndex;
        public int PointsCount;
        public Vector4 Color;

        public void InitializeData(LineElement line, int startIndex)
        {
            Type = (int)line.Line;
            Transform = line.UseLossyScale ? line.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(line.transform.position, line.transform.rotation, Vector3.one).inverse;
            Operation = (int)line.Operation;
            Blend = line.Blend;
            Radius = line.Radius;
            PointStartIndex = startIndex;
            PointsCount = line.Points.Count;
            Color = line.Color;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct GradientLineData : ILineData
    {
        public int Type;
        public Matrix4x4 Transform;
        public int Operation;
        public float Blend;
        public Vector4 Radius;
        public int PointStartIndex;
        public int PointsCount;
        public Vector4 Color;
        public Vector4 GradientColor;
        
        public void InitializeData(LineElement line, int startIndex)
        {
            Type = (int)line.Line;
            Transform = line.UseLossyScale ? line.transform.worldToLocalMatrix : 
                Matrix4x4.TRS(line.transform.position, line.transform.rotation, Vector3.one).inverse;
            Operation = (int)line.Operation;
            Blend = line.Blend;
            Radius = line.Radius;
            PointStartIndex = startIndex;
            PointsCount = line.Points.Count;
            Color = line.Color;
            GradientColor = line.GradientColor;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct PointData : IPointData
    {
        public Vector3 Position;

        public void InitializeData(Transform point)
        {
            Position = point.localPosition;
        }
    }
}
