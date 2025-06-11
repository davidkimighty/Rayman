using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class BvhNodeBufferProvider<T, U> : INodeBufferProvider
        where T : struct, IBounds<T>
        where U : struct, ISetupFromIndexed<SpatialNode<T>>
    {
        public static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");

        private Bvh<T> spatialStructure;
        private T[] activeBounds;
        private U[] nodeData;
        
        public bool IsInitialized => spatialStructure != null && nodeData != null;
        public ISpatialStructure SpatialStructure => spatialStructure;
        
        public GraphicsBuffer InitializeBuffer(IBoundsProvider[] providers, ref Material material)
        {
            activeBounds = providers.GetBounds<T>();
            spatialStructure = new Bvh<T>();
            
            for (int i = 0; i < activeBounds.Length; i++)
                spatialStructure.AddLeafNode(i, activeBounds[i]);
            
            int count = spatialStructure.Count;
            if (count == 0) return null;
            
            nodeData = new U[count];
            GraphicsBuffer nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<U>());
            material.SetBuffer(NodeBufferId, nodeBuffer);
            return nodeBuffer;
        }
        
        public void SyncBounds(IBoundsProvider[] providers, float syncThreshold = 0)
        {
            T[] bounds = providers.GetBounds<T>();
            for (int i = 0; i < bounds.Length; i++)
            {
                T buffBounds = activeBounds[i].Expand(syncThreshold);
                if (buffBounds.Contains(bounds[i])) continue;

                activeBounds[i] = bounds[i];
                spatialStructure.UpdateBounds(i, bounds[i]);
            }
        }
        
        public void SetData(ref GraphicsBuffer buffer)
        {
            if (!IsInitialized) return;

            UpdateNodeData();
            buffer.SetData(nodeData);
        }

        public void ReleaseData()
        {
            spatialStructure = null;
            nodeData = null;
        }
        
#if UNITY_EDITOR
        public void DrawGizmos()
        {
            spatialStructure?.DrawStructure();
        }
#endif
        
        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<T> node, int parentIndex)> queue = new();
            queue.Enqueue((spatialStructure.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<T> current, int parentIndex) = queue.Dequeue();
                U data = new();
                data.SetupFrom(current, -1);

                if (current.LeftChild != null)
                {
                    data.Index = index + queue.Count + 1; // setting child index
                    queue.Enqueue((current.LeftChild, index));
                }
                if (current.RightChild != null)
                    queue.Enqueue((current.RightChild, index));
                
                nodeData[index] = data;
                index++;
            }
        }
    }
    
    
}
