using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class ShapeBufferProvider<T> : IBufferProvider where T : struct, IRaymarchEntityData
    {
        public static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");

        public bool IsInitialized => shapeData != null;

        private RaymarchShapeEntity[] raymarchShapes;
        private T[] shapeData;
        
        public GraphicsBuffer InitializeBuffer(RaymarchEntity[] entities, ref Material material)
        {
            raymarchShapes = entities.OfType<RaymarchShapeEntity>().ToArray();
            int count = raymarchShapes.Length;
            if (count == 0) return null;

            shapeData = new T[count];
            GraphicsBuffer shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<T>());
            material.SetBuffer(ShapeBufferId, shapeBuffer);
            return shapeBuffer;
        }

        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < raymarchShapes.Length; i++)
            {
                if (!raymarchShapes[i]) continue;

                shapeData[i] = new T();
                shapeData[i].InitializeData(raymarchShapes[i]);
            }
            buffer.SetData(shapeData);
        }

        public void ReleaseData()
        {
            shapeData = null;
            raymarchShapes = null;
        }

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            // empty
        }
#endif
    }
}
