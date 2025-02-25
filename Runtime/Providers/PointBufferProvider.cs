using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class PointBufferProvider<T> : IBufferProvider where T : struct, IPointData
    {
        public static readonly int PointBufferId = Shader.PropertyToID("_PointBuffer");
        
        private RaymarchLineEntity[] lines;
        private T[] pointData;
        
        public bool IsInitialized => pointData != null;
        
        public GraphicsBuffer InitializeBuffer(RaymarchEntity[] entities, ref Material material)
        {
            lines = entities.OfType<RaymarchLineEntity>().ToArray();
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
                    pointData[pointIndex].InitializeData(point);
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
    }
}
