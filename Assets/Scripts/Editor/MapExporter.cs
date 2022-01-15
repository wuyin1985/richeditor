using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AmazingAssets.TerrainToMesh;
using GLTFast;
using GLTFast.Export;
using PathEditor;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Rich
{
    public class MapExporter : EditorWindow
    {
        [MenuItem("Rich/Map/MapExporter")]
        static void ShowWindow()
        {
            var window = GetWindow<MapExporter>();
            window.Show();
        }

        public int mapsResolution = 1024;

        private GameObject exportSource;

        private void OnGUI()
        {
            exportSource = (GameObject)EditorGUILayout.ObjectField("Target", exportSource, typeof(GameObject));

            if (exportSource != null)
            {
                if (GUILayout.Button("Export"))
                {
                    export_map();
                    export_terrain();
                }
            }
        }

        private List<PathData> generate_path_data()
        {
            var results = new List<PathData>();
            var ps = exportSource.GetComponentsInChildren<WayPath>();
            foreach (var wayPath in ps)
            {
                var wayPoints = wayPath.gameObject.GetComponentsInChildren<WayPoint>();

                var p = new PathData
                {
                    points = wayPoints.Select(point =>
                    {
                        var pos = point.transform.position;
                        var range = point.reachDistance;

                        return new PathWayPointData
                        {
                            position = new MapVector3
                            {
                                x = pos.x,
                                y = pos.y,
                                z = -pos.z,
                            },
                            reach_range = range,
                        };
                    }).ToList()
                };
                
                results.Add(p);
            }

            return results;
        }

        private void export_map()
        {
            var map = exportSource.GetComponentInChildren<Map>();
            if (map == null) throw new Exception($"can't find map in the game object {exportSource.name}");
            map.config.Save2Pb(exportSource.gameObject.name, generate_path_data());
        }

        private string get_temp_dir_path()
        {
            return $"{Application.dataPath}/EXPORT_TEMP";
        }

        private void reset_temp_dir()
        {
            var path = get_temp_dir_path();
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string full_path_2_asset_path(string fullPath)
        {
            return fullPath.Replace(Application.dataPath, "Assets");
        }


        private Texture2D save_texture(Texture2D texture2D, bool isNormal)
        {
            var jpg = texture2D.EncodeToJPG();
            var textureName = texture2D.name.Replace(" ", "_");
            var imgPath = $"{get_temp_dir_path()}/{textureName}.jpg";
            File.WriteAllBytes(imgPath, jpg);

            var assetPath = full_path_2_asset_path(imgPath);
            AssetDatabase.ImportAsset(assetPath);
            var ti =
                (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (isNormal)
            {
                ti.textureType = TextureImporterType.NormalMap;
                AssetDatabase.ImportAsset(assetPath);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }


        private void export_terrain()
        {
            //reset_temp_dir();
            var setting = ExportSetting.Get();

            var terrain = exportSource.GetComponentInChildren<Terrain>();
            if (terrain == null)
            {
                throw new Exception($"terrain not found");
            }

            var terrainData = terrain.terrainData;
            var newMesh = terrainData.TerrainToMesh().ExportMesh(25, 25, Normal.CalculateFromTerrain);
            if (terrain.terrainData.terrainLayers.Length > 1)
            {
                throw new Exception("only support one layer terrain");
            }

            var layer = terrain.terrainData.terrainLayers[0];
            var diffuseTexture = layer.diffuseTexture;
            var normalTexture = layer.normalMapTexture;

            // var diffuseTexture = save_texture(terrainData.TerrainToMesh()
            //     .ExportBasemapDiffuseTexture(mapsResolution, false, false), false);
            // var normalTexture =
            //     save_texture(terrainData.TerrainToMesh().ExportBasemapNormalTexture(mapsResolution, false), true);
            //

            var shaderName = TerrainToMeshConstants.shaderUnityDefault;
            var mainTexturePropName = TerrainToMeshConstants.materailPropTextureMainTex;
            var bumpMapPropName = TerrainToMeshConstants.materailPropTextureBumpMap;
            //
            var material = new Material(Shader.Find(shaderName));
            InitializeMaterial(material);
            material.mainTextureScale = layer.tileSize;
            material.mainTextureOffset = layer.tileOffset;
            material.SetTexture(mainTexturePropName, diffuseTexture);

            if (normalTexture != null)
            {
                material.SetTexture(bumpMapPropName, normalTexture);
                material.EnableKeyword("_NORMALMAP");

                var normalScale = layer.normalScale;
                material.SetFloat("_BumpScale", normalScale);
            }

            var newTerrainWrap = new GameObject("ExportTerrainWrap");
            var newTerrain = new GameObject("Terrain");
            newTerrain.transform.SetParent(newTerrainWrap.transform);
            newTerrain.transform.localPosition = new Vector3(-terrainData.size.x / 2, 0, -terrainData.size.z / 2);
            newTerrain.transform.localRotation = Quaternion.identity;
            var mf = newTerrain.AddComponent<MeshFilter>();
            mf.mesh = newMesh;
            var render = newTerrain.AddComponent<MeshRenderer>();
            render.material = material;

            //export gltf
            var gltfExportSetting = new ExportSettings
            {
                format = GltfFormat.Binary,
            };

            var export = new GameObjectExport(gltfExportSetting, logger: new ConsoleLogger());
            export.AddScene(new[] { newTerrainWrap }, newTerrainWrap.name);
            var glb_path = $"{Application.dataPath}/MapExport/{exportSource.name}_export.glb";
            export.SaveToFileAndDispose(glb_path);

            var glb_file_name = Path.GetFileName(glb_path);
            var glb_copy_to = $"{Application.dataPath}/{setting.TargetGlbPath}/{glb_file_name}";
            File.Copy(glb_path, glb_copy_to, true);

            AssetDatabase.Refresh();

            DestroyImmediate(newTerrainWrap);
        }

        private void InitializeMaterial(Material material)
        {
            if (TerrainToMeshUtilities.GetCurrentRenderPipeline() ==
                TerrainToMeshUtilities.RenderPipeline.HighDefinition)
            {
                if (material.HasProperty("_DistortionSrcBlend"))
                    material.SetFloat("_DistortionSrcBlend", 1);
                if (material.HasProperty("_DistortionDstBlend"))
                    material.SetFloat("_DistortionDstBlend", 1);
                if (material.HasProperty("_DistortionBlurSrcBlend"))
                    material.SetFloat("_DistortionBlurSrcBlend", 1);
                if (material.HasProperty("_DistortionBlurDstBlend"))
                    material.SetFloat("_DistortionBlurDstBlend", 1);

                if (material.HasProperty("_StencilWriteMask"))
                    material.SetFloat("_StencilWriteMask", 6);
                if (material.HasProperty("_StencilRefGBuffer"))
                    material.SetFloat("_StencilRefGBuffer", 10);
                if (material.HasProperty("_StencilWriteMaskGBuffer"))
                    material.SetFloat("_StencilWriteMaskGBuffer", 14);
                if (material.HasProperty("_StencilRefDepth"))
                    material.SetFloat("_StencilRefDepth", 8);

                if (material.HasProperty("_StencilRefMV"))
                    material.SetFloat("_StencilRefMV", 40);
                if (material.HasProperty("_StencilWriteMaskMV"))
                    material.SetFloat("_StencilWriteMaskMV", 40);

                if (material.HasProperty("_ZTestDepthEqualForOpaque"))
                    material.SetFloat("_ZTestDepthEqualForOpaque", 3);
                if (material.HasProperty("_ZTestModeDistortion"))
                    material.SetFloat("_ZTestModeDistortion", 4);

                material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            }
        }
    }
}