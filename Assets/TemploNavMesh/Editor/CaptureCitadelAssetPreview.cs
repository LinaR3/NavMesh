using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;
public static class CaptureCitadelAssetPreview
{
 public static void Capture(){try{var s=EditorSceneManager.OpenScene("Assets/Scenes/CiudadelaMaya_AssetsPreview.unity");
  // The isolated batch project cannot compile the installed URP shader variants. Use a temporary built-in material only for this capture; do not save scene changes.
  GraphicsSettings.defaultRenderPipeline=null; QualitySettings.renderPipeline=null;
  var previewShader=Shader.Find("Standard");var cache=new System.Collections.Generic.Dictionary<string,Material>();
  foreach(var renderer in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
  {if(renderer.sharedMaterials.Length>0&&renderer.sharedMaterials.All(m=>m&&m.name.Contains("Collider_Transparent"))){renderer.enabled=false;continue;}renderer.sharedMaterials=renderer.sharedMaterials.Select(old=>{var tex=old?(old.HasProperty("_BaseMap")?old.GetTexture("_BaseMap"):old.mainTexture):null;var tint=old?(old.HasProperty("_BaseColor")?old.GetColor("_BaseColor"):old.color):Color.white;var key=old?old.GetInstanceID().ToString():"none";if(!cache.TryGetValue(key,out var m)){var shader=old&&old.renderQueue>=2450?Shader.Find("Unlit/Transparent Cutout"):previewShader;m=new Material(shader){color=tint,mainTexture=tex};if(old&&old.renderQueue>=2450){m.SetFloat("_Cutoff",.35f);m.renderQueue=2450;}cache[key]=m;}return m;}).ToArray();}
  var c=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).FirstOrDefault();if(!c)throw new System.Exception("Camera missing");var r=new RenderTexture(1600,900,24);c.targetTexture=r;c.aspect=1600f/900f;Capture(c,r,"/tmp/CiudadelaMaya_AssetsPreview.png");c.transform.position=new Vector3(0,30,-125);c.transform.rotation=Quaternion.LookRotation(new Vector3(0,8,16)-c.transform.position);c.fieldOfView=55;Capture(c,r,"/tmp/CiudadelaMaya_AssetsPreview_Perspectiva.png");EditorApplication.Exit(0);}catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Capture(Camera c,RenderTexture r,string path){c.Render();RenderTexture.active=r;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);}
}
