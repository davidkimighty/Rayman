using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class ColorBufferProvider : BufferProvider<VisualProvider>
    {
        public static readonly int BufferId = Shader.PropertyToID("_ColorBuffer");

        private ColorProvider[] providers;
        private ColorData[] colorData;

        public override void InitializeBuffer(ref Material material, VisualProvider[] dataProviders)
        {
            if (dataProviders == null || dataProviders.Length == 0) return;

            if (IsInitialized)
                ReleaseBuffer();

            providers = GetColorProviders(dataProviders);
            if (providers.Length == 0) return;

            int count = providers.Length;
            Buffer = new(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<ColorData>());
            material.SetBuffer(BufferId, Buffer);

            colorData = new ColorData[count];
            for (int i = 0; i < providers.Length; i++)
                colorData[i] = new ColorData(providers[i]);
            Buffer.SetData(colorData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            bool setData = false;
            for (int i = 0; i < providers.Length; i++)
            {
                ColorProvider provider = providers[i];
                if (!provider) continue;

                colorData[i] = new ColorData(provider);
                if (!provider.IsVisualDirty) continue;

                provider.IsVisualDirty = false;
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
}
