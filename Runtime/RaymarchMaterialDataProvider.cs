using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchMaterialDataProvider : ScriptableObject
    {
        public abstract void SetData(ref Material mat);
    }
}
