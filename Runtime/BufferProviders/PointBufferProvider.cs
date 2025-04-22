using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class PointBufferProvider<T> : IBufferProvider<LineProvider> where T : struct, ISetupFrom<Transform>
    {
        public static readonly int PointBufferId = Shader.PropertyToID("_PointBuffer");
        
        private LineProvider[] lines;
        private T[] pointData;
        
        public bool IsInitialized => pointData != null;
        
        public GraphicsBuffer InitializeBuffer(LineProvider[] dataProviders, ref Material material)
        {
            lines = dataProviders;
            int lineCount = lines.Length;
            if (lineCount == 0) return null;

            int pointCount = lines.Sum(l => l.Points.Count);
            pointData = new T[pointCount];
            GraphicsBuffer pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, Marshal.SizeOf<T>());
            material.SetBuffer(PointBufferId, pointBuffer);
            return pointBuffer;
        }

        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;
            
            int pointIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i]) continue;

                foreach (Transform point in lines[i].Points)
                {
                    pointData[pointIndex] = new T();
                    pointData[pointIndex].SetupFrom(point);
                    pointIndex++;
                }
            }
            buffer.SetData(pointData);
        }

        public void ReleaseData()
        {
            pointData = null;
            lines = null;
        }
        
#if UNITY_EDITOR
        public void DrawGizmos() { }
#endif
    }
}
