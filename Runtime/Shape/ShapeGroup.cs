using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class ShapeGroup : MonoBehaviour, IRaymarchGroup, IBoundsProvider
    {
        [SerializeField] private OperationType operation;
        [SerializeField] private float blend;
        [SerializeField] private List<ShapeVisual> items = new();

        public OperationType Operation
        {
            get => operation;
            set { operation = value; IsGroupDirty = true; }
        }
        public float Blend
        {
            get => blend;
            set { blend = value; IsGroupDirty = true; }
        }
        public int Count => items?.Count ?? 0;
        public bool IsGroupDirty { get; set; } = true;
        public List<ShapeVisual> Items => items;

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (ShapeVisual sv in items)
            {
                if (sv.EditorTargetSource == null) continue;

                sv.ShapeProvider = sv.EditorTargetSource.GetComponent<ShapeProvider>();
                sv.VisualProvider = sv.EditorTargetSource.GetComponent<VisualProvider>();
            }
            IsGroupDirty = true;
        }
#endif

        public T GetBounds<T>() where T : struct, IBounds<T>
        {
            T bounds = items[0].ShapeProvider.GetBounds<T>();
            for (int i = 1; i < items.Count; i++)
                bounds = bounds.Union(items[i].ShapeProvider.GetBounds<T>());
            return bounds;
        }
    }

    [Serializable]
    public class ShapeVisual
    {
#if UNITY_EDITOR
        public GameObject EditorTargetSource;
#endif
        public ShapeProvider ShapeProvider;
        public VisualProvider VisualProvider;
    }
}