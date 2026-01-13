using UnityEngine;

namespace Rayman
{
    public abstract class MaterialDataProvider : ScriptableObject, IMaterialDataProvider
    {
        public abstract void ProvideData(ref Material material);
    }
}