using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchDataProvider : MonoBehaviour
    {
        public abstract void Setup(ref Material mat, RaymarchEntity[] entities);
        public abstract void SetData();
        public abstract void Release();
    }
}
