using System.Linq;
using UnityEngine;

namespace Rayman
{
#if UNITY_EDITOR
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
    public class ShapeDataProvider : RaymarchBufferProvider
    {
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        
        [Header("Shape Group")]
        
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] private bool executeInEditor;
        [SerializeField] private bool drawGizmos;
#endif
        private GraphicsBuffer shapeBuffer;
        private ShapeData[] shapeData;
        private RaymarchShape[] shapes;
        
        public bool IsInitialized => shapeData != null;
        
        public override void Setup(ref Material mat, RaymarchEntity[] entities)
        {
            shapes = entities.OfType<RaymarchShape>().ToArray();
            int count = shapes.Length;
            if (count == 0) return;
            
            shapeData = new ShapeData[count];
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ShapeData.Stride);
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        public override void SetData()
        {
            for (int i = 0; i < shapes.Length; i++)
            {
                if (shapes[i] == null) continue;

                shapeData[i] = new ShapeData(shapes[i]);
            }
            shapeBuffer.SetData(shapeData);
        }

        public override void Release()
        {
            shapeBuffer?.Release();
            shapeData = null;
            shapes = null;
        }
        
#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            if (!executeInEditor)
            {
                Gizmos.color = new Color(1, 1, 1, 0.3f);
                Gizmos.DrawSphere(transform.position, 0.1f);
            }
        }
#endif
    }
}
