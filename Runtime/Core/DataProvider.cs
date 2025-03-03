using UnityEngine;

namespace Rayman
{
    public abstract class DataProvider : ScriptableObject
    {
        public abstract void ProvideData(ref Material material);
    }
}
