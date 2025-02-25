using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchDataProvider : ScriptableObject
    {
        public abstract void SetupShaderProperties(ref Material material);
    }
}
