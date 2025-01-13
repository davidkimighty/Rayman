using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public abstract class SpatialStructure : MonoBehaviour
    {
        protected GraphicsBuffer NodeBuffer;
        
        public abstract void Setup(ref Material mat, RaymarchEntity[] entities);
        public abstract void SetData();
        public abstract void Release();
    }
}
