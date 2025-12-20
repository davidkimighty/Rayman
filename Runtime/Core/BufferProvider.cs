using UnityEngine;

namespace Rayman
{
    public abstract class BufferProvider<T> : MonoBehaviour, IBufferProvider<T> where T : class
    {
        public virtual GraphicsBuffer Buffer { get; protected set; }
        public virtual bool IsInitialized => Buffer != null;
        public virtual int DataCount { get; protected set; }

        public abstract void InitializeBuffer(ref Material material, T[] dataProviders);

        public abstract void ReleaseBuffer();

        public abstract void SetData();
    }
}