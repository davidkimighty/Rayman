using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public class GroupBufferProvider : BufferProvider<IRaymarchGroup>
    {
        public static readonly int BufferId = Shader.PropertyToID("_GroupBuffer");
        
        private IRaymarchGroup[] groupProviders;
        private GroupData[] groupData;

        public override void InitializeBuffer(ref Material material, IRaymarchGroup[] dataProviders)
        {
            if (dataProviders == null) return;

            int count = dataProviders.Length;
            if (count == 0) return;

            if (IsInitialized)
                ReleaseBuffer();
            groupProviders = dataProviders;

            Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<GroupData>());
            material.SetBuffer(BufferId, Buffer);

            groupData = new GroupData[count];
            for (int i = 0; i < groupProviders.Length; i++)
                groupData[i] = new GroupData(groupProviders[i]);
            Buffer.SetData(groupData);
        }

        public override void SetData()
        {
            if (!IsInitialized) return;

            bool setData = false;
            for (int i = 0; i < groupProviders.Length; i++)
            {
                IRaymarchGroup group = groupProviders[i];
                if (group == null) continue;

                groupData[i] = new GroupData(group);
                if (!group.IsGroupDirty) continue;

                group.IsGroupDirty = false;
                setData = true;
            }
            if (setData)
                Buffer.SetData(groupData);
        }

        public override void ReleaseBuffer()
        {
            Buffer?.Release();
            Buffer = null;
        }
    }
}
