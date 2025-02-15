using UnityEngine;

namespace Rayman
{
    public interface IBufferProvider
    {
        bool IsInitialized { get; }
        
        void SetupBuffer(RaymarchEntity[] entities, ref Material mat);
        void UpdateBufferData();
        void ReleaseBuffer();
#if UNITY_EDITOR
        void DrawGizmos();
#endif
    }
}
