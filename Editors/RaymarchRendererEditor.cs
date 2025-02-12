using Rayman;
using UnityEditor;
using UnityEngine;

namespace RaymanEditor
{
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
                raymarchRenderer.Release();
            
            if (GUILayout.Button("Find All Groups"))
                raymarchRenderer.FindAllGroups();
            EditorGUILayout.EndHorizontal();
        }
    }
}
