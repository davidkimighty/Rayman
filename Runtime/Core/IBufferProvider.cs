using UnityEngine;

namespace Rayman
{
    public interface IBufferProvider<T>
    {
        GraphicsBuffer Buffer { get; }
        bool IsInitialized { get; }

        void InitializeBuffer(ref Material material, T[] data);
        void SetData(T[] data);
        void ReleaseBuffer();
    }
}
