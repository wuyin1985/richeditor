using System;
using System.Diagnostics;
using System.IO;

namespace Rich
{
    public static class FileUtils
    {
        public static void ResetDirectory(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, true);
            }

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public static void CopyFile(string file, string targetDir)
        {
            var name = Path.GetFileName(file);
            File.Copy(file, $"{targetDir}/{name}", true);
        }

        public static void CreateDirectoryIfNotExist(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public static string CallCmd(string exe, string verbs,
            string workingDirectory = null,
            bool showConsole = false)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(exe, verbs);
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                p.StartInfo.WorkingDirectory = workingDirectory;
            }

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = !showConsole;
            p.Start();

            var error = p.StandardError.ReadToEnd();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            p.Close();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"execute {exe} occur error : {error}");
            }

            UnityEngine.Debug.Log($"call {exe} {verbs}, output: \n{output}");

            return output;
        }
    }
}