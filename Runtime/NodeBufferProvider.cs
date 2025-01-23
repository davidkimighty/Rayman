using System.Linq;
using UnityEngine;

namespace Rayman
{
    public abstract class NodeBufferProvider<T, U> : RaymarchBufferProvider  where T : struct, IBounds<T> where U : struct
    {
        protected static int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        
        [SerializeField] protected float updateBoundsThreshold;
        
        protected ISpatialStructure<T> spatialStructure;
        protected BoundingVolume<T>[] boundingVolumes;
        protected U[] nodeData;
        
        public override void SetupBuffer(RaymarchEntity[] entities, ref Material mat)
        {
            buffer?.Release();

            boundingVolumes = entities.Select(e => new BoundingVolume<T>(e)).ToArray();
            spatialStructure = CreateSpatialStructure(boundingVolumes);
            int nodeCount = spatialStructure.Count;
            if (nodeCount == 0) return;

            nodeData = new U[nodeCount];
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, GetNodeStride());
            mat.SetBuffer(NodeBufferId, buffer);
            InvokeOnSetup();
        }

        public override void UpdateData()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < boundingVolumes.Length; i++)
                boundingVolumes[i].SyncVolume(ref spatialStructure, updateBoundsThreshold);

            UpdateNodeData(spatialStructure, ref nodeData);
            buffer.SetData(nodeData);
        }

        public override void ReleaseBuffer()
        {
            buffer?.Release();
            spatialStructure = null;
            boundingVolumes = null;
            nodeData = null;
            InvokeOnRelease();
        }

#if UNITY_EDITOR
        public override void DrawGizmos()
        {
            if (!IsInitialized) return;
            
            spatialStructure.DrawStructure();
        }
#endif
        
        protected abstract ISpatialStructure<T> CreateSpatialStructure(BoundingVolume<T>[] volumes);
        protected abstract int GetNodeStride();
        protected abstract void UpdateNodeData(ISpatialStructure<T> structure, ref U[] data);
    }
}
