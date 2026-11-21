using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WebRelease
{
    // Run from batch mode with -buildTarget WebGL -executeMethod WebRelease.Build.
    public static void Build()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use batch mode to build the web release.");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs"));
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/WebStartup.unity");
        string product = PlayerSettings.productName;
        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save the startup scene.");
            PlayerSettings.productName = "Phase Panic";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { path }, target = BuildTarget.WebGL, locationPathName = output,
                options = BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Web build failed. See the Unity build log.");
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            File.Copy(Path.Combine(Application.dataPath, "Resources/Fonts/DejaVu-License.txt"),
                Path.Combine(output, "DejaVu-License.txt"), true);
            Debug.Log($"Web release succeeded: {report.summary.totalSize} bytes, {report.summary.totalWarnings} warnings.");
        }
        finally
        {
            PlayerSettings.productName = product;
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
            AssetDatabase.DeleteAsset(path);
        }
    }
}
