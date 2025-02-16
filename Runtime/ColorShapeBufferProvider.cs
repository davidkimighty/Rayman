using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class ColorShapeBufferProvider : IBufferProvider
    {
        protected static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");

        public bool IsInitialized => shapeData != null;

        protected ColorShape[] colorShapes;
        protected ColorShapeData[] shapeData;
        protected GraphicsBuffer shapeBuffer;
        
        public void SetupBuffer(RaymarchEntity[] entities, ref Material mat)
        {
            colorShapes = entities.OfType<ColorShape>().ToArray();
            int count = colorShapes.Length;
            if (count == 0) return;

            shapeBuffer?.Release(); 
            shapeData = new ColorShapeData[count];
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ColorShapeData.Stride);
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        public void UpdateBufferData()
        {
            if (!IsInitialized) return;
            
            for (int i = 0; i < colorShapes.Length; i++)
            {
                if (!colorShapes[i]) continue;

                shapeData[i] = new ColorShapeData(colorShapes[i]);
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
