using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchEntityGroup : MonoBehaviour
    {
        public Material MaterialInstance;
        
        [SerializeField] protected Material materialRef;
        [SerializeField] protected SpatialStructure spatialStructure;
        protected GraphicsBuffer EntityBuffer;

        public abstract void Setup();
        public abstract void SetData();
        public abstract void Release();
    }
}
