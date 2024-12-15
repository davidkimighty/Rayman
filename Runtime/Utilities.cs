using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public static class Utilities
    {
        public static List<T> GetChildrenByHierarchical<T>(Transform root = null) where T : Component
        {
            List<T> found = new();
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID);

            foreach (Transform transform in transforms)
            {
                if (transform.parent != root) continue;
                
                SearchAdd(transform);
            }
            return found;
            
            void SearchAdd(Transform target)
            {
                if (!target.gameObject.activeInHierarchy) return;
                
                T component = target.GetComponent<T>();
                if (component != null)
                    found.Add(component);

                foreach (Transform child in target)
                    SearchAdd(child);
            }
        }
    }
}