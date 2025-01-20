using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchDataProvider : ScriptableObject, IDebugProvider
    {
        public abstract void Setup(int groupId, RaymarchEntity[] entities, ref Material mat);
        public abstract void SetData(int groupId);
        public abstract void Release(int groupId);
        
#if UNITY_EDITOR
        public virtual void DrawGizmos(int groupId){ }
#endif
        public virtual string GetDebugMessage() => string.Empty;
    }
}
