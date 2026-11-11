using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CaptureTempleReviewPreview
{
    const string ScenePath = "Assets/Scenes/NewSceneTemplo_Review.unity";
    const string OutputPath = "/tmp/TemploReferenciaMayaPreview.png";

    public static void Capture()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>(true)).FirstOrDefault(x => x.CompareTag("MainCamera"));
            if (camera == null) throw new System.InvalidOperationException("La escena no tiene Main Camera.");
            const int width = 1600, height = 900;
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            camera.targetTexture = texture;
            camera.aspect = (float)width / height;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
            Debug.Log("Temple review preview saved to " + OutputPath);
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }
}
