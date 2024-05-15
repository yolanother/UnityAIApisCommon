using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace DoubTech.ThirdParty.AI.Common.Data
{
    public abstract class ApiConfig : ScriptableObject, IApiConfig
    {
        public abstract string GetUrl(params string[] path);
        public abstract Task RefreshModels();
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(ApiConfig), true)]
    public class ApiConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Refresh Models"))
            {
                RefreshModels();
            }
        }

        private async void RefreshModels()
        {
            IApiConfig config = (IApiConfig) target;
            await config.RefreshModels();
            SafeRepaint();
        }

        private void SafeRepaint()
        {
            EditorApplication.update += DoRepaint;
        }

        private void DoRepaint()
        {
            EditorApplication.update -= DoRepaint;
            Repaint();
        }
    }
#endif
}