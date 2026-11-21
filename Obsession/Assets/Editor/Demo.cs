using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Demo
{
    [MenuItem("Obsession/Play corridor")]
    public static void Play()
    {
        const string path="Assets/Corridor.unity";
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var camera=new GameObject("Corridor camera",typeof(Camera),typeof(Corridor));
            camera.tag="MainCamera";
            EditorSceneManager.SaveScene(scene,path);
        }
        else EditorSceneManager.OpenScene(path);
        EditorApplication.isPlaying=true;
    }
}
