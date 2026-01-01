using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    public abstract class ColorBufferProvider<T> : BufferProvider<VisualProvider>
        where T : struct, IPopulateData<ColorProvider>
    {
        public static int BufferId = Shader.PropertyToID("_ColorBuffer");
        
        private ColorProvider[] providers;
        private T[] colorData;

        public override void InitializeBuffer(ref Material material, VisualProvider[] dataProviders)
        {
            if (IsInitialized)
                ReleaseBuffer();

            providers = GetColorProviders(dataProviders);
            int count = providers.Length;
            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<T>());
            material.SetBuffer(BufferId, Buffer);

            colorData = new T[count];
            for (int i = 0; i < providers.Length; i++)
            {
                colorData[i] = new T();
                colorData[i].Populate(providers[i]);
            }
            Buffer.SetData(colorData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            bool setData = false;
            for (int i = 0; i < providers.Length; i++)
            {
                ColorProvider provider = providers[i];
                if (!provider || !provider.IsDirty) continue;

                colorData[i] = new T();
                colorData[i].Populate(providers[i]);

                provider.IsDirty = false;
                setData = true;
            }
            if (setData)
                Buffer.SetData(colorData);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            colorData = null;
        }
        
        private ColorProvider[] GetColorProviders(VisualProvider[] dataProviders)
        {
            List<ColorProvider> valids = new();
            for (int i = 0; i < dataProviders.Length; i++)
            {
                if (dataProviders[i] is ColorProvider colorProvider)
                    valids.Add(colorProvider);
            }
            return valids.ToArray();
        }
    }
    
    public class ColorBufferProvider : ColorBufferProvider<ColorData>
    {
        public override void InitializeBuffer(ref Material material, VisualProvider[] dataProviders)
        {
            material.DisableKeyword("_GRADIENT_COLOR");
            base.InitializeBuffer(ref material, dataProviders);
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorData : IPopulateData<ColorProvider>
    {
        public float4 Color;

        public void Populate(ColorProvider provider)
        {
            Color = (Vector4)provider.Color;
        }
    }
}
