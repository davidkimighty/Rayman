using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class ShapeGroup : MonoBehaviour, IRaymarchGroup, IBoundsProvider
    {
        [SerializeField] private OperationType operation;
        [SerializeField] private float blend;
#if UNITY_EDITOR
        [SerializeField] private List<GameObject> EditorTargetGameObejcts = new();
#endif
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
        public bool IsGroupDirty { get; set; } = true;
        public List<ShapeVisual> Items => items;

#if UNITY_EDITOR
        private void OnValidate()
        {
            items.Clear();
            foreach (GameObject target in EditorTargetGameObejcts)
            {
                if (target == null) continue;

                ShapeVisual sv = new()
                {
                    ShapeProvider = target.GetComponent<ShapeProvider>(),
                    VisualProvider = target.GetComponent<VisualProvider>()
                };
                items.Add(sv);
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
        public ShapeProvider ShapeProvider;
        public VisualProvider VisualProvider;
    }
}