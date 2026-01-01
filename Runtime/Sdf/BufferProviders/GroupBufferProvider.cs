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
            if (IsInitialized)
                ReleaseBuffer();
            groupProviders = dataProviders;
            int count = groupProviders.Length;

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
                if (group == null || !group.IsGroupDirty) continue;

                groupData[i] = new GroupData(group);
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
