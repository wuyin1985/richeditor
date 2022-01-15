using System;
using UnityEditor;

namespace PathEditor
{
    public class ExportSettingWindow : EditorWindow
    {
        [MenuItem("Rich/Show Setting Window")]
        static void ShowWindow()
        {
            var window = GetWindow<ExportSettingWindow>();
            window.Show();
        }

        private UnityEditor.Editor _editor;

        private void OnEnable()
        {
            _editor = UnityEditor.Editor.CreateEditor(ExportSetting.Get());
        }

        private void OnGUI()
        {
            _editor.OnInspectorGUI();
        }
    }
}