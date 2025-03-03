using UnityEngine;

namespace Rayman
{
    public interface IBufferProvider
    {
        bool IsInitialized { get; }
        
        GraphicsBuffer InitializeBuffer(RaymarchEntity[] entities, ref Material material);
        void SetData(ref GraphicsBuffer buffer);
        void ReleaseData();
#if UNITY_EDITOR
        void DrawGizmos();
#endif
    }
}
