using Rayman;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaymanEditor
{
    [CustomEditor(typeof(RaymarchRenderer))]
    public class RaymarchRendererEditor : Editor
    {
        private RaymarchRenderer raymarchRenderer;
        
        private void OnEnable()
        {
            raymarchRenderer = (RaymarchRenderer)target;
            AssemblyReloadEvents.afterAssemblyReload += Reload;
            EditorSceneManager.sceneSaved += Reload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= Reload;
            EditorSceneManager.sceneSaved -= Reload;

        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build"))
                raymarchRenderer.Build();

            if (GUILayout.Button("Release"))
                raymarchRenderer.Release();
            
            if (GUILayout.Button("Find All Groups"))
                raymarchRenderer.FindAllGroups();
            EditorGUILayout.EndHorizontal();
        }

        private void Reload()
        {
            raymarchRenderer.Release();
            raymarchRenderer.Build();
        }

        private void Reload(Scene scene)
        {
            raymarchRenderer.Release();
            raymarchRenderer.Build();
        }
    }
}
