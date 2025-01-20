using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Provider/Shape Data")]
    public class ShapeDataProvider : ShapeDataProvider<RaymarchShape, ShapeData>
    {
        protected override int GetStride() => ShapeData.Stride;

        protected override ShapeData CreateData(RaymarchShape shape)
        {
            return new ShapeData(shape);
        }
    }
    
    public abstract class ShapeDataProvider<T, U> : RaymarchDataProvider where T : RaymarchEntity where U : struct
    {
        private static readonly int ShapeBufferId = Shader.PropertyToID("_ShapeBuffer");

        private Dictionary<int, GroupData<T, U>> ShapeDataByGroup = new();

        public override void Setup(int groupId, RaymarchEntity[] entities, ref Material mat)
        {
            if (ShapeDataByGroup.TryGetValue(groupId, out GroupData<T, U> data))
                data.Buffer?.Release();

            T[] shapes = entities.OfType<T>().ToArray();
            int count = shapes.Length;
            if (count == 0) return;

            GroupData<T, U> groupData = new()
            {
                Entities = shapes,
                Data = new U[count],
                Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, GetStride())
            };
            ShapeDataByGroup[groupId] = groupData;
            mat.SetBuffer(ShapeBufferId, groupData.Buffer);
        }

        public override void SetData(int groupId)
        {
            if (!ShapeDataByGroup.TryGetValue(groupId, out GroupData<T, U> data)) return;

            for (int i = 0; i < data.Entities.Length; i++)
            {
                if (data.Entities[i] == null) continue;

                data.Data[i] = CreateData(data.Entities[i]);
            }
            data.Buffer.SetData(data.Data);
        }

        public override void Release(int groupId)
        {
            if (!ShapeDataByGroup.TryGetValue(groupId, out GroupData<T, U> data)) return;

            data.Buffer?.Release();
            ShapeDataByGroup.Remove(groupId);
        }

        protected abstract int GetStride();
        protected abstract U CreateData(T shape);
    }
    
    public class GroupData<T, U> where T : RaymarchEntity where U : struct
    {
        public T[] Entities;
        public U[] Data;
        public GraphicsBuffer Buffer;
    }
}
