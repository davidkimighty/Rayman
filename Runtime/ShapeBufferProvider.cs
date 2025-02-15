using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class ShapeBufferProvider : IBufferProvider
    {
        protected static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");

        public bool IsInitialized => shapeData != null;

        protected RaymarchShape[] raymarchShapes;
        protected ShapeData[] shapeData;
        protected GraphicsBuffer shapeBuffer;
        
        public void SetupBuffer(RaymarchEntity[] entities, ref Material mat)
        {
            raymarchShapes = entities.OfType<RaymarchShape>().ToArray();
            int count = raymarchShapes.Length;
            if (count == 0) return;

            shapeBuffer?.Release();
            shapeData = new ShapeData[count];
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ShapeData.Stride);
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        public void UpdateBufferData()
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < raymarchShapes.Length; i++)
            {
                if (!raymarchShapes[i]) continue;

                shapeData[i] = new ShapeData(raymarchShapes[i]);
            }
            shapeBuffer.SetData(shapeData);
        }

        public void ReleaseBuffer()
        {
            shapeBuffer?.Release();
            shapeData = null;
        }

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            // empty
        }
#endif
    }
}
