using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnalyzeTemplePrefabs
{
    const string ReportPath = "Assets/TemploNavMesh/PrefabAnalysis.txt";
    const string PlatformReportPath = "Assets/TemploNavMesh/PlatformPrefabSurvey.txt";

    [MenuItem("Tools/NavMesh Tutorial/Analyze Temple and Character Sizes")]
    public static void Analyze()
    {
        var paths = new[]
        {
            "Assets/(P&W)Temple_Edition/Prefabs/Basic/Platforms/6x6x.25_Floor_A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Basic/Platforms/8x8x.25_Floor_A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Basic/Platforms/Basic_Stairs_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Basic/Platforms/Ramp_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Basic/Platforms/Stairs_Platform_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Platforms/HD_Platform_2A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Platforms/HD_Platform_6A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Archs/HD_Arch_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Entrance_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Entrance_3A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Temple_Stairs_Entrance_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Temple_Stairs_Entrance_1B.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Platform_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Tunnel_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Stack_A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Stack_B.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Stack_C.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Stack_C2.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Group_Walls/Barrier_Group_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Group_Walls/Single_Group_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Walls/HD_Wall_2A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Rounded_Walls/HD_Rounded_Wall_3A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Small_Objects/Torch_Post_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Small_Objects/Bowl_1A.prefab",
            "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Full_Railing_1.prefab",
            "Assets/Adventures_woman/Prefabs/Adventures_woman_02.prefab",
            "Assets/Mini Legion Rock Golem PBR HP Polyart/Prefabs/Polyart_Golem.prefab",
            "Assets/Mini Legion Rock Golem PBR HP Polyart/Prefabs/HP_Golem.prefab",
        };

        var output = new StringBuilder();
        output.AppendLine("P&W Temple Edition — dimensiones de piezas seleccionadas (metros Unity)");
        output.AppendLine("Las medidas son de Renderer/Collider; centro relativo al pivote del prefab.");
        output.AppendLine();
        foreach (var relativePath in paths)
        {
            var path = relativePath;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                output.AppendLine($"NO ENCONTRADO: {path}");
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                var colliders = root.GetComponentsInChildren<Collider>(true);
                var found = false;
                var bounds = new Bounds(root.transform.position, Vector3.zero);
                foreach (var renderer in renderers)
                {
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                foreach (var collider in colliders)
                {
                    if (!found) { bounds = collider.bounds; found = true; }
                    else bounds.Encapsulate(collider.bounds);
                }
                if (!found) output.AppendLine($"{asset.name}: sin Renderer/Collider");
                else
                {
                    var relativeCenter = bounds.center - root.transform.position;
                    output.AppendLine($"{asset.name}: {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2} m; centro local ({relativeCenter.x:F2}, {relativeCenter.y:F2}, {relativeCenter.z:F2}); renderers {renderers.Length}, colliders {colliders.Length}");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        File.WriteAllText(ReportPath, output.ToString());
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Análisis listo", $"Medí las piezas del paquete. Abrí {ReportPath} para consultar tamaños y pivotes.", "OK");
    }

    [MenuItem("Tools/NavMesh Tutorial/Survey Platform Prefabs")]
    public static void SurveyPlatformPrefabs()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/(P&W)Temple_Edition/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.Contains("/Platforms/") || path.Contains("/Unique/Structures/") || path.Contains("/Unique/Unique_"))
            .OrderBy(path => path)
            .ToArray();

        var output = new StringBuilder();
        output.AppendLine("Inventario de piezas transitables y estructuras del paquete P&W Temple Edition");
        output.AppendLine("Cobertura XZ = suma de área proyectada de triángulos / rectángulo de los renderers; sirve para distinguir losas llenas de piezas con huecos.");
        output.AppendLine();
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);

                var projectedArea = 0f;
                foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = meshFilter.sharedMesh;
                    if (mesh == null) continue;
                    var vertices = mesh.vertices;
                    var triangles = mesh.triangles;
                    for (var i = 0; i + 2 < triangles.Length; i += 3)
                    {
                        var a = root.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(vertices[triangles[i]]));
                        var b = root.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]));
                        var c = root.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]));
                        projectedArea += Mathf.Abs((b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z)) * 0.5f;
                    }
                }

                var relative = path.Replace("Assets/(P&W)Temple_Edition/Prefabs/", "");
                var coverage = bounds.size.x * bounds.size.z > 0.001f ? projectedArea / (bounds.size.x * bounds.size.z) : 0f;
                output.AppendLine($"{relative} | {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2} m | cobertura XZ {coverage:F2} | {renderers.Length} renderers");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        File.WriteAllText(PlatformReportPath, output.ToString());
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Inventario listo", $"Comparé {paths.Length} prefabs. El reporte está en {PlatformReportPath}.", "OK");
    }
}
