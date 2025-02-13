using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
#if UNITY_EDITOR
    public enum DebugModes { None, Color, Normal, Hitmap, BoundingVolume, }
#endif
    
    [ExecuteInEditMode]
    public class ColorShapeGroup : RaymarchGroup
    {
        protected static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        protected static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        protected static readonly int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        protected static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");
        
        [SerializeField] protected List<SingleColorShape> entities = new();
        [SerializeField] protected float updateBoundsThreshold;
        
        [Header("PBR")]
        [Range(0f, 1f), SerializeField] protected float metallic;
        [Range(0f, 1f), SerializeField] protected float smoothness = 0.5f;
#if UNITY_EDITOR
        [Header("Dubug Mode")]
        [SerializeField] protected DebugModes debugMode = DebugModes.Hitmap;
        [SerializeField] protected int boundsDisplayThreshold = 300;
#endif

        protected Material matInstance;
        protected SingleColorShape[] activeEntities;
        protected ISpatialStructure<Aabb> bvh;
        protected BoundingVolume<Aabb>[] boundingVolumes;
        
        protected NodeDataAabb[] nodeData;
        protected GraphicsBuffer nodeBuffer;
        protected ShapeColorData[] shapeData;
        protected GraphicsBuffer shapeBuffer;
        
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
            if (!IsInitialized()) return;

            if (matInstance)
            {
                SetupShaderProperties(ref matInstance);                
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            bvh.DrawStructure();
        }
#endif
        
        public override bool IsInitialized() => matInstance && activeEntities != null &&
                                                bvh != null && boundingVolumes != null;

        public override Material InitializeGroup()
        {
            activeEntities = entities.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return null;

            matInstance = new Material(shader);
            if (!matInstance) return null;
            
            SetupShaderProperties(ref matInstance);

            SetupNodeBuffer(ref matInstance);
            SetupShapeBuffer(ref matInstance);
            
            return matInstance;
        }

        public override void ReleaseGroup()
        {
            if (Application.isEditor)
                DestroyImmediate(matInstance);
            else
                Destroy(matInstance);
            activeEntities = null;
            ReleaseNodeBuffer();
            ReleaseShapeBuffer();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All Shapes")]
        public void FindAllShapes()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<SingleColorShape>(transform);
        }
#endif

        protected virtual void SetupShaderProperties(ref Material mat)
        {
            mat.SetFloat(MetallicId, metallic);
            mat.SetFloat(SmoothnessId, smoothness);
#if UNITY_EDITOR
            if (debugMode != DebugModes.None)
            {
                mat.EnableKeyword("DEBUG_MODE");
                mat.SetInt("_DebugMode", (int)debugMode);
                mat.SetInt("_BoundsDisplayThreshold", boundsDisplayThreshold);
            }
#endif
        }
        
        private void UpdateNodeData()
        {
            int index = 0;
            Queue<(SpatialNode<Aabb> node, int parentIndex)> queue = new();
            queue.Enqueue((bvh.Root, -1));

            while (queue.Count > 0)
            {
                (SpatialNode<Aabb> current, int parentIndex) = queue.Dequeue();
                NodeDataAabb node = new()
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
            for (int i = 0; i < activeEntities.Length; i++)
            {
                if (!activeEntities[i]) continue;

                shapeData[i] = new ShapeColorData(activeEntities[i]);
            }
        }
        
        private void SetupNodeBuffer(ref Material mat)
        {
            nodeBuffer?.Release();

            boundingVolumes = activeEntities.Select(e => new BoundingVolume<Aabb>(e)).ToArray();
            bvh = Bvh<Aabb>.Create(boundingVolumes);
            int nodeCount = bvh.Count;
            if (nodeCount == 0) return;

            nodeData = new NodeDataAabb[nodeCount];
            nodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, NodeDataAabb.Stride);
            mat.SetBuffer(NodeBufferId, nodeBuffer);
        }

        private void SetupShapeBuffer(ref Material mat)
        {
            shapeBuffer?.Release();
            int count = activeEntities.Length;
            if (count == 0) return;

            shapeData = new ShapeColorData[count];
            shapeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, ShapeColorData.Stride);
            mat.SetBuffer(ShapeBufferId, shapeBuffer);
        }

        private void ReleaseNodeBuffer()
        {
            bvh = null;
            boundingVolumes = null;
            nodeBuffer?.Release();
            nodeData = null;
        }

        private void ReleaseShapeBuffer()
        {
            shapeBuffer?.Release();
            shapeData = null;
        }
    }
}
