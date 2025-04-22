using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class LineBufferProvider<T> : IBufferProvider<LineProvider> where T : struct, ISetupFromIndexed<LineProvider>
    {
        public static readonly int LineBufferId = Shader.PropertyToID("_LineBuffer");
        
        private LineProvider[] lines;
        private T[] lineData;
        
        public bool IsInitialized => lineData != null;

        public GraphicsBuffer InitializeBuffer(LineProvider[] dataProviders, ref Material material)
        {
            lines = dataProviders;
            int count = lines.Length;
            if (count == 0) return null;

            lineData = new T[count];
            GraphicsBuffer lineBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<T>());
            material.SetBuffer(LineBufferId, lineBuffer);
            return lineBuffer;
        }

        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;
            
            int pointIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i]) continue;

                lineData[i] = new T();
                lineData[i].SetupFrom(lines[i], pointIndex);
                pointIndex += lines[i].Points.Count;
            }
            buffer.SetData(lineData);
        }

        public void ReleaseData()
        {
            lineData = null;
            lines = null;
        }
        
#if UNITY_EDITOR
        public void DrawGizmos() { }
#endif
    }
}
