using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf;
using ProtoBuf.Meta;
using Rich;
using UnityEditor;
using UnityEngine;

namespace PathEditor
{
    [Serializable]
    [ProtoContract]
    public class Wave
    {
        [ProtoMember(1)] public float wait_time;

        [ProtoMember(2)] [NonSerialized] public ulong unit;

        [ProtoMember(3)] public float spawn_cool_down;

        [ProtoMember(4)] public float duration;

        [ProtoMember(5)] public int per_spawn_count;

        [ProtoMember(6)] public int path_index;

        public string unit_name;
    }

    [Serializable]
    [ProtoContract]
    public class WaveQueue
    {
        [ProtoMember(1)] public float wait_time;

        [ProtoMember(2)] public List<Wave> waves;
    }

    [ProtoContract]
    public class MapVector3
    {
        [ProtoMember(1)] public float x;
        [ProtoMember(2)] public float y;
        [ProtoMember(3)] public float z;

        public static MapVector3 RightHandPosition(Vector3 value)
        {
            return new MapVector3
            {
                x = value.x,
                y = value.y,
                z = -value.z,
            };
        }
    }

    [ProtoContract]
    public class MapVector4
    {
        [ProtoMember(1)] public float x;
        [ProtoMember(2)] public float y;
        [ProtoMember(3)] public float z;
        [ProtoMember(4)] public float w;

        public static MapVector4 Create(Quaternion value)
        {
            return new MapVector4
            {
                x = value.x,
                y = value.y,
                z = value.z,
                w = value.w,
            };
        }

        public static MapVector4 Create(Color value)
        {
            return new MapVector4
            {
                x = value.r,
                y = value.g,
                z = value.b,
                w = value.a,
            };
        }

        public static MapVector4 RightHandRotation(Quaternion value)
        {
            return new MapVector4
            {
                x = -value.x,
                y = -value.y,
                z = value.z,
                w = value.w,
            };
        }
    }

    [ProtoContract]
    public class PathWayPointData
    {
        [ProtoMember(1)] public MapVector3 position;
        [ProtoMember(2)] public float reach_range;
    }

    [ProtoContract]
    public class PathData
    {
        [ProtoMember(1)] public List<PathWayPointData> points;
    }

    [ProtoContract]
    public class CameraConfig
    {
        [ProtoMember(1)] public MapVector3 position;
        [ProtoMember(2)] public MapVector4 rotation;
        [ProtoMember(3)] public float fov;
        [ProtoMember(4)] public float aspect_ratio;
        [ProtoMember(5)] public float near;
        [ProtoMember(6)] public float far;
    }

    [ProtoContract]
    public class LightConfig
    {
        [ProtoMember(1)] public MapVector3 position;
        [ProtoMember(2)] public MapVector4 rotation;
        [ProtoMember(3)] public MapVector4 color;
        [ProtoMember(4)] public float shadow_bias;
        [ProtoMember(5)] public float shadow_normal_bias;
    }

    [ProtoContract]
    public class MapConfig : ScriptableObject
    {
        [ProtoMember(1)] public CameraConfig camera = new();

        [ProtoMember(2)] public LightConfig light = new();

        [ProtoMember(3)] public List<WaveQueue> wave_queues = new();

        [ProtoMember(4)] public List<PathData> paths;

#if UNITY_EDITOR
        [MenuItem("Assets/Create/MapConfig")]
        private static void CreateAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save wave config",
                "MapConfig", "asset", "Please enter a file name to save the wave config to");
            var asset = CreateInstance<MapConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif

        public void Save2Pb(string fileName, List<PathData> exportPathData)
        {
            var setting = ExportSetting.Get();

            var model = RuntimeTypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            var proto = model.GetSchema(typeof(MapConfig), ProtoSyntax.Proto3);
            var protoPath = $"{Application.dataPath}/MapExport/{fileName}_pb.proto";
            //create proto
            File.WriteAllText(protoPath, proto, new UTF8Encoding(false));
            //create rust code of decode proto
            FileUtils.CallCmd("pb-rs",
                $"--dont_use_cow -d {Application.dataPath}/{setting.TargetProtoSourcePath} {protoPath}");
            //create pb data
            using var file = File.Open($"{Application.dataPath}/{setting.TargetProtoDataPath}/{fileName}_pb.map",
                FileMode.Create);

            //deal wave
            {
                foreach (var waveQueue in wave_queues)
                {
                    foreach (var wave in waveQueue.waves)
                    {
                        wave.unit = CallNative.c_get_hash(Encoding.UTF8.GetBytes(wave.unit_name + '\0'));
                        Debug.Log($"unit name {wave.unit_name} -> {wave.unit}");
                    }
                }

                var path = $"{Application.dataPath}/../../rich/assets/config/hash.json";
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    throw new Exception($"the dir {dir} not exist");
                }

                CallNative.c_save_hash(Encoding.UTF8.GetBytes(path + '\0'));
            }

            {
                var unityCamera = Camera.main;
                if (unityCamera == null) throw new Exception("failed to find main camera");

                camera = new CameraConfig
                {
                    position = MapVector3.RightHandPosition(unityCamera.transform.position),
                    rotation = MapVector4.RightHandRotation(unityCamera.transform.rotation),
                    near = unityCamera.nearClipPlane,
                    far = unityCamera.farClipPlane,
                    fov = unityCamera.fieldOfView,
                    aspect_ratio = unityCamera.aspect,
                };
            }

            {
                var directionLight = FindObjectOfType<Light>();

                light = new LightConfig
                {
                    position = MapVector3.RightHandPosition(directionLight.transform.position),
                    rotation = MapVector4.RightHandRotation(directionLight.transform.rotation),
                    color = MapVector4.Create(directionLight.color),
                    shadow_bias = directionLight.shadowBias,
                    shadow_normal_bias = directionLight.shadowNormalBias,
                };
            }


            paths = exportPathData;
            Serializer.Serialize(file, this);
            paths = null;
            camera = null;
            light = null;
        }
    }
}