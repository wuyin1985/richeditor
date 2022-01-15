using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PathEditor
{
    public class WayPath : MonoBehaviour
    {
        [Button]
        private void AddPoint()
        {
            var ps = GetComponentsInChildren<WayPoint>();
            var newObj = new GameObject($"p{ps.Length + 1}");
            newObj.AddComponent<WayPoint>();
            newObj.transform.SetParent(transform);

            if (ps.Length > 0)
            {
                newObj.transform.position = ps[^1].transform.position;

#if UNITY_EDITOR
                UnityEditor.Selection.activeObject = newObj;
#endif
            }
        }

        [Button]
        private void RenamePoints()
        {
            var ps = GetComponentsInChildren<WayPoint>();
            for (int i = 0; i < ps.Length; i++)
            {
                ps[i].gameObject.name = $"p{i + 1}";
            }
        }

        private void OnDrawGizmos()
        {
            var setting = ExportSetting.Get();
            if (!setting.showPathGizmo) return;
            var ps = GetComponentsInChildren<WayPoint>();
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                Transform next = null;
                if (i != ps.Length - 1)
                {
                    next = ps[i + 1].transform;
                }

                if (next != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(p.transform.position, next.transform.position);
                }

                if (p.reachDistance > 0)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(p.transform.position, p.reachDistance);
                }
            }
        }
    }
}