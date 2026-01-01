using UnityEngine;

namespace Rayman
{
    public interface IBufferProvider<T> where T : class
    {
        GraphicsBuffer Buffer { get; }
        bool IsInitialized { get; }

        void InitializeBuffer(ref Material material, T[] dataProviders);
        void SetData();
        void ReleaseBuffer();
    }
}
