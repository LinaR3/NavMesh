using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CaptureCitadelBlockoutPreview
{
    public static void Capture()
    {
        try {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CiudadelaMaya_Blockout.unity",OpenSceneMode.Single);
            var cam=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
            if(!cam)throw new System.Exception("Camera missing");
            var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.aspect=1600f/900f;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
            File.WriteAllBytes("/tmp/CiudadelaMaya_Blockout.png",tex.EncodeToPNG());EditorApplication.Exit(0);
        } catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
