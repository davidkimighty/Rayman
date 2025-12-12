using UnityEngine;

namespace Rayman
{
    public class MaterialDataProvider : ScriptableObject, IMaterialDataProvider
    {
        public virtual void ProvideData(ref Material material) { }
    }
}