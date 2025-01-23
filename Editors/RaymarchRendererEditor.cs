using Rayman;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            AssemblyReloadEvents.afterAssemblyReload += Release;
            EditorSceneManager.sceneSaved += Reload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= Release;
            EditorSceneManager.sceneSaved -= Reload;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build"))
                raymarchRenderer.Setup();

            if (GUILayout.Button("Release"))
                raymarchRenderer.Release();
            
            if (GUILayout.Button("Find All Groups"))
                raymarchRenderer.FindAllGroups();
            EditorGUILayout.EndHorizontal();
        }

        private void Release()
        {
            raymarchRenderer.Release();
        }

        private void Reload(Scene scene)
        {
            bool rebuild = raymarchRenderer.IsInitialized;
            raymarchRenderer.Release();
            if (rebuild)
                raymarchRenderer.Setup();
        }
    }
}
