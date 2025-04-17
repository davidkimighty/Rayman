using Rayman;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RaymanEditor
{
#if UNITY_EDITOR
    [CustomEditor(typeof(RaymarchRenderer))]
    [CanEditMultipleObjects]
    public class RaymarchRendererEditor : Editor
    {
        private RaymarchRenderer raymarchRenderer;

        private void OnEnable()
        {
            raymarchRenderer = (RaymarchRenderer)target;
        }
        
        public override void OnInspectorGUI()
        {
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup"))
                raymarchRenderer.Setup();

            if (GUILayout.Button("Release"))
                raymarchRenderer.Cleanup();

            if (GUILayout.Button("Find All Objects"))
            {
                foreach (Object t in targets)
                {
                    RaymarchRenderer renderer = (RaymarchRenderer)t;
                    renderer.FindAllRaymarchObjects();
                    EditorUtility.SetDirty(renderer);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
