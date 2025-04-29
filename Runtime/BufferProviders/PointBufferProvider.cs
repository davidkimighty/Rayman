using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class PointBufferProvider<T> : IBufferProvider<LineObject.Segment> where T : struct, ISetupFrom<Transform>
    {
        public static readonly int PointBufferId = Shader.PropertyToID("_PointBuffer");
        
        private LineObject.Segment[] segments;
        private T[] pointData;
        
        public bool IsInitialized => pointData != null;
        
        public GraphicsBuffer InitializeBuffer(LineObject.Segment[] dataProviders, ref Material material)
        {
            segments = dataProviders;
            int segmentCount = segments.Length;
            if (segmentCount == 0) return null;

            int pointCount = (segments[0].Points.Length - 1) * segments.Length + 1;
            pointData = new T[pointCount];
            GraphicsBuffer pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, Marshal.SizeOf<T>());
            material.SetBuffer(PointBufferId, pointBuffer);
            return pointBuffer;
        }

        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;
            
            int pointIndex = 0;
            if (segments[0] != null)
            {
                foreach (Transform point in segments[0].Points)
                {
                    pointData[pointIndex] = new T();
                    pointData[pointIndex++].SetupFrom(point);
                }
            }
            
            for (int i = 1; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                for (int j = 1; j < segments[i].Points.Length; j++)
                {
                    pointData[pointIndex] = new T();
                    pointData[pointIndex++].SetupFrom(segments[i].Points[j]);
                }
            }
            buffer.SetData(pointData);
        }

        public void ReleaseData()
        {
            pointData = null;
            segments = null;
        }
        
#if UNITY_EDITOR
        public void DrawGizmos() { }
#endif
    }
}
