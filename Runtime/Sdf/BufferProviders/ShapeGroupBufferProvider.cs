using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class ShapeGroupBufferProvider : ShapeBufferProvider<ShapeGroupData> { }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeGroupData : IPopulateData<ShapeProvider>
    {
        public float3 Position;
        public Quaternion Rotation;
        public float3 Scale;
        public float3 Size;
        public float3 Pivot;
        public int Operation;
        public float Blend;
        public float Roundness;
        public int ShapeType;
        public int GroupIndex;

        public void Populate(ShapeProvider provider)
        {
            Position = provider.transform.position;
            Rotation = Quaternion.Inverse(provider.transform.rotation);
            Scale = provider.GetScale();
            Size = provider.Size;
            Pivot = provider.Pivot;
            Operation = (int)provider.Operation;
            Blend = provider.Blend;
            Roundness = provider.Roundness;
            ShapeType = (int)provider.Shape;
            GroupIndex = provider.GroupIndex;
        }
    }
}
