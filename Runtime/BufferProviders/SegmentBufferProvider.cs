using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class SegmentBufferProvider<T> : IBufferProvider<LineObject.Segment> where T : struct, ISetupFromIndexed<LineObject.Segment>
    {
        public static readonly int SegmentBufferId = Shader.PropertyToID("_SegmentBuffer");
        
        private LineObject.Segment[] segments;
        private T[] segmentData;
        
        public bool IsInitialized => segmentData != null;

        public GraphicsBuffer InitializeBuffer(LineObject.Segment[] dataProviders, ref Material material)
        {
            segments = dataProviders;
            int count = segments.Length;
            if (count == 0) return null;

            segmentData = new T[count];
            GraphicsBuffer lineBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<T>());
            material.SetBuffer(SegmentBufferId, lineBuffer);
            return lineBuffer;
        }

        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;
            
            int pointIndex = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                segmentData[i] = new T();
                segmentData[i].SetupFrom(segments[i], pointIndex);
                pointIndex += segments[i].Points.Length - 1;
            }
            buffer.SetData(segmentData);
        }

        public void ReleaseData()
        {
            segmentData = null;
            segments = null;
        }
        
#if UNITY_EDITOR
        public void DrawGizmos() { }
#endif
    }
}
