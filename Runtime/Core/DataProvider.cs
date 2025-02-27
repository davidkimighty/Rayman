using UnityEngine;

namespace Rayman
{
    public abstract class DataProvider : ScriptableObject
    {
        public abstract void ProvideShaderProperties(ref Material material);
    }
}
