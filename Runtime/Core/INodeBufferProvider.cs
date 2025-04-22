using UnityEngine;

namespace Rayman
{
    public interface INodeBufferProvider
    {
        bool IsInitialized { get; }
        ISpatialStructure SpatialStructure { get; }

        GraphicsBuffer InitializeBuffer(IBoundsProvider[] providers, ref Material material);
        void SyncBounds(IBoundsProvider[] providers, float syncThreshold = 0f);
        void SetData(ref GraphicsBuffer buffer);
        void ReleaseData();
    #if UNITY_EDITOR
        void DrawGizmos();
    #endif
    }
}
