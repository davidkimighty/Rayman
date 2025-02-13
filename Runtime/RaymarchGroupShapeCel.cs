using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchGroupShapeCel : RaymarchGroup, ISpatialStructureDebugProvider, IRaymarchDebugProvider
    {
        private static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        
        [SerializeField] private float updateBoundsThreshold;
        
        private ISpatialStructure<AABB> bvh;
        private BoundingVolume<AABB>[] boundingVolumes;
        private NodeDataAABB[] nodeData;
        private GraphicsBuffer nodeBuffer;
        
        private RaymarchShapeColor[] shapes;
        private ShapeColorData[] shapeData;
        private GraphicsBuffer shapeBuffer;
        
        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            for (int i = 0; i < boundingVolumes.Length; i++)
                boundingVolumes[i].SyncVolume(ref bvh, updateBoundsThreshold);
            
            UpdateNodeData();
            nodeBuffer.SetData(nodeData);
            
            UpdateShapeData();
            shapeBuffer.SetData(shapeData);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            bvh.DrawStructure();
        }
#endif
        
        public override Material InitializeGroup()
        {
            activeEntities = entities.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return null;

            Material matInstance = shader ? CoreUtils.CreateEngineMaterial(shader) :
                CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Lit");
            if (!matInstance) return null;
            
            SetupShaderProperties(ref matInstance);

            SetupNodeBuffer(ref matInstance);
            SetupShapeBuffer(ref matInstance);
            
            return matInstance;
        }

        public override void ReleaseGroup()
        {
            ReleaseNodeBuffer();
            ReleaseShapeBuffer();
            activeEntities = null;
        }

        public (int nodeCount, int maxHeight) GetDebugInfo()
        {
            return (bvh.Count, bvh.MaxHeight);
        }
        
        int IRaymarchDebugProvider.GetDebugInfo()
        {
            throw new System.NotImplementedException();
        }
        
        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<AABB> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<AABB> current, int parentIndex) = queue.Dequeue();
                NodeDataAABB node = new()
                {
                    Id = current.Id,
                    ChildIndex = -1,
                    Bounds = current.Bounds,
                };

                if (current.LeftChild != null)
                {
                    node.ChildIndex = index + queue.Count + 1;
                    queue.Enqueue((current.LeftChild, index));
                }
                if (current.RightChild != null)
                    queue.Enqueue((current.RightChild, index));
                
                nodeData[index] = node;
                index++;
            }
        }

        private void UpdateShapeData()
        {
            for (int i = 0; i < shapes.Length; i++)
            {
                if (!shapes[i]) continue;

                shapeData[i] = new ShapeColorData(shapes[i]);
            }
        }

        private void SetupShaderProperties(ref Material mat)
        {
            
        }
        
        private void SetupNodeBuffer(ref Material mat)
        {
            nodeBuffer?.Release();

            boundingVolumes = entities.Select(e => new BoundingVolume<AABB>(e)).ToArray();
            bvh = Bvh<AABB>.Create(boundingVolumes);
            int nodeCount = bvh.Count;
            if (nodeCount == 0) return;

            nodeData = new NodeDataAABB[nodeCount];
            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, NodeDataAABB.Stride);
            mat.SetBuffer(NodeBufferId, nodeBuffer);
        }

        private void SetupShapeBuffer(ref Material mat)
        {
            shapeBuffer?.Release();
            shapes = entities.OfType<RaymarchShapeColor>().ToArray();
            int count = shapes.Length;
            if (count == 0) return;

            shapeData = new ShapeColorData[count];
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ShapeColorData.Stride);
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        private void ReleaseNodeBuffer()
        {
            nodeBuffer?.Release();
            bvh = null;
            boundingVolumes = null;
            nodeData = null;
        }

        private void ReleaseShapeBuffer()
        {
            shapeBuffer?.Release();
            shapes = null;
            shapeData = null;
        }
    }
}
