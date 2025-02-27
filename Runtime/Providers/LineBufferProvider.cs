using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class LineBufferProvider<T> : IBufferProvider where T : struct, ILineData
    {
        public static readonly int LineBufferId = Shader.PropertyToID("_LineBuffer");
        
        private RaymarchLineEntity[] lines;
        private T[] lineData;
        
        public bool IsInitialized => lineData != null;
        
        public GraphicsBuffer InitializeBuffer(RaymarchEntity[] entities, ref Material material)
        {
            lines = entities.OfType<RaymarchLineEntity>().ToArray();
            int lineCount = lines.Length;
            if (lineCount == 0) return null;

            lineData = new T[lineCount];
            GraphicsBuffer lineBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lineCount, Marshal.SizeOf<T>());
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
