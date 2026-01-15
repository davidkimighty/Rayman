using Unity.Collections;
using UnityEngine;

namespace Rayman
{
    public interface INativeBufferProvider<T> where T : struct
    {
        GraphicsBuffer Buffer { get; }
        bool IsInitialized { get; }
        int DataLength { get; }

        void InitializeBuffer(ref Material material, NativeArray<T> data, NativeArray<int> primitiveIds);
        void SetData(NativeArray<T> data);
        void ReleaseBuffer();
    }
}
