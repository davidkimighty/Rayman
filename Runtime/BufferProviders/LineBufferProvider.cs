using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class LineBufferProvider<T> : IRaymarchBufferProvider where T : struct, ILineProviderData
    {
        public static readonly int LineBufferId = Shader.PropertyToID("_LineBuffer");
        
        private LineProvider[] lines;
        private T[] lineData;
        
        public bool IsInitialized => lineData != null;
        
        public GraphicsBuffer InitializeBuffer<T1>(T1[] dataProviders, ref Material material)
        {
            lines = dataProviders.OfType<LineProvider>().ToArray();
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
                lineData[i].InitializeData(lines[i], pointIndex);
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
