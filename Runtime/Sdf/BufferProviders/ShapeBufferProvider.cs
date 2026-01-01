using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public abstract class ShapeBufferProvider<T> : BufferProvider<ShapeProvider>
        where T : struct, IPopulateData<ShapeProvider>
    {
        public static int BufferId = Shader.PropertyToID("_ShapeBuffer");
        
        private ShapeProvider[] providers;
        private T[] shapeData;

        public override int DataCount => shapeData?.Length ?? 0;

        public override void InitializeBuffer(ref Material material, ShapeProvider[] dataProviders)
        {
            if (IsInitialized)
                ReleaseBuffer();
            providers = dataProviders;
            int count = providers.Length;

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<T>());
            material.SetBuffer(BufferId, Buffer);

            shapeData = new T[count];
            for (int i = 0; i < count; i++)
            {
                shapeData[i] = new T();
                shapeData[i].Populate(providers[i]);
            }
            Buffer.SetData(shapeData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            bool setData = false;
            for (int i = 0; i < providers.Length; i++)
            {
                ShapeProvider provider = providers[i];
                if (!provider || provider.gameObject.isStatic) continue;

                shapeData[i] = new T();
                shapeData[i].Populate(provider);
                setData = true;
            }
            if (setData)
                Buffer.SetData(shapeData);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            Buffer = null;
            shapeData = null;
        }
    }

    public class ShapeBufferProvider : ShapeBufferProvider<ShapeData> { }
    
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
}
