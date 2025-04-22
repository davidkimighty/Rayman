using UnityEngine;

namespace Rayman
{
    public interface IBufferProvider<T> 
    {
        bool IsInitialized { get; }
        
        GraphicsBuffer InitializeBuffer(T[] dataProviders, ref Material material);
        void SetData(ref GraphicsBuffer buffer);
        void ReleaseData();
#if UNITY_EDITOR
        void DrawGizmos();
#endif
    }
}
