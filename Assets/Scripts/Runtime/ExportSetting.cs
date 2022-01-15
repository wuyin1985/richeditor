using System.IO;
using UnityEngine;

namespace PathEditor
{
    public class ExportSetting : ScriptableObject
    {
        private static ExportSetting _instance;

        public const string PATH = "Assets/Scripts/ExportSetting.asset";

        public static ExportSetting Get()
        {
            if (_instance != null)
            {
                return _instance;
            }

#if UNITY_EDITOR
            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ExportSetting>(PATH);

            if (_instance == null)
            {
                _instance = CreateInstance<ExportSetting>();
                UnityEditor.AssetDatabase.CreateAsset(_instance, PATH);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif

            return _instance;
        }

        public bool showPathGizmo;

		public string TargetGlbPath;

		public string TargetProtoDataPath;

		public string TargetProtoSourcePath;
    }
}