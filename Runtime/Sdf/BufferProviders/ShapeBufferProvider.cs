using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public class ShapeBufferProvider<T> : IBufferProvider<ShapeProvider>
        where T : struct, IPopulateData<ShapeProvider>
    {
        public static int BufferId = Shader.PropertyToID("_ShapeBuffer");
        public static readonly int Stride = UnsafeUtility.SizeOf<T>();

        private T[] shapeData;

        public GraphicsBuffer Buffer { get; private set; }

        public bool IsInitialized => Buffer != null;

        public void InitializeBuffer(ref Material material, ShapeProvider[] data)
        {
            if (IsInitialized)
                ReleaseBuffer();

            int count = data.Length;
            shapeData = new T[count];

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Stride);
            material.SetBuffer(BufferId, Buffer);
        }

        public void SetData(ShapeProvider[] data)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < data.Length; i++)
            {
                ShapeProvider provider = data[i];
                if (!provider) continue;

                shapeData[i] = new T();
                shapeData[i].Populate(provider);
            }

            Buffer.SetData(shapeData);
        }

        public void ReleaseBuffer()
        {
            Buffer?.Release();
            shapeData = null;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ShapeData : IPopulateData<ShapeProvider>
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
        }
    }

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
