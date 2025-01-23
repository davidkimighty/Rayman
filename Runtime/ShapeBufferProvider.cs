using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class ShapeBufferProvider : ShapeBufferProvider<RaymarchShape, ShapeData>, IRaymarchDebugProvider
    {
        protected override int GetStride() => ShapeData.Stride;

        protected override ShapeData CreateData(RaymarchShape shape)
        {
            return new ShapeData(shape);
        }

        public int GetDebugInfo()
        {
            return shapes.Length;
        }
    }
    
    public abstract class ShapeBufferProvider<T, U> : RaymarchBufferProvider where T : RaymarchEntity where U : struct
    {
        protected static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");

        protected T[] shapes;
        protected U[] shapeData;

        public override void SetupBuffer(RaymarchEntity[] entities, ref Material mat)
        {
            buffer?.Release();
            shapes = entities.OfType<T>().ToArray();
            int count = shapes.Length;
            if (count == 0) return;

            shapeData = new U[count];
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, GetStride());
            mat.SetBuffer(ShapeBufferId, buffer);
            InvokeOnSetup();
        }

        public override void UpdateData()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < shapes.Length; i++)
            {
                if (!shapes[i]) continue;

                shapeData[i] = CreateData(shapes[i]);
            }
            buffer.SetData(shapeData);
        }

        public override void ReleaseBuffer()
        {
            buffer?.Release();
            shapes = null;
            shapeData = null;
            InvokeOnRelease();
        }

        protected abstract int GetStride();
        protected abstract U CreateData(T shape);
    }
}
