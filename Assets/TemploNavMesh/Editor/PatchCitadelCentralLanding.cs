#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Idempotently restores the missing upper-central landing without rebuilding the prefab.</summary>
static class PatchCitadelCentralLanding
{
    const string PrefabPath = "Assets/TemploNavMesh/Prefabs/CiudadelaMaya_DesdeFotos_Revision.prefab";
    const string FloorPath = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Platforms/HD_Platform_6A.prefab";

    [MenuItem("Tools/Ciudadela/Repair central gallery landing")]
    static void Repair()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var prefabAdded = 0;
        try
        {
            var gallery = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Galeria_superior_central");
            if (gallery == null) throw new InvalidOperationException("No se encontró Galeria_superior_central en el prefab.");
            var floorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPath);
            if (floorAsset == null) throw new InvalidOperationException("No se encontró el módulo de piso P&W.");
            prefabAdded = RepairCentralStairwell(gallery, floorAsset);
            if (prefabAdded > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var scene = EditorSceneManager.GetActiveScene();
        var sceneGallery = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "Galeria_superior_central");
        if (sceneGallery == null) throw new InvalidOperationException("No se encontró Galeria_superior_central en la escena activa.");
        var sceneAdded = RepairCentralStairwell(sceneGallery, AssetDatabase.LoadAssetAtPath<GameObject>(FloorPath));
        if (sceneAdded > 0) EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Abertura de escalera central reparada: prefab y escena; losas centrales retiradas, hombros laterales y descansos continuos a Y=8.18 m. Las antorchas y luces no se regeneraron.");
    }

    [MenuItem("Tools/Ciudadela/Repair central lookout stair connection")]
    static void RepairLookoutStairConnection()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).ToArray();
        var flight = roots.FirstOrDefault(t => t.name == "Escalera_galería_a_mirador_ala_0");
        var landing = roots.FirstOrDefault(t => t.name == "Descanso_superior_escalera_mirador_0_Piso_transitable_a_cota");
        if (flight == null || landing == null) throw new InvalidOperationException("No se encontró la escalera central ni su descanso superior.");

        // Stair flights top out at z=61.27 m. The authored landing begins at z=61.27
        // but its box is only ~4.73 m deep, leaving no full-width walkable apron to z=70.
        // Add an 8 x 8 stone apron at the same level, overlapping both the stair lip and terrace.
        const string apronName = "Descanso_superior_mirador_extension_PW";
        var temple = roots.FirstOrDefault(t => t.name == "Templo_final_y_mirador");
        if (temple == null) throw new InvalidOperationException("No se encontró la plataforma del mirador.");
        var existing = temple.Find(apronName);
        if (existing == null)
        {
            var floor = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPath);
            if (floor == null) throw new InvalidOperationException("No se encontró el módulo de piso P&W.");
            var tile = PrefabUtility.InstantiatePrefab(floor, temple) as GameObject;
            if (tile == null) throw new InvalidOperationException("No se pudo crear la extensión de descanso.");
            tile.name = apronName;
            tile.transform.rotation = Quaternion.identity;
            var rs = tile.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) throw new InvalidOperationException("El módulo de piso no tiene Renderer.");
            var bounds = rs[0].bounds;
            foreach (var renderer in rs.Skip(1)) bounds.Encapsulate(renderer.bounds);
            const float x = 0f, z = 71f, topY = 12.18f;
            tile.transform.position += new Vector3(x - bounds.center.x, topY - bounds.max.y, z - bounds.center.z);
            SetStatic(tile.transform);

            var support = new GameObject(apronName + "_Piso_transitable_a_cota");
            support.transform.SetParent(temple, false);
            support.transform.SetPositionAndRotation(new Vector3(x, topY - .04f, z), Quaternion.identity);
            support.AddComponent<BoxCollider>().size = new Vector3(8f, .08f, 8f);
            SetStatic(support.transform);
            EditorSceneManager.MarkSceneDirty(scene);
        }
            Debug.Log("Conexión del mirador: extensión P&W de 8 x 8 m y collider transitable a Y=12.18 m añadidos a Z=71 entre el final de la escalera y la plataforma. Iluminación sin cambios.");
    }

    static int RepairCentralStairwell(Transform gallery, GameObject floorAsset)
    {
        if (floorAsset == null) throw new InvalidOperationException("No se encontró el módulo de piso P&W.");
        var changed = 0;
        foreach (var oldName in new[]
        {
            "Galeria_superior_central_losa_1_1", "Galeria_superior_central_losa_1_1_Piso_transitable_a_cota",
            "Galeria_superior_central_losa_2_1", "Galeria_superior_central_losa_2_1_Piso_transitable_a_cota"
        })
        {
            var old = gallery.Find(oldName);
            if (old == null) continue;
            UnityEngine.Object.DestroyImmediate(old.gameObject, true);
            changed++;
        }

        foreach (var side in new[] { -1, 1 })
        {
            var x = side * 5f;
            changed += EnsureSurface(gallery, floorAsset,
                $"Hombro_apertura_escalera_galeria_central_{side}", x, 43.635f, 8.18f, 6f, 3.27f);
        }
        foreach (var side in new[] { -1, 1 })
        {
            var x = side * 4f;
            changed += EnsureSurface(gallery, floorAsset,
                $"Descanso_salida_escalera_galeria_central_{side}", x, 49.27f, 8.18f, 8f, 8f);
        }
        return changed;
    }

    static int EnsureSurface(Transform parent, GameObject floorAsset, string name, float x, float z, float topY, float width, float depth)
    {
        const float colliderThickness = .08f;
        var visualName = name;
        var supportName = name + "_Piso_transitable_a_cota";
        var visual = parent.Find(visualName);
        if (visual == null)
        {
            var tile = PrefabUtility.InstantiatePrefab(floorAsset, parent) as GameObject;
            if (tile == null) throw new InvalidOperationException("No se pudo crear " + visualName);
            tile.name = visualName;
            tile.transform.localScale = Vector3.Scale(tile.transform.localScale, new Vector3(width / 8f, 1f, depth / 8f));
            tile.transform.rotation = Quaternion.identity;
            var renderers = tile.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(visualName + " no tiene Renderer.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            tile.transform.position += new Vector3(x - bounds.center.x, topY - bounds.max.y, z - bounds.center.z);
            SetStatic(tile.transform);
            visual = tile.transform;
        }
        var support = parent.Find(supportName);
        if (support == null)
        {
            var colliderObject = new GameObject(supportName);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.SetPositionAndRotation(new Vector3(x, topY - colliderThickness * .5f, z), Quaternion.identity);
            colliderObject.AddComponent<BoxCollider>().size = new Vector3(width, colliderThickness, depth);
            SetStatic(colliderObject.transform);
        }
        return visual != null ? 1 : 0;
    }

    static void SetStatic(Transform transform)
    {
        transform.gameObject.isStatic = true;
        foreach (Transform child in transform) SetStatic(child);
    }
}
#endif
