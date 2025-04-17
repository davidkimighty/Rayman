using UnityEngine;

namespace Rayman
{
    public interface IRaymarchBufferProvider
    {
        bool IsInitialized { get; }
        
        GraphicsBuffer InitializeBuffer<T>(T[] dataProviders, ref Material material);
        void SetData(ref GraphicsBuffer buffer);
        void ReleaseData();
#if UNITY_EDITOR
        void DrawGizmos();
#endif
    }
}
