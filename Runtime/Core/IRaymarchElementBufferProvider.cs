using UnityEngine;

namespace Rayman
{
    public interface IRaymarchElementBufferProvider
    {
        bool IsInitialized { get; }
        
        GraphicsBuffer InitializeBuffer(RaymarchElement[] elements, ref Material material);
        void SetData(ref GraphicsBuffer buffer);
        void ReleaseData();
#if UNITY_EDITOR
        void DrawGizmos();
#endif
    }
}
