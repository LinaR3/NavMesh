using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a self-contained architecture review scene. A timestamped copy of
/// the currently open draft is saved before its scene objects are replaced.
/// The production NavMesh scene and the rejected older prefab are untouched.
/// </summary>
public static class BuildTempleReview
{
    const string ScenePath = "Assets/Scenes/NewSceneTemplo_Review.unity";
    const string RootName = "Templo_Maya_Referencia_Arquitectura";
    const string PrefabPath = "Assets/TemploNavMesh/Prefabs/TemploReferenciaMaya.prefab";
    const string Floor = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Platforms/HD_Platform_6A.prefab";
    const string MainStairs = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Temple_Stairs_Entrance_1B.prefab";
    const string MidStairs = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Temple_Stairs_Entrance_1A.prefab";
    const string SideStairs = "Assets/(P&W)Temple_Edition/Prefabs/Basic/Platforms/Stairs_Platform_1A.prefab";
    const string TempleHall = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Entrance_3B.prefab";
    const string SideShrine = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Structures/Entrance_1A.prefab";
    const string Wall = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Walls/HD_Wall_2A.prefab";
    const string MazeWall = "Assets/(P&W)Temple_Edition/Prefabs/Unique/Unique_Wall_B.prefab";
    const string Arch = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Archs/HD_Arch_1A.prefab";
    const string Column = "Assets/(P&W)Temple_Edition/Prefabs/Basic/Walls/Basic_Pillar_1A.prefab";
    const string Bowl = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Small_Objects/Bowl_1A.prefab";
    const string Torch = "Assets/(P&W)Temple_Edition/Prefabs/High_Detailed/Small_Objects/Torch_Post_1A.prefab";
    const string StonePath = "Assets/(P&W)Temple_Edition/FBX/Materials/Walls_1.mat";
    const string TerrainDataPath = "Assets/TemploNavMesh/Environment/TemploMayaTerrain.asset";
    const string RampMaterialPath = "Assets/TemploNavMesh/Environment/TemploMayaStone.mat";
    const string TreeOak = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Oak_Tree.prefab";
    const string TreePine = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Pine_Tree.prefab";
    const string Grass1 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_1.prefab";
    const string Grass2 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_2.prefab";
    const string Grass3 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_3.prefab";
    const string Grass4 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_4.prefab";
    const string Rock1 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Rock_1.prefab";
    const string Rock2 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Rock_2.prefab";
    const string Rock3 = "Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Rock_3.prefab";
    const string RockLayerSource = "Assets/ZNS3D/FREE_VEGETATION_PACK/TEXTURES/TERRAIN/ROCKY_GROUND/NewLayer.terrainlayer";
    const string SoilLayerSource = "Assets/ZNS3D/FREE_VEGETATION_PACK/TEXTURES/TERRAIN/SOIL_GROUND/NewLayer.terrainlayer";
    const string GrassLayerSource = "Assets/ZNS3D/FREE_VEGETATION_PACK/TEXTURES/TERRAIN/GRASS/NewLayer.terrainlayer";
    const string RockLayerPath = "Assets/TemploNavMesh/Environment/TemploMayaRock.terrainlayer";
    const string SoilLayerPath = "Assets/TemploNavMesh/Environment/TemploMayaSoil.terrainlayer";
    const string GrassLayerPath = "Assets/TemploNavMesh/Environment/TemploMayaGrass.terrainlayer";

    const float Level0 = 0f;
    const float Level1 = 6.08f;
    const float Level2 = 10.08f;
    const float Level3 = 14.08f;
    const float TileHalf = 4f;
    const float WallLength = 2.2f;

    [MenuItem("Tools/NavMesh Tutorial/Build Maya Temple Review (new composition)")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Abre la escena de revisión", "Esta composición solo se monta en Assets/Scenes/NewSceneTemplo_Review.unity.", "OK");
            return;
        }

        var required = new[] { Floor, MainStairs, MidStairs, SideStairs, TempleHall, SideShrine, Wall, MazeWall, Arch, Column, Bowl, Torch };
        var missing = required.Concat(new[] { TreeOak, TreePine, Grass1, Grass2, Grass3, Grass4, Rock1, Rock2, Rock3 })
            .Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p) == null).ToArray();
        var vegetationMissing = new[] { RockLayerSource, SoilLayerSource, GrassLayerSource }.Where(p => AssetDatabase.LoadAssetAtPath<TerrainLayer>(p) == null).ToArray();
        if (missing.Length > 0 || vegetationMissing.Length > 0)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Faltan assets", string.Join("\n", missing.Concat(vegetationMissing)), "OK");
            return;
        }

        if (!SaveTimestampedDraft(scene))
        {
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("No se pudo respaldar", "No continuaré sin conservar la escena previa.", "OK");
            return;
        }

        foreach (var go in scene.GetRootGameObjects().ToArray())
            if (!go.GetComponent<Camera>() && !go.GetComponent<Light>()) Undo.DestroyObjectImmediate(go);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Crear composición maya de revisión");
        var terrainRoot = Group("00_Terreno_y_Montanas_Cenote", root.transform);
        var terrain = BuildTerrain(terrainRoot);
        var architecture = Group("01_Arquitectura_PW_y_Niveles_Transitables", root.transform);

        BuildWalkableFloors(architecture);
        BuildGreatStairSequence(architecture);
        BuildTerraceWalls(architecture);
        BuildTempleCourts(architecture);
        BuildMayaPortalAndArcade(architecture);
        BuildMaze(architecture);
        BuildSideRoutesAndDetails(architecture);
        BuildJungle(root.transform, terrain);
        var audit = AuditGeometry(root);
        ConfigureCameraAndLighting(root.transform);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        if (prefab == null) throw new InvalidOperationException("No se pudo crear el prefab de revisión nuevo.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText("Assets/TemploNavMesh/TempleReferenceGeometryAudit.txt", audit);
        AssetDatabase.ImportAsset("Assets/TemploNavMesh/TempleReferenceGeometryAudit.txt");
        Selection.activeGameObject = root;
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Composición nueva lista", "Creé una escena nueva e independiente con un gran patio maya escalonado, terreno de valle y laderas, escalinatas modulares, rampas caminables, terrazas niveladas, corredor, portal, laberinto y vegetación. El estado previo quedó respaldado con fecha. TemploNavMesh y TemploCompleto no se tocaron.", "OK");
    }

    public static void BatchBuild()
    {
        try
        {
            EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("No se pudo crear la escena de revisión vacía.");
            Build();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) throw new InvalidOperationException("El prefab final no quedó guardado.");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    static bool SaveTimestampedDraft(Scene scene)
    {
        var backup = "Assets/Scenes/NewSceneTemplo_Respaldo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        var folder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(folder)) return false;
        // saveAsCopy persists unsaved in-memory changes without changing active scene path.
        return EditorSceneManager.SaveScene(scene, backup, true);
    }

    static Terrain BuildTerrain(Transform parent)
    {
        EnsureFolder("Assets/TemploNavMesh/Environment");
        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (data == null)
        {
            data = new TerrainData { heightmapResolution = 513, size = new Vector3(300f, 80f, 230f) };
            AssetDatabase.CreateAsset(data, TerrainDataPath);
        }
        data.heightmapResolution = 513;
        data.size = new Vector3(300f, 80f, 230f);
        var heights = new float[513, 513];
        for (var z = 0; z < 513; z++)
        {
            var wz = -95f + z / 512f * 230f;
            for (var x = 0; x < 513; x++)
            {
                var wx = -150f + x / 512f * 300f;
                var y = CanyonHeight(wx, wz);
                heights[z, x] = Mathf.Clamp01((y + 10f) / 80f);
            }
        }
        data.SetHeights(0, 0, heights);
        var layers = new[]
        {
            CopyTerrainLayer(RockLayerSource, RockLayerPath, "Roca y grava del valle", 10f),
            CopyTerrainLayer(SoilLayerSource, SoilLayerPath, "Tierra húmeda de la selva", 13f),
            CopyTerrainLayer(GrassLayerSource, GrassLayerPath, "Pasto de las laderas", 8f)
        };
        data.terrainLayers = layers;
        data.alphamapResolution = 256;
        var alpha = new float[256, 256, 3];
        for (var z = 0; z < 256; z++)
        {
            var wz = -95f + z / 255f * 230f;
            for (var x = 0; x < 256; x++)
            {
                var wx = -150f + x / 255f * 300f;
                var mountain = Mathf.Clamp01((Mathf.Abs(wx) - 92f) / 38f + Mathf.Max(0f, (wz - 64f) / 70f));
                var grass = Mathf.Lerp(.04f, .52f, Smooth01(mountain));
                var soil = Mathf.Lerp(.09f, .28f, Smooth01(mountain));
                alpha[z, x, 2] = grass;
                alpha[z, x, 1] = soil;
                alpha[z, x, 0] = 1f - grass - soil;
            }
        }
        data.SetAlphamaps(0, 0, alpha);
        data.SetBaseMapDirty();
        EditorUtility.SetDirty(data);
        var terrainGO = Terrain.CreateTerrainGameObject(data);
        terrainGO.name = "Laderas_calizas_esculpidas_con_valle_central_en_Y0";
        terrainGO.transform.SetParent(parent, false);
        terrainGO.transform.position = new Vector3(-150f, -10f, -95f);
        terrainGO.isStatic = true;
        return terrainGO.GetComponent<Terrain>();
    }

    static TerrainLayer CopyTerrainLayer(string sourcePath, string destinationPath, string name, float tileSize)
    {
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(destinationPath);
        if (layer == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<TerrainLayer>(sourcePath);
            if (source == null) throw new InvalidOperationException("No se encontró la capa de terreno " + sourcePath);
            layer = new TerrainLayer
            {
                name = name,
                diffuseTexture = source.diffuseTexture,
                normalMapTexture = source.normalMapTexture,
                maskMapTexture = source.maskMapTexture,
                metallic = source.metallic,
                smoothness = source.smoothness,
                tileSize = new Vector2(tileSize, tileSize),
                tileOffset = source.tileOffset,
                normalScale = source.normalScale
            };
            AssetDatabase.CreateAsset(layer, destinationPath);
        }
        return layer;
    }

    static float CanyonHeight(float x, float z)
    {
        // Continuous flat central shelf, then steep limestone walls and irregular karst ridges.
        var ax = Mathf.Max(0f, (Mathf.Abs(x) - 122f) / 26f);
        var side = Smooth01(ax) * 34f;
        var rearT = Mathf.Max(0f, (z - 68f) / 46f);
        var rear = Smooth01(rearT) * 24f;
        var frontT = Mathf.Max(0f, (-z - 91f) / 30f);
        var front = Smooth01(frontT) * 18f;
        var ridge = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - 111f) / 15f, 2f)) * 14f;
        var secondary = Mathf.Exp(-Mathf.Pow((z - 93f) / 23f, 2f)) * Mathf.Exp(-Mathf.Pow(x / 90f, 2f)) * 13f;
        var rough = (Mathf.Sin(x * .105f + z * .063f) + Mathf.Sin(z * .19f - x * .04f) * .55f) *
                    Mathf.Clamp01((Mathf.Abs(x) - 48f) / 28f) * 1.2f;
        return Mathf.Max(0f, side + rear + front + ridge + secondary + rough);
    }

    static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    static void BuildWalkableFloors(Transform parent)
    {
        var floors = Group("01_Pisos_PW_Con_Juntas_Alineadas", parent);
        // Main approach at Y=0: exact top surface, long enough to read as a real plaza.
        TileRect(floors, "Explanada_Baja_Y0", -96f, 96f, -94f, -50f, Level0);
        // Three broad, nested terraces. Each grid is snapped to the same 8 m module.
        TileRect(floors, "Terraza_Monumental_Y6_08", -80f, 80f, -34f, -2f, Level1);
        TileRect(floors, "Patio_de_columnas_Y10_08", -64f, 64f, 2f, 34f, Level2);
        TileRect(floors, "Santuario_superior_Y14_08", -48f, 48f, 38f, 70f, Level3);
        // A side promenade and two large courts continue out from the main processional axis.
        TileRect(floors, "Plaza_lateral_oeste_Y0", -112f, -96f, -38f, 26f, Level0);
        TileRect(floors, "Plaza_lateral_este_Y0", 96f, 112f, -38f, 26f, Level0);
        TileRect(floors, "Andador_oeste_elevado", -96f, -80f, -30f, 30f, Level1);
        TileRect(floors, "Andador_este_elevado", 80f, 96f, -30f, 30f, Level1);
        TileRect(floors, "Andador_oeste_patío_a_santuario", -64f, -48f, 34f, 70f, Level2);
        TileRect(floors, "Andador_este_patío_a_santuario", 48f, 64f, 34f, 70f, Level2);
    }

    static void TileRect(Transform parent, string name, float minX, float maxX, float minZ, float maxZ, float topY)
    {
        var group = Group(name, parent);
        var countX = Mathf.FloorToInt((maxX - minX) / 8f + .0001f);
        var countZ = Mathf.FloorToInt((maxZ - minZ) / 8f + .0001f);
        var firstX = (minX + maxX) * .5f - (countX - 1) * 4f;
        var firstZ = (minZ + maxZ) * .5f - (countZ - 1) * 4f;
        var row = 0;
        for (var rz = 0; rz < countZ; rz++, row++)
        {
            var col = 0;
            var z = firstZ + rz * 8f;
            for (var cx = 0; cx < countX; cx++, col++)
            {
                var x = firstX + cx * 8f;
                var tile = PlaceBoundsCenter(Floor, group, "Losa_" + row + "_" + col,
                    Vector3.one, Quaternion.identity, new Vector3(x, topY - .15f, z));
                // Keep the source P&W MeshCollider: floor seams and the NavMesh surface share geometry.
                SetStatic(tile.transform);
            }
        }
    }

    static void BuildGreatStairSequence(Transform parent)
    {
        var sequence = Group("02_Escalinatas_En_Varios_Tramos_y_Rampas", parent);
        // Broad source stair module rises exactly 6.08 m from the ground floor to the first terrace.
        PlaceBoundsCenter(MainStairs, sequence, "Escalinata_principal_ancha_y_simetrica",
            Vector3.one, Quaternion.identity, new Vector3(0f, Level1 * .5f, -41f));
        // Three visual stair flights from reference: centered inner flight plus two set-back flank flights.
        PlaceBoundsCenter(MidStairs, sequence, "Escalera_central_al_patío_alto",
            Vector3.one, Quaternion.identity, new Vector3(0f, (Level1 + Level2) * .5f, 1f));
        PlaceBoundsCenter(SideStairs, sequence, "Escalera_lateral_oeste_con_descanso",
            Vector3.one, Quaternion.Euler(0f, 180f, 0f), new Vector3(-42f, (Level1 + Level2) * .5f, 2f));
        PlaceBoundsCenter(SideStairs, sequence, "Escalera_lateral_este_con_descanso",
            Vector3.one, Quaternion.identity, new Vector3(42f, (Level1 + Level2) * .5f, 2f));
        PlaceBoundsCenter(MidStairs, sequence, "Escalera_final_al_santuario",
            new Vector3(.9f, 1f, 1f), Quaternion.identity, new Vector3(0f, (Level2 + Level3) * .5f, 37f));

        // Gentle stone ramps flank the monumental stairs and connect the actual terrace heights.
        CreateRamp(sequence, "Rampa_oeste_explanada_a_terraza", new Vector3(-112f, Level0, -18f), new Vector3(-80f, Level1, -18f), 12f);
        CreateRamp(sequence, "Rampa_este_explanada_a_terraza", new Vector3(112f, Level0, -18f), new Vector3(80f, Level1, -18f), 12f);
        CreateRamp(sequence, "Rampa_oeste_terraza_a_patío", new Vector3(-96f, Level1, 19f), new Vector3(-64f, Level2, 19f), 10f);
        CreateRamp(sequence, "Rampa_este_terraza_a_patío", new Vector3(96f, Level1, 19f), new Vector3(64f, Level2, 19f), 10f);
        CreateRamp(sequence, "Rampa_oeste_patío_a_santuario", new Vector3(-64f, Level2, 54f), new Vector3(-48f, Level3, 54f), 9f);
        CreateRamp(sequence, "Rampa_este_patío_a_santuario", new Vector3(64f, Level2, 54f), new Vector3(48f, Level3, 54f), 9f);
    }

    static void CreateRamp(Transform parent, string name, Vector3 low, Vector3 high, float width)
    {
        var horizontal = new Vector2(high.x - low.x, high.z - low.z);
        var run = horizontal.magnitude;
        var rise = high.y - low.y;
        var thickness = .48f;
        // The ramp top is a real sloped mesh with its own MeshCollider, not an invisible navigation cheat.
        var mesh = new Mesh { name = name + "_Malla" };
        mesh.vertices = new[]
        {
            new Vector3(-width/2, -rise/2, -run/2), new Vector3(width/2, -rise/2, -run/2),
            new Vector3(-width/2, rise/2, run/2), new Vector3(width/2, rise/2, run/2),
            new Vector3(-width/2, -rise/2-thickness, -run/2), new Vector3(width/2, -rise/2-thickness, -run/2),
            new Vector3(-width/2, rise/2-thickness, run/2), new Vector3(width/2, rise/2-thickness, run/2)
        };
        mesh.triangles = new[]
        {
            0,2,1, 1,2,3, 4,5,6, 5,7,6,
            0,4,2, 2,4,6, 1,3,5, 3,7,5,
            0,1,4, 1,5,4, 2,6,3, 3,6,7
        };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = (low + high) * .5f;
        var angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        var yaw = Mathf.Atan2(horizontal.x, horizontal.y) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-angle, Vector3.right);
        // Pitch locally first, then orient that axis along the chosen world-space route.
        var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = GetStoneMaterial();
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        go.isStatic = true;
    }

    static void BuildTerraceWalls(Transform parent)
    {
        var walls = Group("03_Muros_de_contencion_sin_huecos_entre_modulos", parent);
        RetainingTier(walls, "Contencion_terraza_1", -82f, 82f, -34f, -2f, Level0, Level1, 35f, -18f);
        RetainingTier(walls, "Contencion_patío_2", -66f, 66f, 2f, 34f, Level1, Level2, 51f, 19f);
        RetainingTier(walls, "Contencion_santuario_3", -50f, 50f, 38f, 70f, Level2, Level3, 22f, 54f);
    }

    static void RetainingTier(Transform parent, string name, float left, float right, float frontZ, float backZ, float baseY, float topY, float opening, float sideRouteZ)
    {
        var vertical = (topY - baseY) / 4f;
        for (var z = frontZ + WallLength * .5f; z < backZ; z += WallLength)
        {
            if (Mathf.Abs(z - sideRouteZ) < 8f) continue;
            WallAt(parent, name + "_Oeste_" + z, new Vector3(left, baseY, z), new Vector3(1f, vertical, 1f), 90f);
            WallAt(parent, name + "_Este_" + z, new Vector3(right, baseY, z), new Vector3(1f, vertical, 1f), 90f);
        }
        for (var x = left + WallLength * .5f; x < right; x += WallLength)
        {
            if (Mathf.Abs(x) > opening) WallAt(parent, name + "_Frente_" + x, new Vector3(x, baseY, frontZ), new Vector3(1f, vertical, 1f), 0f);
            WallAt(parent, name + "_Fondo_" + x, new Vector3(x, baseY, backZ), new Vector3(1f, vertical, 1f), 0f);
        }
    }

    static void WallAt(Transform parent, string name, Vector3 baseCenter, Vector3 scale, float yaw)
    {
        PlaceFloorAligned(Wall, parent, name, scale, Quaternion.Euler(0f, yaw, 0f), baseCenter);
    }

    static void BuildTempleCourts(Transform parent)
    {
        var courts = Group("04_Patios_laterales_y_galerias", parent);
        // Twin lateral shrines repeat the photo's paired stone structures, facing into the ceremonial axis.
        var west = PlaceFloorAligned(SideShrine, courts, "Santuario_lateral_oeste", new Vector3(.9f, 1.05f, 1f), Quaternion.identity, new Vector3(-44f, Level2, 18f));
        var east = PlaceFloorAligned(SideShrine, courts, "Santuario_lateral_este", new Vector3(.9f, 1.05f, 1f), Quaternion.Euler(0f, 180f, 0f), new Vector3(44f, Level2, 18f));
        SetStatic(west.transform); SetStatic(east.transform);

        // Outer colonnades use regular 4.2 m bays and leave a 14 m clear walking aisle.
        for (var z = 8f; z <= 30f; z += 7.3f)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * 57f;
                PlaceFloorAligned(Column, courts, "Pilar_galeria_" + side + "_" + z, new Vector3(1.15f, 1.2f, 1.15f), Quaternion.identity, new Vector3(x, Level2, z));
                PlaceBoundsCenter(Arch, courts, "Arco_de_galeria_" + side + "_" + z, new Vector3(1.4f, 1f, 1f), Quaternion.identity, new Vector3(x, Level2 + 6.4f, z));
            }
        }
    }

    static void BuildMayaPortalAndArcade(Transform parent)
    {
        var shrine = Group("05_Santuario_maya_y_portal_de_piedra", parent);
        PlaceFloorAligned(TempleHall, shrine, "Gran_salon_abierto_de_columnas", new Vector3(1f, 1f, 1f), Quaternion.identity, new Vector3(0f, Level3, 54f));
        PlaceFloorAligned(SideShrine, shrine, "Portal_piramidal_maya", new Vector3(1.05f, 1.15f, 1f), Quaternion.identity, new Vector3(0f, Level3, 69f));
        // Paired monumental bowls and torches emphasize the primary route without blocking it.
        foreach (var side in new[] { -1f, 1f })
        {
            PlaceFloorAligned(Torch, shrine, "Antorcha_del_santuario_" + side, Vector3.one * 1.15f, Quaternion.identity, new Vector3(side * 29f, Level3, 41f));
            PlaceFloorAligned(Bowl, shrine, "Cuenco_del_santuario_" + side, Vector3.one * 1.35f, Quaternion.identity, new Vector3(side * 35f, Level3, 38f));
        }
        // Central stepping-stone line keeps the processional aisle visible and navigable.
        PlaceFloorAligned(Arch, shrine, "Arco_de_acceso_al_santuario", new Vector3(1.25f, 1.15f, 1f), Quaternion.identity, new Vector3(0f, Level3, 41f));
    }

    static void BuildMaze(Transform parent)
    {
        var maze = Group("06_Laberinto_de_piedra_con_ruta_legible", parent);
        const float ox = -102f;
        const float oz = 6f;
        const float halfW = 18.9f;
        const float halfD = 18f;
        const float pitch = 4.2f;
        var y = Level0;
        // Outer perimeter: a clear entrance in the near side and a different exit to the far court.
        for (var x = -16.8f; x <= 16.8f; x += pitch)
        {
            if (Mathf.Abs(x + 8.4f) > 3.6f) PlaceFloorAligned(MazeWall, maze, "Perimetro_sur_" + x, Vector3.one, Quaternion.identity, new Vector3(ox + x, y, oz - halfD));
            if (Mathf.Abs(x - 8.4f) > 3.6f) PlaceFloorAligned(MazeWall, maze, "Perimetro_norte_" + x, Vector3.one, Quaternion.identity, new Vector3(ox + x, y, oz + halfD));
        }
        for (var z = -14f; z <= 14f; z += pitch)
        {
            PlaceFloorAligned(MazeWall, maze, "Perimetro_oeste_" + z, Vector3.one, Quaternion.Euler(0f, 90f, 0f), new Vector3(ox - halfW, y, oz + z));
            PlaceFloorAligned(MazeWall, maze, "Perimetro_este_" + z, Vector3.one, Quaternion.Euler(0f, 90f, 0f), new Vector3(ox + halfW, y, oz + z));
        }
        // Offset inner walls create alternating turns, 4.2 m portals and a continuous west-to-east route.
        var rows = new[] { -10.5f, -3.5f, 3.5f, 10.5f };
        var gapsByRow = new[] { 2, 6, 1, 5 };
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < 8; c++)
            {
                if (c == gapsByRow[r] || c == gapsByRow[r] - 1) continue;
                var x = -14.7f + c * pitch;
                PlaceFloorAligned(MazeWall, maze, "Muro_intercalado_fila_" + r + "_" + c, Vector3.one, Quaternion.identity, new Vector3(ox + x, y, oz + rows[r]));
            }
        }
        // Short perpendicular returns guide turns but do not seal any of the four passage openings.
        foreach (var p in new[] { new Vector2(-9.5f, -7f), new Vector2(6.5f, -0.7f), new Vector2(-5.5f, 6.3f), new Vector2(10f, 12f) })
            PlaceFloorAligned(MazeWall, maze, "Giro_de_labyrinth_" + p.x + "_" + p.y, Vector3.one, Quaternion.Euler(0f, 90f, 0f), new Vector3(ox + p.x, y, oz + p.y));
    }

    static void BuildSideRoutesAndDetails(Transform parent)
    {
        var detail = Group("07_Detalle_de_bordes_y_pasos", parent);
        foreach (var level in new[] { Level1, Level2, Level3 })
        {
            var z0 = level == Level1 ? -30f : level == Level2 ? 5f : 41f;
            var z1 = level == Level1 ? -4f : level == Level2 ? 31f : 67f;
            var x = level == Level1 ? 76f : level == Level2 ? 60f : 44f;
            for (var side = -1; side <= 1; side += 2)
            {
                for (var z = z0; z <= z1; z += 2.2f)
                {
                    if (Mathf.Abs(z - (level == Level1 ? -18f : level == Level2 ? 19f : 54f)) < 8f) continue;
                    PlaceFloorAligned(Wall, detail, "Pretil_macizo_" + level + "_" + side + "_" + z, new Vector3(1f, .36f, 1f), Quaternion.Euler(0f, 90f, 0f), new Vector3(side * x, level, z));
                }
            }
        }
        foreach (var side in new[] { -1f, 1f })
        {
            for (var z = -84f; z <= -60f; z += 12f)
            {
                PlaceFloorAligned(Column, detail, "Columna_plaza_baja_" + side + "_" + z, Vector3.one * 1.1f, Quaternion.identity, new Vector3(side * 88f, Level0, z));
                PlaceFloorAligned(Bowl, detail, "Cuenco_plaza_baja_" + side + "_" + z, Vector3.one * 1.2f, Quaternion.identity, new Vector3(side * 92f, Level0, z));
            }
        }
    }

    static void BuildJungle(Transform root, Terrain terrain)
    {
        var vegetation = Group("08_Selva_repartida_sin_tapar_escalinatas", root);
        var treePaths = new[] { TreeOak, TreePine };
        var spots = new[]
        {
            new Vector2(-72,-82),new Vector2(-92,-70),new Vector2(-118,-62),new Vector2(-130,-34),new Vector2(-122,4),new Vector2(-116,37),new Vector2(-125,69),new Vector2(-104,95),
            new Vector2(-80,84),new Vector2(-70,62),new Vector2(-73,34),new Vector2(-78,-10),new Vector2(-78,-58),
            new Vector2(72,-82),new Vector2(95,-69),new Vector2(118,-55),new Vector2(130,-28),new Vector2(120,8),new Vector2(115,41),new Vector2(130,73),new Vector2(105,96),
            new Vector2(80,83),new Vector2(70,61),new Vector2(75,34),new Vector2(78,-8),new Vector2(79,-56),
            new Vector2(-52,-14),new Vector2(52,-14),new Vector2(-48,28),new Vector2(48,28),new Vector2(-40,63),new Vector2(40,63),
            new Vector2(-88,50),new Vector2(89,51),new Vector2(-99,-26),new Vector2(102,-25)
        };
        for (var i = 0; i < spots.Length; i++)
        {
            var x = spots[i].x;
            var z = spots[i].y;
            var atCourtEdge = Mathf.Abs(x) < 56f;
            var y = atCourtEdge ? (z < 0 ? Level1 : z < 40 ? Level2 : Level3) : CanyonHeight(x, z);
            var path = treePaths[(i + (i / 3)) % treePaths.Length];
            var scale = (i % 5 == 0) ? 1.35f : (i % 4 == 0 ? 1.18f : 1f);
            PlaceFloorAligned(path, vegetation, "Arbol_jungla_" + (i + 1), Vector3.one * scale,
                Quaternion.Euler(0f, (i * 47) % 360, 0f), new Vector3(x, y, z));
        }

        var random = new System.Random(7319);
        var grassPaths = new[] { Grass1, Grass2, Grass3, Grass4 };
        for (var i = 0; i < 190; i++)
        {
            var x = (float)(random.NextDouble() * 280 - 140);
            var z = (float)(random.NextDouble() * 210 - 90);
            // Grass follows the laderas and courtyard planting strips; keep the walkable route clear.
            if (Mathf.Abs(x) < 52f && z > -55f && z < 74f) continue;
            if (Mathf.Abs(x) > 118f && z < -72f) continue;
            var s = .55f + (float)random.NextDouble() * .75f;
            PlaceFloorAligned(grassPaths[i % grassPaths.Length], vegetation, "Pasto_y_maleza_de_ladera_" + i,
                Vector3.one * s, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), new Vector3(x, CanyonHeight(x, z), z));
        }
    }

    static void ConfigureCameraAndLighting(Transform root)
    {
        var camera = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>(true)).FirstOrDefault(x => x.CompareTag("MainCamera"));
        if (camera != null)
        {
            var target = new Vector3(0f, 10f, 3f);
            camera.transform.position = new Vector3(0f, 76f, -166f);
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
            camera.fieldOfView = 49f;
            camera.farClipPlane = 600f;
        }
        var sun = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Light>(true)).FirstOrDefault(x => x.type == LightType.Directional);
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(34f, -30f, 0f);
            sun.color = new Color(1f, .79f, .63f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.41f, .39f, .35f);
        RenderSettings.ambientEquatorColor = new Color(.28f, .27f, .23f);
        RenderSettings.ambientGroundColor = new Color(.13f, .12f, .1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.47f, .40f, .32f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .0019f;
    }

    static string AuditGeometry(GameObject root)
    {
        var floors = root.GetComponentsInChildren<MeshCollider>(true).Length;
        var ramps = root.GetComponentsInChildren<MeshCollider>(true).Where(c => c.name.StartsWith("Rampa_")).ToArray();
        var levels = new[] { Level0, Level1, Level2, Level3 };
        return "REVISIÓN GEOMÉTRICA — templo maya\n" +
               "Plataformas con MeshCollider (incluye pisos y laberinto): " + floors + "\n" +
               "Niveles de diseño (m): " + string.Join(", ", levels.Select(v => v.ToString("0.00"))) + "\n" +
               "Rampas: " + ramps.Length + " (malla y MeshCollider en cada una)\n" +
               "Terreno: 300 x 230 m; explanada central nominal en Y=0\n" +
               "Nota: inspección estática de colocación; NavMesh pendiente de la etapa posterior de aprobación.";
    }

    static GameObject PlaceFloorAligned(string path, Transform parent, string name, Vector3 scale, Quaternion rotation, Vector3 xzAndFloorY)
    {
        var instance = InstantiateAsset(path, parent, name, scale, rotation);
        var b = BoundsOf(instance);
        instance.transform.position += new Vector3(xzAndFloorY.x - b.center.x, xzAndFloorY.y - b.min.y, xzAndFloorY.z - b.center.z);
        SetStatic(instance.transform);
        return instance;
    }

    static GameObject PlaceBoundsCenter(string path, Transform parent, string name, Vector3 scale, Quaternion rotation, Vector3 boundsCenter)
    {
        var instance = InstantiateAsset(path, parent, name, scale, rotation);
        instance.transform.position += boundsCenter - BoundsOf(instance).center;
        SetStatic(instance.transform);
        return instance;
    }

    static GameObject InstantiateAsset(string path, Transform parent, string name, Vector3 scale, Quaternion rotation)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
        if (go == null) throw new InvalidOperationException("No se pudo instanciar " + path);
        go.name = name;
        go.transform.localScale = scale;
        go.transform.rotation = rotation;
        return go;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException(root.name + " no tiene Renderer.");
        var b = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    static Material GetStoneMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RampMaterialPath);
        if (mat != null) return mat;
        var source = AssetDatabase.LoadAssetAtPath<Material>(StonePath);
        if (source == null) throw new InvalidOperationException("No está Walls_1.mat del paquete P&W.");
        mat = new Material(source) { name = "TemploMayaPiedra_Rampas" };
        mat.mainTextureScale = new Vector2(12f, 12f);
        AssetDatabase.CreateAsset(mat, RampMaterialPath);
        return mat;
    }

    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void SetStatic(Transform root)
    {
        root.gameObject.isStatic = true;
        foreach (Transform child in root) SetStatic(child);
    }
}
