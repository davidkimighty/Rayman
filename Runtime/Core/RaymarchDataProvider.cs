using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchDataProvider : ScriptableObject
    {
        public abstract void SetData(ref Material mat);
    }
}
