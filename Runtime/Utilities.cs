using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public static class Utilities
    {
        public static List<T> GetObjectsByTypes<T>(Transform root = null) where T : Component
        {
            List<T> found = new();
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

            foreach (Transform transform in transforms)
            {
                if (transform.parent != root) continue;
                SearchAdd(transform);
            }
            return found;
            
            void SearchAdd(Transform parent)
            {
                T component = parent.GetComponent<T>();
                if (component == null) return;
                found.Add(component);

                foreach (Transform child in parent)
                    SearchAdd(child);
            }
        }
    }
}