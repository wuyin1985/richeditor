using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UnityEngine;

namespace GltfFast.Runtime.Scripts
{
    public static class ImageConvert
    {
        private static Texture2D get_texture2d_readable(Texture texture, string assetPath)
        {
            var t2x = texture as Texture2D;
            if (t2x == null) throw new Exception($"the texture is not texture2d");
            bool isNormal = false;
#if UNITY_EDITOR
            UnityEditor.TextureImporter ti =
                (UnityEditor.TextureImporter)UnityEditor.TextureImporter.GetAtPath(assetPath);
            if (!t2x.isReadable)
            {
                ti.isReadable = true;
                UnityEditor.AssetDatabase.ImportAsset(assetPath);
            }
#endif
            return t2x;
        }


        public static string ConvertImageByExtension(UnityEngine.Texture uTexture, string originFileName)
        {
            var extension = Path.GetExtension(originFileName).ToLower();
            switch (extension)
            {
                case ".tif":
                {
                    var fullPath = $"{Application.dataPath}/../{originFileName}";
                    using Image imageFile = Image.FromFile(fullPath);
                    var frameDimensions = new FrameDimension(imageFile.FrameDimensionsList[0]);
                    var frameNum = imageFile.GetFrameCount(frameDimensions);
                    if (frameNum > 1) throw new Exception("the tif has more than one frame");

                    var fileName = Path.GetFileNameWithoutExtension(originFileName);
                    var newPath = $"Temp/{fileName}.jpeg";
                    imageFile.SelectActiveFrame(frameDimensions, 0);

                    using (var bmp = new Bitmap(imageFile))
                    {
                        bmp.Save($"{Application.dataPath}/../{newPath}", ImageFormat.Jpeg);
                    }

                    Debug.Log($"convert {originFileName} to {newPath}");
                    return newPath;
                }
            }

            return originFileName;
        }
    }
}