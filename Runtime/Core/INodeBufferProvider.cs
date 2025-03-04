using UnityEngine;

namespace Rayman
{
    public interface INodeBufferProvider<T> where T : struct, IBounds<T>
    {
        bool IsInitialized { get; }
        ISpatialStructure<T> SpatialStructure { get; }
        
        GraphicsBuffer InitializeBuffer(T[] bounds, ref Material material);
        void SyncBounds(int id, T bounds, float threshold = 0f);
        void SetData(ref GraphicsBuffer buffer);
        void ReleaseData();
#if UNITY_EDITOR
        void DrawGizmos();
#endif
    }
}
