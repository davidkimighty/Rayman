using Rayman;
using UnityEditor;
using UnityEngine;

namespace RaymanEditor
{
    [CustomEditor(typeof(RaymarchGroup))]
    [CanEditMultipleObjects]
    public class RaymarchGroupEditor : Editor
    {
        private RaymarchGroup raymarchGroup;

        private void OnEnable()
        {
            raymarchGroup = (RaymarchGroup)target;
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
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Find All Entities"))
                raymarchGroup.FindAllEntities();
            
            if (GUILayout.Button("Get Buffer Providers"))
                raymarchGroup.FindAllBufferProviders();
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
