using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class ShapeBufferProvider : BufferProvider<ShapeProvider>
    {
        public static readonly int BufferId = Shader.PropertyToID("_ShapeBuffer");

        private ShapeProvider[] providers;
        private ShapeData[] shapeData;

        public override void InitializeBuffer(ref Material material, ShapeProvider[] dataProviders)
        {
            if (dataProviders == null) return;

            int count = dataProviders.Length;
            if (count == 0) return;

            if (IsInitialized)
                ReleaseBuffer();
            providers = dataProviders;

            Buffer = new(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<ShapeData>());
            material.SetBuffer(BufferId, Buffer);

            shapeData = new ShapeData[count];
            for (int i = 0; i < count; i++)
                shapeData[i] = new ShapeData(providers[i]);
            Buffer.SetData(shapeData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            bool setData = false;
            for (int i = 0; i < providers.Length; i++)
            {
                ShapeProvider provider = providers[i];
                if (!provider) continue;

                shapeData[i] = new ShapeData(provider);
                if (provider.gameObject.isStatic) continue;

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
}
