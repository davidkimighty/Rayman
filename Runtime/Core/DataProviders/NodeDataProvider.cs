using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public abstract class NodeDataProvider<T, U> : RaymarchDataProvider  where T : struct, IBounds<T> where U : struct
    {
        protected static int NodeBufferId = Shader.PropertyToID("_NodeBuffer");
        
        [SerializeField] protected float updateBoundsThreshold;
        
        protected Dictionary<int, NodeGroupData<T, U>> nodeDataByGroup = new();
        
        public override void Setup(int groupId, RaymarchEntity[] entities, ref Material mat)
        {
            if (nodeDataByGroup.TryGetValue(groupId, out NodeGroupData<T, U> groupData))
                groupData.Buffer?.Release();

            BoundingVolume<T>[] volumes = entities.Select(e => new BoundingVolume<T>(e)).ToArray();
            ISpatialStructure<T> structure = CreateSpatialStructure(volumes);
            int nodeCount = structure.Count;
            if (nodeCount == 0) return;

            groupData = new NodeGroupData<T, U>
            {
                Structure = structure,
                Volumes = volumes,
                Data = new U[nodeCount],
                Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, GetNodeStride())
            };
            nodeDataByGroup[groupId] = groupData;
            mat.SetBuffer(NodeBufferId, groupData.Buffer);
        }

        public override void SetData(int groupId)
        {
            if (!nodeDataByGroup.TryGetValue(groupId, out NodeGroupData<T, U> groupData)) return;

            for (int i = 0; i < groupData.Volumes.Length; i++)
                groupData.Volumes[i].SyncVolume(ref groupData.Structure, updateBoundsThreshold);

            UpdateNodeData(groupData.Structure, ref groupData.Data);

            groupData.Buffer.SetData(groupData.Data);
        }

        public override void Release(int groupId)
        {
            if (!nodeDataByGroup.TryGetValue(groupId, out NodeGroupData<T, U> groupData)) return;

            groupData.Buffer?.Release();
            nodeDataByGroup.Remove(groupId);
        }

#if UNITY_EDITOR
        public override void DrawGizmos(int groupId)
        {
            if (!nodeDataByGroup.TryGetValue(groupId, out NodeGroupData<T, U> data)) return;
            
            data.Structure.DrawStructure();
        }
#endif
        
        protected abstract ISpatialStructure<T> CreateSpatialStructure(BoundingVolume<T>[] volumes);
        protected abstract int GetNodeStride();
        protected abstract void UpdateNodeData(ISpatialStructure<T> structure, ref U[] nodeData);
    }
    
    public class NodeGroupData<U, V> where U : struct, IBounds<U> where V : struct
    {
        public ISpatialStructure<U> Structure;
        public BoundingVolume<U>[] Volumes;
        public V[] Data;
        public GraphicsBuffer Buffer;
    }
}
