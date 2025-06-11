using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    [Serializable]
    public class ShapeGroupProvider : MonoBehaviour, IBoundsProvider
    {
        public Operations Operation = Operations.Union;
        [Range(0, 1f)] public float Blend = 0.01f;
        public List<ShapeProvider> Shapes = new();

        public bool IsActive => HasActiveShape();
        
        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            if (Shapes.Count == 0) return default;

            T bounds = Shapes[0].GetBounds<T>();
            for (var i = 1; i < Shapes.Count; i++)
                bounds = bounds.Union(Shapes[i].GetBounds<T>());
            return bounds;
        }
        
        private bool HasActiveShape()
        {
            for (int i = 0; i < Shapes.Count; i++)
            {
                if (Shapes[i] != null && Shapes[i].gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeGroupData : ISetupFromIndexed<ShapeGroupProvider>
    {
        public int Operation;
        public float Blend;
        public int StartIndex;
        public int Count;
        
        public int Index
        {
            get => StartIndex;
            set => StartIndex = value;
        }
        
        public void SetupFrom(ShapeGroupProvider data, int index)
        {
            Operation = (int)data.Operation;
            Blend = data.Blend;
            StartIndex = index;
            Count = data.Shapes.Count;
        }
    }
}