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
    
    public class LineProvider : MonoBehaviour, IBoundsProvider
    {
        [Header("Line")]
        public Lines Line = Lines.Segment;
        public Vector2 Radius = Vector2.one * 0.1f;
        public float ExpandBounds;
        public bool UseLossyScale = true;
        public List<Transform> Points = new();
        
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend;
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
        
        public T GetBounds<T>() where T : struct, IBounds<T>
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
    
    public interface ILineProviderData
    {
        void InitializeData(LineProvider provider, int startIndex);
    }
    
    public interface IPointProviderData
    {
        void InitializeData(Transform point);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct LineData : ILineProviderData
    {
        public int Type;
        public Matrix4x4 Transform;
        public int Operation;
        public float Blend;
        public Vector4 Radius;
        public int PointStartIndex;
        public int PointsCount;
        public Vector4 Color;

        public void InitializeData(LineProvider line, int startIndex)
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
    public struct GradientLineData : ILineProviderData
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
        
        public void InitializeData(LineProvider line, int startIndex)
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
    public struct PointData : IPointProviderData
    {
        public Vector3 Position;

        public void InitializeData(Transform point)
        {
            Position = point.localPosition;
        }
    }
}
