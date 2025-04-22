using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class ShapeGroupBufferProvider<T> : IBufferProvider<ShapeGroupProvider>
        where T : struct, ISetupFromIndexed<ShapeGroupProvider>
    {
        public static readonly int ShapeGroupBufferId = Shader.PropertyToID("_ShapeGroupBuffer");
        
        private ShapeGroupProvider[] groups;
        private T[] groupData;

        public bool IsInitialized => groupData != null;
        
        public GraphicsBuffer InitializeBuffer(ShapeGroupProvider[] dataProviders, ref Material material)
        {
            groups = dataProviders;
            int count = groups.Length;
            if (count == 0) return null;

            groupData = new T[count];
            GraphicsBuffer groupBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<T>());
            material.SetBuffer(ShapeGroupBufferId, groupBuffer);
            return groupBuffer;
        }

        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;
            
            int pointIndex = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                if (!groups[i]) continue;

                groupData[i] = new T();
                groupData[i].SetupFrom(groups[i], pointIndex);
                pointIndex += groups[i].Shapes.Count;
            }
            buffer.SetData(groupData);
        }

        public void ReleaseData()
        {
            groupData = null;
            groups = null;
        }

#if UNITY_EDITOR
        public void DrawGizmos() { }
#endif
    }
}