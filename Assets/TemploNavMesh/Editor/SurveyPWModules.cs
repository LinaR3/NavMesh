using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SurveyPWModules
{
    const string Root = "Assets/(P&W)Temple_Edition/Prefabs";
    const string Report = "Assets/TemploNavMesh/PW_ModuleDimensions.csv";

    [MenuItem("Tools/NavMesh Tutorial/Survey all P&W prefab geometry")]
    public static void Run()
    {
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x).ToArray();
        var sb=new StringBuilder("Prefab,Category,BoundsX,BoundsY,BoundsZ,PivotOffsetX,PivotOffsetY,PivotOffsetZ,Renderers,Colliders,MeshColliders,Children\n");
        foreach(var path in paths)
        {
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rs=go.GetComponentsInChildren<Renderer>(true);
                var cs=go.GetComponentsInChildren<Collider>(true);
                var b=new Bounds(Vector3.zero,Vector3.zero); bool any=false;
                foreach(var r in rs){if(!any){b=r.bounds;any=true;}else b.Encapsulate(r.bounds);}
                foreach(var c in cs){if(!any){b=c.bounds;any=true;}else b.Encapsulate(c.bounds);}
                var rel=path.Substring(Root.Length+1);var split=rel.Split('/');var category=split.Length>1?split[0]+"/"+split[1]:split[0];
                var name=Path.GetFileNameWithoutExtension(path);
                sb.AppendLine(string.Join(",",new[]{Q(name),Q(category),F(b.size.x),F(b.size.y),F(b.size.z),F(b.center.x-go.transform.position.x),F(b.center.y-go.transform.position.y),F(b.center.z-go.transform.position.z),rs.Length.ToString(),cs.Length.ToString(),go.GetComponentsInChildren<MeshCollider>(true).Length.ToString(),go.GetComponentsInChildren<Transform>(true).Length.ToString()}));
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        File.WriteAllText(Report,sb.ToString());AssetDatabase.ImportAsset(Report);
        Debug.Log("Surveyed "+paths.Length+" P&W prefabs; report: "+Report);
    }
    static string F(float v)=>v.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
    static string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
}
