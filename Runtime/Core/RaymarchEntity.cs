using UnityEngine;

namespace Rayman
{
    public enum Operations
    {
        Union,
        Subtract,
        Intersect
    }
    
    public class RaymarchEntity : MonoBehaviour, IBoundsProvider
    {
        public bool UseLossyScale = true;
        public float ExpandBounds;

        public virtual T GetBounds<T>() where T : struct, IBounds<T>
        {
            return default;
        }
        
        public Vector3 GetScale() => UseLossyScale ? transform.lossyScale : Vector3.one;
    }
    
    public interface IRaymarchEntityData
    {
        void InitializeData(RaymarchEntity entity);
    }
}
