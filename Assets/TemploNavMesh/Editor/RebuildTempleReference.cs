using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the old scattered FBX display objects into a playable, symmetrical
/// temple courtyard using the modular prefabs from (P&W) Temple Edition.
/// </summary>
public static class RebuildTempleReference
{
    const string RootName = "Templo_PW_NavMesh_Level";
    const string PrefabPath = "Assets/TemploNavMesh/Prefabs/TemploCompleto.prefab";
    const string NavMeshPath = "Assets/TemploNavMesh/Navigation/TemploNavMeshData.asset";
    const string StoneMaterialPath = "Assets/(P&W)Temple_Edition/FBX/Materials/Walls_1.mat";
    const string FloorMaterialPath = "Assets/TemploNavMesh/Materials/TempleFloorStone.mat";
    const string BackupScenePath = "Assets/TemploNavMesh/Backups/TemploNavMesh_PreReferenceRebuild.unity";
    const string BackupPrefabPath = "Assets/TemploNavMesh/Backups/TemploCompleto_GridDraft.prefab";
    const string BeforeFrontalLayoutSceneBackupPath = "Assets/TemploNavMesh/Backups/TemploNavMesh_BeforeFrontalLayout.unity";
    const string BeforeFrontalLayoutPrefabBackupPath = "Assets/TemploNavMesh/Backups/TemploCompleto_BeforeFrontalLayout.prefab";
    const string OldPrefabPath = "Assets/TemploNavMesh/Prefabs/TempleEnvironment.prefab";
    const string FloorPrefab = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Platforms/HD_Platform_6A.prefab";
    const string MainStairsPrefab = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Temple_Stairs_Entrance_1B.prefab";
    const string InnerStairsPrefab = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Temple_Stairs_Entrance_1A.prefab";
    const string SideEntrancePrefab = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Entrance_1A.prefab";
    const string SanctuaryPrefab = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Entrance_3B.prefab";
    const string RailPrefab = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Full_Railing_1.prefab";
    const string BarrierPrefab = "Assets/(P&W)Temple_Edition/Prefabs/Group_Walls/Barrier_Group_1A.prefab";
    const string TorchPrefab = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Small_Objects/Torch_Post_1A.prefab";
    const string BowlPrefab = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Small_Objects/Bowl_1A.prefab";
    const string MazeWallPrefab = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Wall_B.prefab";
    const string HeroPrefab = "Assets/Adventures_woman/Prefabs/Adventures_woman_02.prefab";
    const string GolemPrefab = "Assets/Mini Legion Rock Golem PBR HP Polyart/Prefabs/Polyart_Golem.prefab";

    const float LowerSurface = 0.5f;
    const float MiddleSurface = 2.567f;
    const float UpperSurface = 4.767f;
    const float GolemScale = 0.31f;

    [MenuItem("Tools/NavMesh Tutorial/Rebuild Temple from P&W Map Reference")]
    public static void Rebuild()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "TemploNavMesh")
        {
            EditorUtility.DisplayDialog("Abre la escena del templo", "Abre Assets/Scenes/TemploNavMesh.unity antes de reconstruir el mapa.", "OK");
            return;
        }

        var required = new[] { FloorPrefab, MainStairsPrefab, InnerStairsPrefab, SideEntrancePrefab, SanctuaryPrefab, RailPrefab, BarrierPrefab, TorchPrefab, BowlPrefab, MazeWallPrefab, HeroPrefab, GolemPrefab };
        var missing = required.Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path) == null).ToArray();
        if (missing.Length > 0)
        {
            EditorUtility.DisplayDialog("Faltan assets", "No encontré estos prefabs:\n\n" + string.Join("\n", missing), "OK");
            return;
        }

        Directory.CreateDirectory("Assets/TemploNavMesh/Backups");
        Directory.CreateDirectory("Assets/TemploNavMesh/Materials");
        Directory.CreateDirectory("Assets/TemploNavMesh/Prefabs");
        Directory.CreateDirectory("Assets/TemploNavMesh/Navigation");

        // Keep the previous, incorrect gallery arrangement recoverable.
        EditorSceneManager.SaveScene(scene);
        if (!File.Exists(BackupScenePath)) AssetDatabase.CopyAsset(scene.path, BackupScenePath);
        if (File.Exists(PrefabPath) && !File.Exists(BackupPrefabPath)) AssetDatabase.CopyAsset(PrefabPath, BackupPrefabPath);
        // Preserve the current valid NavMesh layout before correcting its width and camera angle.
        if (!File.Exists(BeforeFrontalLayoutSceneBackupPath)) AssetDatabase.CopyAsset(scene.path, BeforeFrontalLayoutSceneBackupPath);
        if (File.Exists(PrefabPath) && !File.Exists(BeforeFrontalLayoutPrefabBackupPath)) AssetDatabase.CopyAsset(PrefabPath, BeforeFrontalLayoutPrefabBackupPath);
        AssetDatabase.SaveAssets();

        var roots = scene.GetRootGameObjects();
        var cameraObject = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Select(camera => camera.gameObject).FirstOrDefault();
        var lightObject = roots.SelectMany(root => root.GetComponentsInChildren<Light>(true)).FirstOrDefault(light => light.type == LightType.Directional)?.gameObject;

        // Detach these scene-wide services before deleting an old environment root
        // so they cannot be destroyed with it.
        if (cameraObject != null) cameraObject.transform.SetParent(null, true);
        if (lightObject != null && lightObject != cameraObject) lightObject.transform.SetParent(null, true);

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root == cameraObject || root == lightObject) continue;
            Object.DestroyImmediate(root);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(OldPrefabPath) != null)
            AssetDatabase.DeleteAsset(OldPrefabPath);
        if (File.Exists("Assets/TemploNavMesh/PrefabDimensions.txt"))
            AssetDatabase.DeleteAsset("Assets/TemploNavMesh/PrefabDimensions.txt");

        var rootObject = new GameObject(RootName);
        var environment = new GameObject("Geometria_Estatica_NavMesh");
        environment.transform.SetParent(rootObject.transform, false);
        var surface = environment.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.12f;
        surface.minRegionArea = 1.5f;

        var lower = Group("00_Acceso_y_Plaza_Baja_48x24m", environment.transform);
        TileFloor(lower, new[] { -20f, -12f, -4f, 4f, 12f, 20f }, new[] { -15f, -7f, 1f }, LowerSurface);

        var stairs = Group("01_Escalinatas_Centrales", environment.transform);
        var middle = Group("02_Patio_Central_48x16m", environment.transform);
        TileFloor(middle, new[] { -20f, -12f, -4f, 4f, 12f, 20f }, new[] { 13f, 21f }, MiddleSurface);

        var architecture = Group("03_Arquitectura_Simetrica", environment.transform);
        var upper = Group("04_Terraza_Superior_40x16m", environment.transform);
        // The 5x2 grid spans X=-20..20 and Z=27..43 at Y=4.767 m.
        // The central 14 m lane is reserved for the level landing from z=27..28.
        TileFloor(upper, new[] { -16f, -8f, 0f, 8f, 16f }, new[] { 31f, 39f }, UpperSurface,
            frontLaneWidth: 14f, frontLaneEndZ: 28f);

        // The package's built structures use a large native scale. Bounds-centering
        // places their walkable ends flush with the three tiled levels.
        var mainStairs = PlaceScaled(MainStairsPrefab, stairs, "Escalinata_Principal_30m", new Vector3(0.45f, 0.34f, 0.34f), Quaternion.identity,
            new Vector3(0f, LowerSurface + 0.34f * (6.08f * 0.5f), 7f));
        // Bounds are 6.30 m deep; at 0.55 scale the stair ends at z=27.0,
        // flush with the upper terrace edge, while overlapping the middle floor.
        var innerStairs = PlaceScaled(InnerStairsPrefab, stairs, "Escalinata_Al_Santuario_15m", new Vector3(0.75f, 0.55f, 0.55f), Quaternion.identity,
            new Vector3(0f, MiddleSurface + 0.55f * (4.00f * 0.5f), 25.27f));
        DisableAuthoredColliders(mainStairs);
        DisableAuthoredColliders(innerStairs);
        // The rendered steps stay visible. These hidden, collidable ramps provide
        // a continuous walkable slope for the CharacterController and the bake.
        // End exactly at the middle terrace's front edge (z=9). The previous
        // 0.7 m overlap left a vertical lip that stopped the CharacterController.
        CreateNavMeshRamp(stairs, "Guia_NavMesh_Bajo_a_Patio", new Vector3(0f, LowerSurface, 4.3f),
            new Vector3(0f, MiddleSurface, 9f), 26f, 0.12f);
        // The upper ramp meets the terrace at z=27. A level, one-metre landing
        // extends the walkable top over the navmesh seam without a vertical lip.
        CreateNavMeshLanding(stairs, "Aterrizaje_NavMesh_Santuario", UpperSurface, 27f, 28f, 14f, 0.12f);
        CreateNavMeshRamp(stairs, "Guia_NavMesh_Patio_a_Santuario", new Vector3(0f, MiddleSurface, 21.8f),
            new Vector3(0f, UpperSurface, 27f), 14f, 0.12f);

        // Broaden the flanking entrances across X while keeping their walls upright;
        // the previous uniform shrink made each building read like a narrow vertical panel.
        var pavilionScale = new Vector3(0.8f, 0.4f, 0.65f);
        PlaceScaled(SideEntrancePrefab, architecture, "Pabellon_Lateral_Izquierdo", pavilionScale, Quaternion.identity,
            new Vector3(-15.5f, MiddleSurface + pavilionScale.y * (10.61f * 0.5f), 17f));
        PlaceScaled(SideEntrancePrefab, architecture, "Pabellon_Lateral_Derecho", pavilionScale, Quaternion.identity,
            new Vector3(15.5f, MiddleSurface + pavilionScale.y * (10.61f * 0.5f), 17f));
        Place(SanctuaryPrefab, architecture, "Templo_Central_Ensamblado", 0.30f, Quaternion.identity,
            new Vector3(0f, UpperSurface + 0.30f * (10.62f * 0.5f), 33f));

        var boundaries = Group("05_Barandas_y_Bordes", environment.transform);
        int railingCount = 0;
        foreach (var side in new[] { -1f, 1f })
        {
            foreach (var z in new[] { 11.25f, 15.75f, 20.25f, 24.75f })
            {
                Place(RailPrefab, boundaries, "Baranda_Plaza_" + (++railingCount), 1f, Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(side * 23.5f, MiddleSurface + 1.75f * 0.5f, z));
            }
            foreach (var z in new[] { -17f, -13.6f, -10.2f, -6.8f, -3.4f, 0f, 3.4f })
            {
                Place(BarrierPrefab, boundaries, "Murete_Acceso_" + (++railingCount), 1f, Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(side * 23.5f, LowerSurface + 1.85f * 0.5f, z));
            }
            foreach (var z in new[] { 27.25f, 31.75f, 36.25f, 40.75f })
            {
                Place(RailPrefab, boundaries, "Baranda_Terraza_" + (++railingCount), 1f, Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(side * 19.5f, UpperSurface + 1.75f * 0.5f, z));
            }
        }

        var details = Group("06_Antorchas_y_Recipientes", environment.transform);
        Place(TorchPrefab, details, "Antorcha_Entrada_Izquierda", 0.75f, Quaternion.identity,
            new Vector3(-21f, LowerSurface + 0.75f * (3.06f * 0.5f), 3f));
        Place(TorchPrefab, details, "Antorcha_Entrada_Derecha", 0.75f, Quaternion.identity,
            new Vector3(21f, LowerSurface + 0.75f * (3.06f * 0.5f), 3f));
        Place(TorchPrefab, details, "Antorcha_Santuario_Izquierda", 0.75f, Quaternion.identity,
            new Vector3(-16f, UpperSurface + 0.75f * (3.06f * 0.5f), 27f));
        Place(TorchPrefab, details, "Antorcha_Santuario_Derecha", 0.75f, Quaternion.identity,
            new Vector3(16f, UpperSurface + 0.75f * (3.06f * 0.5f), 27f));
        Place(BowlPrefab, details, "Recipiente_Izquierdo", 0.75f, Quaternion.identity,
            new Vector3(-18f, LowerSurface + 0.75f * (1.06f * 0.5f), -16f));
        Place(BowlPrefab, details, "Recipiente_Derecho", 0.75f, Quaternion.identity,
            new Vector3(18f, LowerSurface + 0.75f * (1.06f * 0.5f), -16f));

        BuildMiniMaze(environment.transform);

        var heroineGroup = new GameObject("01_Player_Aventurera_Protagonista");
        heroineGroup.transform.SetParent(rootObject.transform, false);
        var heroine = PlacePlayer(HeroPrefab, heroineGroup.transform, "Aventurera_Player",
            new Vector3(0f, LowerSurface, -12f));

        var golems = Group("02_Golems_NavMesh", rootObject.transform);
        var golemActors = new[]
        {
            PlaceGolem(GolemPrefab, golems, "Golem_01_Plaza_Izquierda", new Vector3(-12f, LowerSurface, -3f), surface.agentTypeID, 0),
            PlaceGolem(GolemPrefab, golems, "Golem_02_Plaza_Derecha", new Vector3(12f, LowerSurface, -8f), surface.agentTypeID, 1),
            PlaceGolem(GolemPrefab, golems, "Golem_03_Laberinto_Entrada", new Vector3(7.4f, MiddleSurface, 12.2f), surface.agentTypeID, 2),
            PlaceGolem(GolemPrefab, golems, "Golem_04_Laberinto_Corredor", new Vector3(7.4f, MiddleSurface, 16.3f), surface.agentTypeID, 3),
            PlaceGolem(GolemPrefab, golems, "Golem_05_Laberinto_Salida", new Vector3(7.4f, MiddleSurface, 19.2f), surface.agentTypeID, 4),
            PlaceGolem(GolemPrefab, golems, "Golem_06_Patio_Central", new Vector3(-7f, MiddleSurface, 12f), surface.agentTypeID, 5),
            PlaceGolem(GolemPrefab, golems, "Golem_07_Terraza_Izquierda", new Vector3(-16f, UpperSurface, 29f), surface.agentTypeID, 6),
            PlaceGolem(GolemPrefab, golems, "Golem_08_Terraza_Derecha", new Vector3(16f, UpperSurface, 37f), surface.agentTypeID, 7)
        };

        cameraObject = SetupCamera(cameraObject, rootObject.transform, heroine.transform);
        heroine.GetComponent<AventureraPlayerController>().SetCamera(cameraObject.transform);
        SetupLight(lightObject, rootObject.transform);

        // Bake only the temple geometry; actor colliders and models are outside the surface's children.
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null)
            AssetDatabase.DeleteAsset(NavMeshPath);
        surface.BuildNavMesh();
        if (surface.navMeshData == null)
            throw new System.InvalidOperationException("Unity no generó datos para NavMeshSurface.");
        surface.RemoveData();
        surface.navMeshData.name = "TemploNavMeshData";
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        EditorUtility.SetDirty(surface);
        surface.AddData();

        SnapGolemsToSurface(golemActors, surface.agentTypeID);

        var pathReport = CheckRoute(heroine.transform.position, surface.agentTypeID);
        pathReport += "\n" + CheckGolemRoutes(golemActors, heroine.transform.position, surface.agentTypeID);
        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootObject, PrefabPath);
        if (savedPrefab == null) throw new System.InvalidOperationException("No se pudo guardar TemploCompleto.prefab.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = rootObject;

        WriteLayoutReport(pathReport);
        EditorUtility.DisplayDialog("Templo reconstruido", 
            "Organicé el escenario como una composición frontal y simétrica, separé a la aventurera (Player) de los golems (NavMeshAgent), y añadí un mini laberinto de piedra.\n\n" +
            "Escena: Assets/Scenes/TemploNavMesh.unity\n" +
            "Prefab completo: " + PrefabPath + "\n" +
            "Huella del patio: 48 m de ancho x 62 m de largo; terrazas conectadas por dos escalinatas.\n" +
            "Aventurera: CharacterController y gravedad; cámara de tercera persona. Golems: 8 instancias de un prefab, cada uno con NavMeshAgent y persecución.\n" +
            "Golems: ~2.06 m (escala 0.31).\n\n" + pathReport,
            "OK");
    }

    [MenuItem("Tools/NavMesh Tutorial/Reload Saved TemploNavMesh Scene")]
    public static void ReloadSavedScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TemploNavMesh.unity", OpenSceneMode.Single);
    }

    [MenuItem("Tools/NavMesh Tutorial/Diagnose NavMesh Queries")]
    public static void DiagnoseNavMeshQueries()
    {
        var root = GameObject.Find(RootName);
        var surface = root != null ? root.GetComponentInChildren<NavMeshSurface>(true) : null;
        var heroine = root != null ? root.GetComponentsInChildren<CharacterController>(true).FirstOrDefault(controller => controller.name == "Aventurera_Player") : null;
        var golem = root != null ? root.GetComponentsInChildren<NavMeshAgent>(true).FirstOrDefault(agent => agent.name == "Golem_04_Laberinto_Corredor") : null;
        var triangulation = NavMesh.CalculateTriangulation();
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (triangulation.vertices.Length > 0)
        {
            bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (var vertex in triangulation.vertices.Skip(1)) bounds.Encapsulate(vertex);
        }

        var report = new StringBuilder();
        report.AppendLine($"NavMeshSurface hallada={surface != null}, activa={surface != null && surface.enabled}, tipo={surface?.agentTypeID}, data={(surface != null && surface.navMeshData != null ? surface.navMeshData.name : "nula")}");
        report.AppendLine($"Triangulación: {triangulation.vertices.Length} vértices; límites X={bounds.min.x:F2}..{bounds.max.x:F2}, Y={bounds.min.y:F2}..{bounds.max.y:F2}, Z={bounds.min.z:F2}..{bounds.max.z:F2}.");
        if (heroine != null) report.AppendLine(SampleDiagnostics("Aventurera (CharacterController, sin NavMeshAgent)", heroine.transform.position, surface != null ? surface.agentTypeID : 0));
        if (golem != null) report.AppendLine(SampleDiagnostics("Golem central", golem.transform.position, surface != null ? surface.agentTypeID : 0));
        report.AppendLine(SampleDiagnostics("Centro nivel bajo", new Vector3(0f, LowerSurface, -15f), surface != null ? surface.agentTypeID : 0));
        report.AppendLine(SampleDiagnostics("Centro nivel medio", new Vector3(0f, MiddleSurface, 13f), surface != null ? surface.agentTypeID : 0));
        report.AppendLine(SampleDiagnostics("Centro nivel alto", new Vector3(0f, UpperSurface, 29f), surface != null ? surface.agentTypeID : 0));
        if (heroine != null && golem != null)
        {
            var middlePoint = new Vector3(0f, MiddleSurface, 13f);
            var upperPoint = new Vector3(0f, UpperSurface, 29f);
            report.AppendLine(PathDiagnostics("Bajo → medio", heroine.transform.position, middlePoint));
            report.AppendLine(PathDiagnostics("Medio → alto", middlePoint, upperPoint));
            report.AppendLine(PathDiagnostics("Aventurera → golem", heroine.transform.position, golem.transform.position));
        }
        report.AppendLine(StairSamples("Escalinata baja", new[] { 4.6f, 5f, 5.5f, 6f, 6.5f, 7f, 7.5f, 8f, 8.5f, 9f, 9.4f }, LowerSurface, MiddleSurface));
        report.AppendLine(StairSamples("Escalinata alta", new[] { 23.2f, 23.5f, 24f, 24.5f, 25f, 25.5f, 26f, 26.5f, 26.8f }, MiddleSurface, UpperSurface));

        EditorUtility.DisplayDialog("Diagnóstico de NavMesh", report.ToString(), "OK");
    }

    static string PathDiagnostics(string label, Vector3 start, Vector3 end)
    {
        var path = new NavMeshPath();
        var calculated = NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
        var corners = string.Join(" → ", path.corners.Select(corner => $"({corner.x:F1},{corner.y:F1},{corner.z:F1})"));
        return $"Ruta {label}: calculada={calculated}, estado={path.status}, esquinas={path.corners.Length}; {corners}";
    }

    static string StairSamples(string label, float[] zValues, float lowHeight, float highHeight)
    {
        var samples = new StringBuilder(label + ": ");
        foreach (var z in zValues)
        {
            var t = Mathf.InverseLerp(zValues[0], zValues[zValues.Length - 1], z);
            var y = Mathf.Lerp(lowHeight, highHeight, t);
            var found = NavMesh.SamplePosition(new Vector3(0f, y, z), out var hit, 1.5f, NavMesh.AllAreas);
            samples.Append(found
                ? $"z{z:F1}→({hit.position.y:F1},{hit.position.z:F1}) "
                : $"z{z:F1}→fuera ");
        }
        return samples.ToString();
    }

    static string SampleDiagnostics(string label, Vector3 point, int agentTypeID)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
        var shortFilter = NavMesh.SamplePosition(point, out var shortFilterHit, 2.5f, filter);
        var longFilter = NavMesh.SamplePosition(point, out var longFilterHit, 100f, filter);
        var defaultQuery = NavMesh.SamplePosition(point, out var defaultHit, 100f, NavMesh.AllAreas);
        var filtered = longFilter
            ? $"filtro(100m) sí, y={longFilterHit.position.y:F2}, d={longFilterHit.distance:F2}"
            : "filtro(100m) no";
        var normal = defaultQuery
            ? $"consulta(100m) sí, y={defaultHit.position.y:F2}, d={defaultHit.distance:F2}"
            : "consulta(100m) no";
        return $"{label} en ({point.x:F2},{point.y:F2},{point.z:F2}): filtro(2.5m)={shortFilter}; {filtered}; {normal}.";
    }

    static Transform Group(string name, Transform parent)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    static void TileFloor(Transform parent, float[] xCenters, float[] zCenters, float surfaceHeight,
        float frontLaneWidth = 0f, float frontLaneEndZ = 0f)
    {
        var material = GetFloorMaterial();
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "Base_Solida_Continua_NavMesh";
        slab.transform.SetParent(parent, false);
        slab.transform.position = new Vector3(0f, surfaceHeight - 0.35f, zCenters.Average());
        slab.transform.localScale = new Vector3(xCenters.Length * 8f, 0.5f, zCenters.Length * 8f);
        slab.GetComponent<Renderer>().sharedMaterial = material;
        // Every HD_Platform_6A tile has a full 8x8 m mesh collider. Keeping a
        // second collider 0.1 m below those tiles creates stacked walkable faces.
        var foundationCollider = slab.GetComponent<Collider>();
        if (foundationCollider != null) Object.DestroyImmediate(foundationCollider);
        SetStatic(slab.transform);

        // Hidden box colliders exactly at the visible tile height provide the
        // physics/NavMesh surface; detailed tile mesh colliders are redundant.
        var floorWidth = xCenters.Length * 8f;
        var floorStartZ = zCenters.Min() - 4f;
        var floorEndZ = zCenters.Max() + 4f;
        var supportCenterY = surfaceHeight - 0.05f;
        if (frontLaneWidth <= 0f)
        {
            CreateFloorSupport(parent, "Collider_Piso_Continuo_Alineado",
                new Vector3(0f, supportCenterY, (floorStartZ + floorEndZ) * 0.5f),
                new Vector3(floorWidth, 0.1f, floorEndZ - floorStartZ));
        }
        else
        {
            var laneEndZ = Mathf.Clamp(frontLaneEndZ, floorStartZ, floorEndZ);
            var laneDepth = laneEndZ - floorStartZ;
            var sideWidth = (floorWidth - frontLaneWidth) * 0.5f;
            CreateFloorSupport(parent, "Collider_Piso_Continuo_Alineado",
                new Vector3(0f, supportCenterY, (laneEndZ + floorEndZ) * 0.5f),
                new Vector3(floorWidth, 0.1f, floorEndZ - laneEndZ));
            foreach (var side in new[] { -1f, 1f })
            {
                CreateFloorSupport(parent, side < 0f ? "Collider_Ala_Izquierda_Alineada" : "Collider_Ala_Derecha_Alineada",
                    new Vector3(side * (frontLaneWidth * 0.5f + sideWidth * 0.5f), supportCenterY,
                        (floorStartZ + laneEndZ) * 0.5f),
                    new Vector3(sideWidth, 0.1f, laneDepth));
            }
        }

        int row = 0;
        foreach (var z in zCenters)
        {
            int column = 0;
            foreach (var x in xCenters)
            {
                var tile = Place(FloorPrefab, parent, "Baldosa_" + row + "_" + column, 1f, Quaternion.identity,
                    new Vector3(x, surfaceHeight - 0.15f, z));
                foreach (var tileCollider in tile.GetComponentsInChildren<Collider>(true))
                    tileCollider.enabled = false;
                column++;
            }
            row++;
        }
    }

    static void CreateFloorSupport(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var floorSupport = new GameObject(name);
        floorSupport.transform.SetParent(parent, false);
        floorSupport.transform.position = center;
        floorSupport.AddComponent<BoxCollider>().size = size;
        SetStatic(floorSupport.transform);
    }

    static GameObject CreateNavMeshLanding(Transform parent, string name, float surfaceHeight,
        float startZ, float endZ, float width, float thickness)
    {
        var landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        landing.name = name;
        landing.transform.SetParent(parent, false);
        landing.transform.position = new Vector3(0f, surfaceHeight - thickness * 0.5f, (startZ + endZ) * 0.5f);
        landing.transform.localScale = new Vector3(width, thickness, endZ - startZ);
        var renderer = landing.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        SetStatic(landing.transform);
        return landing;
    }

    static void BuildMiniMaze(Transform environment)
    {
        var maze = Group("07_Laberinto_Mini_Patio_Central", environment);
        var segmentScale = new Vector3(1f, 1.35f, 0.30f);
        var wallCenterY = MiddleSurface + 1.35f;
        var gateCentersX = new[] { -8f, -4f, 4f, 8f };

        foreach (var x in gateCentersX)
        {
            PlaceScaled(MazeWallPrefab, maze, $"Muro_Entrada_{x:+0;-0}", segmentScale, Quaternion.identity,
                new Vector3(x, wallCenterY, 10.8f));
            PlaceScaled(MazeWallPrefab, maze, $"Muro_Salida_{x:+0;-0}", segmentScale, Quaternion.identity,
                new Vector3(x, wallCenterY, 24f));
        }

        foreach (var side in new[] { -1f, 1f })
        {
            foreach (var z in new[] { 12.9f, 17.1f, 21.3f, 25.5f })
                PlaceScaled(MazeWallPrefab, maze, $"Muro_Lateral_{(side < 0f ? "Oeste" : "Este")}_{z:F1}",
                    segmentScale, Quaternion.Euler(0f, 90f, 0f), new Vector3(side * 10.1f, wallCenterY, z));
        }

        foreach (var x in new[] { -8f, -4f, 0f, 4f })
        {
            PlaceScaled(MazeWallPrefab, maze, $"Laberinto_Tramo_1_{x:+0;-0}", segmentScale, Quaternion.identity,
                new Vector3(x, wallCenterY, 14f));
            PlaceScaled(MazeWallPrefab, maze, $"Laberinto_Tramo_2_{x:+0;-0}", segmentScale, Quaternion.identity,
                new Vector3(x, wallCenterY, 18.2f));
        }

        PlaceScaled(MazeWallPrefab, maze, "Laberinto_Divisor_Oeste", segmentScale, Quaternion.Euler(0f, 90f, 0f),
            new Vector3(4f, wallCenterY, 16.1f));
        PlaceScaled(MazeWallPrefab, maze, "Laberinto_Divisor_Este", segmentScale, Quaternion.Euler(0f, 90f, 0f),
            new Vector3(9.8f, wallCenterY, 16.1f));
        foreach (var x in new[] { 4f, 8f })
            PlaceScaled(MazeWallPrefab, maze, $"Laberinto_Tramo_3_{x:+0;-0}", segmentScale, Quaternion.identity,
                new Vector3(x, wallCenterY, 20.3f));
    }

    static void DisableAuthoredColliders(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }

    static void SnapGolemsToSurface(GameObject[] golems, int agentTypeID)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
        foreach (var golem in golems)
        {
            if (NavMesh.SamplePosition(golem.transform.position, out var hit, 1.5f, filter))
                golem.transform.position = hit.position;
            else
                Debug.LogError($"{golem.name} no encontró el NavMesh cerca del punto de aparición {golem.transform.position}.", golem);
        }
    }

    static GameObject CreateNavMeshRamp(Transform parent, string name, Vector3 lowPoint, Vector3 highPoint,
        float width, float thickness)
    {
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = name;
        ramp.transform.SetParent(parent, false);
        var delta = highPoint - lowPoint;
        var run = new Vector2(delta.x, delta.z).magnitude;
        var slopeAngle = Mathf.Atan2(delta.y, run) * Mathf.Rad2Deg;
        var slopeLength = Mathf.Sqrt(run * run + delta.y * delta.y);
        ramp.transform.position = (lowPoint + highPoint) * 0.5f - Vector3.up * (thickness * 0.5f * Mathf.Cos(slopeAngle * Mathf.Deg2Rad));
        ramp.transform.rotation = Quaternion.Euler(-Mathf.Atan2(delta.y, delta.z) * Mathf.Rad2Deg, 0f, 0f);
        ramp.transform.localScale = new Vector3(width, thickness, slopeLength);
        var renderer = ramp.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        SetStatic(ramp.transform);
        return ramp;
    }

    static Material GetFloorMaterial()
    {
        var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
        if (floorMaterial == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(StoneMaterialPath);
            if (source == null) throw new System.InvalidOperationException("No se encontró el material de piedra Walls_1 del paquete.");
            floorMaterial = new Material(source) { name = "TempleFloorStone" };
            AssetDatabase.CreateAsset(floorMaterial, FloorMaterialPath);
        }
        floorMaterial.mainTextureScale = new Vector2(8f, 6f);
        EditorUtility.SetDirty(floorMaterial);
        return floorMaterial;
    }

    static GameObject Place(string path, Transform parent, string name, float uniformScale, Quaternion rotation, Vector3 boundsCenter)
    {
        return PlaceScaled(path, parent, name, Vector3.one * uniformScale, rotation, boundsCenter);
    }

    static GameObject PlaceScaled(string path, Transform parent, string name, Vector3 scale, Quaternion rotation, Vector3 boundsCenter)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var instance = PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
        if (instance == null) throw new System.InvalidOperationException("No se pudo instanciar " + path);
        instance.name = name;
        instance.transform.localScale = scale;
        instance.transform.rotation = rotation;
        var current = GetBounds(instance);
        instance.transform.position += boundsCenter - current.center;
        SetStatic(instance.transform);
        return instance;
    }

    static GameObject PlacePlayer(string path, Transform parent, string name, Vector3 footPosition)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new System.InvalidOperationException("No se encontró " + path);
        var actor = new GameObject(name);
        actor.tag = "Player";
        actor.transform.SetParent(parent, false);
        actor.transform.position = footPosition;
        var model = PrefabUtility.InstantiatePrefab(asset, actor.transform) as GameObject;
        if (model == null) throw new System.InvalidOperationException("No se pudo instanciar " + path);
        model.name = "Modelo_" + asset.name;
        model.transform.localScale = Vector3.one;
        var bounds = GetBounds(model);
        var targetCenter = new Vector3(footPosition.x, footPosition.y + bounds.extents.y, footPosition.z);
        model.transform.position += targetCenter - bounds.center;

        DisableAuthoredColliders(model);
        var animator = model.GetComponentInChildren<Animator>(true);
        var humanoidAvatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Adventures_woman/Mesh/Adventures_woman.FBX")
            .OfType<Avatar>()
            .FirstOrDefault(avatar => avatar.isHuman);
        if (animator != null)
        {
            animator.avatar = humanoidAvatar;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        if (humanoidAvatar == null)
            Debug.LogError("No se encontró el Avatar Humanoid de Adventures_woman.FBX; la pose de movimiento no podrá aplicarse.");

        var controller = actor.AddComponent<CharacterController>();
        controller.radius = 0.34f;
        controller.height = 2.04f;
        controller.center = new Vector3(0f, 1.02f, 0f);
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.25f;
        controller.skinWidth = 0.04f;
        controller.minMoveDistance = 0f;
        actor.AddComponent<AventureraPlayerController>();
        actor.AddComponent<AventureraLocomotionPose>();
        actor.AddComponent<AventureraRouteAudit>();
        return actor;
    }

    static GameObject PlaceGolem(string path, Transform parent, string name, Vector3 footPosition, int agentTypeID, int orbitSlot)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new System.InvalidOperationException("No se encontró " + path);
        var actor = new GameObject(name);
        actor.transform.SetParent(parent, false);
        actor.transform.position = footPosition;
        var model = PrefabUtility.InstantiatePrefab(asset, actor.transform) as GameObject;
        if (model == null) throw new System.InvalidOperationException("No se pudo instanciar " + path);
        model.name = "Modelo_Polyart_Golem";
        model.transform.localScale = Vector3.one * GolemScale;
        var bounds = GetBounds(model);
        var targetCenter = new Vector3(footPosition.x, footPosition.y + bounds.extents.y, footPosition.z);
        model.transform.position += targetCenter - bounds.center;

        // The NavMeshAgent remains the only movement authority; this kinematic body
        // lets its solid capsule take part in contacts without gravity or force drift.
        var body = actor.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var solidCollider = actor.AddComponent<CapsuleCollider>();
        solidCollider.radius = 0.68f;
        solidCollider.height = 1.8f;
        solidCollider.center = new Vector3(0f, 0.9f, 0f);
        solidCollider.isTrigger = false;

        var agent = actor.AddComponent<NavMeshAgent>();
        agent.agentTypeID = agentTypeID;
        agent.radius = 0.68f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.speed = 2.8f;
        agent.acceleration = 9f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 1.1f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.areaMask = NavMesh.AllAreas;
        var chase = actor.AddComponent<GolemChasePlayer>();
        chase.ConfigureOrbitSlot(orbitSlot, 8);
        var chaseProperties = new SerializedObject(chase);
        chaseProperties.FindProperty("orbitSlot").intValue = orbitSlot;
        chaseProperties.FindProperty("orbitSlotCount").intValue = 8;
        chaseProperties.FindProperty("approachRadius").floatValue = 3.2f;
        chaseProperties.FindProperty("minimumPlayerClearance").floatValue = 2.4f;
        chaseProperties.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(chase);
        return actor;
    }

    static GameObject SetupCamera(GameObject cameraObject, Transform parent, Transform target)
    {
        if (cameraObject == null) cameraObject = new GameObject("Main Camera");
        cameraObject.name = "Main Camera";
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = target.position + new Vector3(0f, 2.6f, -6.5f);
        cameraObject.transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * 1.4f - cameraObject.transform.position, Vector3.up);
        var camera = cameraObject.GetComponent<Camera>();
        if (camera == null) camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 300f;
        var follower = cameraObject.GetComponent<ThirdPersonAdventureCamera>();
        if (follower == null) follower = cameraObject.AddComponent<ThirdPersonAdventureCamera>();
        follower.Target = target;
        follower.Distance = 8.0f;
        follower.Pitch = 42f;
        return cameraObject;
    }

    static void SetupLight(GameObject lightObject, Transform parent)
    {
        if (lightObject == null) lightObject = new GameObject("Directional Light");
        lightObject.name = "Directional Light";
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = lightObject.GetComponent<Light>();
        if (light == null) light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
    }

    static void SetStatic(Transform root)
    {
        root.gameObject.isStatic = true;
        foreach (Transform child in root)
            SetStatic(child);
    }

    static Bounds GetBounds(GameObject root)
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
        if (!found) throw new System.InvalidOperationException(root.name + " no tiene Renderer ni Collider.");
        return bounds;
    }

    static string CheckRoute(Vector3 start, int agentTypeID)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
        var triangulation = NavMesh.CalculateTriangulation();
        var checkpoints = new (string label, Vector3 position)[]
        {
            ("baja oeste", new Vector3(-19f, LowerSurface, -16f)),
            ("baja este", new Vector3(19f, LowerSurface, -16f)),
            ("rampa baja", new Vector3(0f, 1.5f, 7f)),
            ("entrada del laberinto", new Vector3(0f, MiddleSurface, 12f)),
            ("corredor este del laberinto", new Vector3(7.4f, MiddleSurface, 16f)),
            ("salida del laberinto", new Vector3(0f, MiddleSurface, 22f)),
            ("terraza alta al pie de la rampa", new Vector3(0f, UpperSurface, 27.2f)),
            ("terraza superior izquierda", new Vector3(-16f, UpperSurface, 29f)),
            ("terraza superior derecha", new Vector3(16f, UpperSurface, 37f))
        };

        var report = new StringBuilder($"NavMesh: {triangulation.vertices.Length} vértices triangulados. ");
        var previous = start;
        var allComplete = true;
        foreach (var checkpoint in checkpoints)
        {
            var fromFound = NavMesh.SamplePosition(previous, out var fromHit, 2.5f, filter);
            var toFound = NavMesh.SamplePosition(checkpoint.position, out var toHit, 2.5f, filter);
            var complete = false;
            var corners = 0;
            if (fromFound && toFound)
            {
                var path = new NavMeshPath();
                complete = NavMesh.CalculatePath(fromHit.position, toHit.position, filter, path) && path.status == NavMeshPathStatus.PathComplete;
                corners = path.corners.Length;
            }
            report.Append($"\n- {checkpoint.label}: origen={fromFound}, destino={toFound}, rutaCompleta={complete}, esquinas={corners}.");
            allComplete &= fromFound && toFound && complete;
            previous = checkpoint.position;
        }
        report.Append(allComplete ? "\nConectividad NavMesh de todos los segmentos: APROBADA." : "\nConectividad NavMesh: HAY PUNTOS QUE REVISAR.");
        report.Append(" Esta comprobación calcula rutas; la prueba física con CharacterController se ejecuta en Play Mode con F8.");
        return report.ToString();
    }

    static string CheckGolemRoutes(GameObject[] golems, Vector3 playerPosition, int agentTypeID)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
        var playerFound = NavMesh.SamplePosition(playerPosition, out var playerHit, 2.5f, filter);
        var allComplete = playerFound;
        var report = new StringBuilder("Rutas NavMesh de cada golem hacia la Player:");

        foreach (var golem in golems)
        {
            var startFound = NavMesh.SamplePosition(golem.transform.position, out var golemHit, 2.5f, filter);
            var path = new NavMeshPath();
            var calculated = startFound && playerFound && NavMesh.CalculatePath(golemHit.position, playerHit.position, filter, path);
            var complete = calculated && path.status == NavMeshPathStatus.PathComplete;
            allComplete &= complete;
            report.Append($"\n- {golem.name} → Player: origen={startFound}, rutaCompleta={complete}, estado={(calculated ? path.status.ToString() : "No calculada")}, esquinas={path.corners.Length}.");
        }

        report.Append(allComplete
            ? "\nConectividad de persecución de los 8 golems: APROBADA."
            : "\nConectividad de persecución: HAY GOLEMS SIN RUTA COMPLETA.");
        return report.ToString();
    }

    static void WriteLayoutReport(string pathReport)
    {
        var report = new StringBuilder();
        report.AppendLine("P&W Temple Edition — montaje del mapa para NavMesh");
        report.AppendLine("Composición frontal y simétrica basada en la referencia del paquete P&W Temple Edition. El santuario usa Entrance_3B (15.7 m de ancho x 5.6 m de fondo a escala 0.30), dejando un paseo amplio por delante y por ambos lados.");
        report.AppendLine("Huella del patio: 48 m de ancho x 62 m de largo (Z = -19 a 43); losas modulares HD_Platform_6A colocadas horizontales sobre terrazas de piedra continuas con collider.");
        report.AppendLine("Niveles transitables: 0.50 m / 2.567 m / 4.767 m.");
        report.AppendLine("Player: Aventurera; CharacterController radius 0.34 m, height 2.04 m, stepOffset 0.25 m, slopeLimit 45°. Gravedad/movimiento propio y locomoción Humanoid procedural; NO tiene NavMeshAgent ni Rigidbody.");
        report.AppendLine("Cámara: tercera persona elevada detrás de la aventurera, seguimiento, órbita con botón derecho y rueda para zoom; colisión con el escenario y los golems.");
        report.AppendLine("Golems: 8 instancias del mismo prefab Polyart_Golem; escala 0.31; NavMeshAgent y cápsula física sólida radius 0.68 m, height 1.80 m, Rigidbody cinemático sin gravedad, velocidad 2.8 m/s y persecución en una formación distribuida a 3.2 m del tag Player, con 2.4 m de separación mínima.");
        report.AppendLine("Mini laberinto: muros modulares P&W Unique_Wall_B, altura montada ~2.70 m, aberturas centrales de entrada/salida y recorrido en zigzag en el patio intermedio.");
        report.AppendLine("Superficie NavMesh: solo la jerarquía Geometria_Estatica_NavMesh; Geometry = Physics Colliders; voxel = 0.12 m.");
        report.AppendLine("Cada terraza tiene colliders de soporte alineados con sus baldosas; en la terraza superior se dividen alrededor del canal de 14 m de la escalinata, y un descanso invisible de 14 x 1 m completa la unión al mismo nivel. Los colliders redundantes de baldosas, bases visuales y escalinatas decorativas están desactivados. Rampas invisibles con BoxCollider conectan físicamente las tres alturas.");
        report.AppendLine("Ruta de prueba: " + pathReport);
        report.AppendLine("Auditoría física Player: entrar en Play Mode y pulsar F8 para recorrer los 28 destinos con CharacterController y comprobar grounded y NavMesh.SamplePosition.");
        File.WriteAllText("Assets/TemploNavMesh/TempleLayout.txt", report.ToString());
        AssetDatabase.Refresh();
    }
}
