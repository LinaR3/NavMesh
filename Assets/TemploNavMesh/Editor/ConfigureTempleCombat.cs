using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds combat and presentation to the existing temple prefab without rebuilding
/// or relocating any of the authored geometry.
/// </summary>
public static class ConfigureTempleCombat
{
    const string TemplePrefabPath = "Assets/TemploNavMesh/Prefabs/TemploCompleto.prefab";
    const string GolemModelPath = "Assets/Mini Legion Rock Golem PBR HP Polyart/Prefabs/Polyart_Golem.prefab";
    const string GolemActorPath = "Assets/TemploNavMesh/Prefabs/Golem_NavMesh_Actor.prefab";
    const string PistolPath = "Assets/Adventures_woman/Prefabs/Pistol.prefab";
    const string FoliageMaterialPath = "Assets/TemploNavMesh/Materials/TempleFoliage.mat";
    const float MiddleSurface = 2.567f;
    const float UpperSurface = 4.767f;

    [MenuItem("Tools/NavMesh Tutorial/Apply Combat and Visual Polish")]
    public static void Apply()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "TemploNavMesh")
        {
            EditorUtility.DisplayDialog("Abre la escena del templo", "Abre Assets/Scenes/TemploNavMesh.unity antes de aplicar el combate.", "OK");
            return;
        }

        var missing = new[] { TemplePrefabPath, GolemModelPath, PistolPath }
            .Where(path => AssetDatabase.LoadAssetAtPath<Object>(path) == null).ToArray();
        if (missing.Length > 0)
        {
            EditorUtility.DisplayDialog("Faltan assets", "No encontré estos recursos:\n\n" + string.Join("\n", missing), "OK");
            return;
        }

        if (!EditorSceneManager.SaveScene(activeScene))
            throw new System.InvalidOperationException("Unity no pudo guardar la escena actual antes de pulir el prefab.");

        var prefabRoot = PrefabUtility.LoadPrefabContents(TemplePrefabPath);
        try
        {
            var player = FindChild(prefabRoot.transform, "Aventurera_Player")?.gameObject;
            var golemGroup = FindChild(prefabRoot.transform, "02_Golems_NavMesh");
            var surface = prefabRoot.GetComponentInChildren<NavMeshSurface>(true);
            if (player == null || golemGroup == null || surface == null)
                throw new System.InvalidOperationException("El prefab actual necesita Aventurera_Player, 02_Golems_NavMesh y NavMeshSurface.");

            var actorPrefab = CreateGolemActorPrefab(prefabRoot.transform, surface.agentTypeID);
            SetupExistingGolems(prefabRoot, actorPrefab);
            var camera = prefabRoot.GetComponentsInChildren<Camera>(true).FirstOrDefault(cam => cam.CompareTag("MainCamera"))
                         ?? prefabRoot.GetComponentInChildren<Camera>(true);
            SetupPlayerAndPistol(player, camera);
            SetupMazeWave(prefabRoot.transform, player.transform, golemGroup, actorPrefab);
            SetupTorchLights(prefabRoot.transform);
            SetupFoliage(prefabRoot.transform);

            var saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, TemplePrefabPath);
            if (saved == null) throw new System.InvalidOperationException("No se pudo guardar el prefab del templo.");
            EditorUtility.SetDirty(saved);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
        SetupSceneLighting();
        ApplyCombatToActiveScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = GameObject.Find("Templo_PW_NavMesh_Level");

        EditorUtility.DisplayDialog("Combate y presentación listos",
            "Apliqué los cambios al campo de la escena y actualicé el prefab completo sin reconstruir ni mover su geometría.\n\n" +
            "• Clic izquierdo dispara al centro de la cámara; clic derecho apunta y mueve la cámara.\n" +
            "• La pistola queda unida a la mano derecha; cada gólem resiste 3 impactos.\n" +
            "• Al entrar al laberinto aparece una oleada de 3 gólems desde la terraza superior, en el mismo prefab y sobre el NavMesh.\n" +
            "• Se añadieron luces cálidas junto a las antorchas y agaves decorativos sin colliders.\n\n" +
            "La NavMesh existente se conserva; las plantas y luces quedan fuera de sus geometrías de horneado.",
            "OK");
    }

    static void ApplyCombatToActiveScene()
    {
        var root = GameObject.Find("Templo_PW_NavMesh_Level");
        if (root == null) throw new System.InvalidOperationException("No encuentro Templo_PW_NavMesh_Level en la escena activa.");
        var player = FindChild(root.transform, "Aventurera_Player")?.gameObject;
        var golemGroup = FindChild(root.transform, "02_Golems_NavMesh");
        var surface = root.GetComponentInChildren<NavMeshSurface>(true);
        if (player == null || golemGroup == null || surface == null)
            throw new System.InvalidOperationException("La escena necesita Aventurera_Player, 02_Golems_NavMesh y NavMeshSurface.");

        AlignGolem08Model(root.transform);
        var actorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GolemActorPath);
        if (actorPrefab == null) throw new System.InvalidOperationException("No se guardó el prefab reutilizable Golem_NavMesh_Actor.");

        SetupExistingGolems(root, actorPrefab);
        var camera = root.GetComponentsInChildren<Camera>(true).FirstOrDefault(cam => cam.CompareTag("MainCamera"))
                     ?? root.GetComponentInChildren<Camera>(true);
        SetupPlayerAndPistol(player, camera);
        SetupMazeWave(root.transform, player.transform, golemGroup, actorPrefab);
        SetupTorchLights(root.transform);
        SetupFoliage(root.transform);

        // The scene has a serialized NavMeshData asset, but its surface component
        // was disabled. Reactivate the data before considering any rebake.
        surface.enabled = true;
        EditorUtility.SetDirty(surface);
        surface.AddData();
        var activeTriangles = NavMesh.CalculateTriangulation().indices.Length / 3;
        if (activeTriangles == 0)
        {
            surface.BuildNavMesh();
            var generatedData = surface.navMeshData;
            if (generatedData != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(generatedData)))
            {
                var dataPath = AssetDatabase.GenerateUniqueAssetPath("Assets/TemploNavMesh/Navigation/TemploNavMeshData_Baked.asset");
                generatedData.name = "TemploNavMeshData_Baked";
                AssetDatabase.CreateAsset(generatedData, dataPath);
                surface.navMeshData = generatedData;
                EditorUtility.SetDirty(surface);
                AssetDatabase.SaveAssets();
            }
            activeTriangles = NavMesh.CalculateTriangulation().indices.Length / 3;
        }

        // Keep the promised full-field prefab synchronized with the user-authored
        // scene rather than an older parallel copy of the map.
        var sceneRootPrefab = PrefabUtility.SaveAsPrefabAsset(root, TemplePrefabPath);
        if (sceneRootPrefab == null) throw new System.InvalidOperationException("No pude sincronizar el prefab TemploCompleto con la escena activa.");
        EditorUtility.SetDirty(sceneRootPrefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Templo] Combate y grounding aplicados al Player y a {golemGroup.GetComponentsInChildren<NavMeshAgent>(true).Length} gólems. NavMesh activo: {activeTriangles} triángulos.", root);
    }

    static void AlignGolem08Model(Transform root)
    {
        var outlier = FindChild(root, "Golem_08_Terraza_Derecha");
        var reference = FindChild(root, "Golem_07_Terraza_Izquierda");
        if (outlier == null || reference == null) return;
        var outlierModel = outlier.Find("Modelo_Polyart_Golem");
        var referenceModel = reference.Find("Modelo_Polyart_Golem");
        if (outlierModel == null || referenceModel == null) return;

        Undo.RecordObject(outlierModel, "Alinear modelo del gólem con su cápsula");
        outlierModel.localPosition = referenceModel.localPosition;
        PrefabUtility.RecordPrefabInstancePropertyModifications(outlierModel);
        EditorUtility.SetDirty(outlierModel);
    }

    static void SetupExistingGolems(GameObject root, GameObject actorPrefab)
    {
        var player = FindChild(root.transform, "Aventurera_Player");
        foreach (var chase in root.GetComponentsInChildren<GolemChasePlayer>(true))
        {
            chase.ConfigureEngagement(1.8f, 1.18f);
            if (player != null) chase.Target = player;
            var agent = chase.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.stoppingDistance = 0.08f;
                agent.radius = 0.68f;
                agent.height = 1.8f;
                agent.baseOffset = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
            var solid = chase.GetComponent<CapsuleCollider>();
            if (solid != null)
            {
                solid.isTrigger = false;
                solid.direction = 1;
                solid.radius = 0.68f;
                solid.height = 1.8f;
                solid.center = new Vector3(0f, 0.9f, 0f);
            }
            var body = chase.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            if (chase.GetComponent<GolemHealth>() == null)
                chase.gameObject.AddComponent<GolemHealth>();
            EditorUtility.SetDirty(chase);
        }
    }

    static GameObject CreateGolemActorPrefab(Transform temporaryParent, int agentTypeID)
    {
        var oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GolemActorPath);
        var temporaryActor = new GameObject("Golem_NavMesh_Actor");
        temporaryActor.transform.SetParent(temporaryParent, false);
        temporaryActor.transform.localPosition = new Vector3(0f, -30f, 0f);

        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GolemModelPath);
        var model = PrefabUtility.InstantiatePrefab(modelAsset, temporaryActor.transform) as GameObject;
        if (model == null) throw new System.InvalidOperationException("No se pudo crear el modelo de gólem para el prefab reutilizable.");
        model.name = "Modelo_Polyart_Golem";
        model.transform.localScale = Vector3.one * 0.31f;
        var modelBounds = GetBounds(model);
        var desiredCenter = temporaryActor.transform.position + Vector3.up * modelBounds.extents.y;
        model.transform.position += desiredCenter - modelBounds.center;

        var body = temporaryActor.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var solidCollider = temporaryActor.AddComponent<CapsuleCollider>();
        solidCollider.radius = 0.68f;
        solidCollider.height = 1.8f;
        solidCollider.center = new Vector3(0f, 0.9f, 0f);
        solidCollider.direction = 1;
        solidCollider.isTrigger = false;

        var agent = temporaryActor.AddComponent<NavMeshAgent>();
        agent.agentTypeID = agentTypeID;
        agent.radius = 0.68f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.speed = 2.8f;
        agent.acceleration = 9f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.08f;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        var chase = temporaryActor.AddComponent<GolemChasePlayer>();
        chase.ConfigureOrbitSlot(0, 12);
        chase.ConfigureEngagement(1.8f, 1.18f);
        temporaryActor.AddComponent<GolemHealth>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(temporaryActor, GolemActorPath);
        if (prefab == null) throw new System.InvalidOperationException("No se pudo guardar Golem_NavMesh_Actor.prefab.");
        Object.DestroyImmediate(temporaryActor);
        if (oldPrefab != null) EditorUtility.SetDirty(prefab);
        return prefab;
    }

    static Transform SetupPlayerAndPistol(GameObject player, Camera camera)
    {
        var animator = player.GetComponentInChildren<Animator>(true);
        var humanoidAvatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Adventures_woman/Mesh/Adventures_woman.FBX")
            .OfType<Avatar>().FirstOrDefault(avatar => avatar.isHuman);
        if (animator != null && humanoidAvatar != null && (animator.avatar == null || !animator.avatar.isHuman))
            animator.avatar = humanoidAvatar;

        var hand = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        Transform muzzle = null;
        if (hand != null)
        {
            var weapon = hand.Find("Pistola_Protagonista");
            if (weapon == null)
            {
                var pistolAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PistolPath);
                var pistolInstance = PrefabUtility.InstantiatePrefab(pistolAsset, hand) as GameObject;
                if (pistolInstance != null)
                {
                    pistolInstance.name = "Pistola_Protagonista";
                    pistolInstance.transform.localPosition = new Vector3(0.015f, -0.025f, 0.025f);
                    pistolInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    weapon = pistolInstance.transform;
                }
            }

            if (weapon != null)
            {
                muzzle = weapon.Find("Muzzle");
                if (muzzle == null)
                {
                    var muzzleObject = new GameObject("Muzzle");
                    muzzle = muzzleObject.transform;
                    muzzle.SetParent(weapon, false);
                    muzzle.localPosition = new Vector3(0f, 0f, 0.24f);
                }
            }
        }
        else
        {
            Debug.LogWarning("No encontré el hueso RightHand; el disparo funcionará desde la cámara, pero la pistola no se pudo montar en la mano.", player);
        }

        var combat = player.GetComponent<AventureraCombatController>();
        if (combat == null) combat = player.AddComponent<AventureraCombatController>();
        combat.AimCamera = camera;
        combat.Muzzle = muzzle;
        EditorUtility.SetDirty(combat);
        return muzzle;
    }

    static void SetupMazeWave(Transform root, Transform player, Transform golemGroup, GameObject actorPrefab)
    {
        var trigger = FindChild(root, "Oleada_Golems_Entrada_Laberinto");
        if (trigger == null)
        {
            var triggerObject = new GameObject("Oleada_Golems_Entrada_Laberinto");
            triggerObject.transform.SetParent(root, false);
            trigger = triggerObject.transform;
        }

        trigger.position = new Vector3(0f, MiddleSurface, 12.1f);
        var spawner = trigger.GetComponent<GolemWaveSpawner>();
        if (spawner == null) spawner = trigger.gameObject.AddComponent<GolemWaveSpawner>();
        spawner.GolemActorPrefab = actorPrefab;
        spawner.Player = player;
        spawner.GolemParent = golemGroup;
        spawner.SpawnPoints = new[]
        {
            new Vector3(-16f, UpperSurface, 40f),
            new Vector3(16f, UpperSurface, 40f),
            new Vector3(0f, UpperSurface, 40f)
        };
        spawner.WaveSize = 3;
        EditorUtility.SetDirty(spawner);
    }

    static void SetupTorchLights(Transform root)
    {
        var torches = root.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name.StartsWith("Antorcha_", System.StringComparison.Ordinal)).ToArray();
        foreach (var torch in torches)
        {
            var lightTransform = torch.Find("Luz_Antorcha");
            if (lightTransform == null)
            {
                var lightObject = new GameObject("Luz_Antorcha");
                lightTransform = lightObject.transform;
                lightTransform.SetParent(torch, false);
                lightTransform.localPosition = new Vector3(0f, 1.25f, 0f);
            }
            var light = lightTransform.GetComponent<Light>();
            if (light == null) light = lightTransform.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.56f, 0.24f);
            light.intensity = 2.2f;
            light.range = 7f;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
        }
    }

    static void SetupFoliage(Transform root)
    {
        var oldDecor = FindChild(root, "90_Decoracion_Fuera_del_NavMesh");
        if (oldDecor != null) Object.DestroyImmediate(oldDecor.gameObject);

        var foliage = CreateOrLoadFoliageMaterial();
        var decorObject = new GameObject("90_Decoracion_Fuera_del_NavMesh");
        decorObject.transform.SetParent(root, false);
        decorObject.transform.localPosition = Vector3.zero;

        var patches = root.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name.StartsWith("Recipiente_", System.StringComparison.Ordinal))
            .Select(item => root.InverseTransformPoint(item.position + Vector3.up * 0.08f)).ToList();
        if (patches.Count == 0)
        {
            patches.Add(new Vector3(-18f, 0.98f, -16f));
            patches.Add(new Vector3(18f, 0.98f, -16f));
        }

        var meshObject = new GameObject("Agaves_Bajas_en_Macetas");
        meshObject.transform.SetParent(decorObject.transform, false);
        var meshFilter = meshObject.AddComponent<MeshFilter>();
        var meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = CreateAgaveMesh(patches);
        meshRenderer.sharedMaterial = foliage;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;
        meshObject.isStatic = false;
    }

    static Material CreateOrLoadFoliageMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(FoliageMaterialPath);
        if (material != null) return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = "TempleFoliage" };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.18f, 0.32f, 0.16f, 1f));
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.18f, 0.32f, 0.16f, 1f));
        material.enableInstancing = true;
        AssetDatabase.CreateAsset(material, FoliageMaterialPath);
        return material;
    }

    static Mesh CreateAgaveMesh(List<Vector3> centers)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const int segments = 4;
        const int blades = 8;

        foreach (var center in centers)
        {
            for (var blade = 0; blade < blades; blade++)
            {
                var angle = Mathf.PI * 2f * blade / blades;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var side = new Vector3(direction.z, 0f, -direction.x);
                var height = 0.52f + (blade % 3) * 0.08f;
                var bend = 0.14f + (blade % 2) * 0.05f;
                var firstVertex = vertices.Count;

                for (var segment = 0; segment <= segments; segment++)
                {
                    var t = (float)segment / segments;
                    var width = 0.012f + Mathf.Sin(t * Mathf.PI) * 0.072f;
                    var bladeCenter = center + direction * (bend * t * t) + Vector3.up * (height * t);
                    vertices.Add(bladeCenter - side * width);
                    vertices.Add(bladeCenter + side * width);
                }

                for (var segment = 0; segment < segments; segment++)
                {
                    var a = firstVertex + segment * 2;
                    var b = a + 1;
                    var c = a + 2;
                    var d = a + 3;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                    triangles.Add(b); triangles.Add(c); triangles.Add(a);
                    triangles.Add(d); triangles.Add(c); triangles.Add(b);
                }
            }
        }

        var mesh = new Mesh { name = "TempleAgave_Combined" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void SetupSceneLighting()
    {
        var root = GameObject.Find("Templo_PW_NavMesh_Level");
        if (root == null) return;

        var directional = root.GetComponentsInChildren<Light>(true).FirstOrDefault(light => light.type == LightType.Directional);
        if (directional != null)
        {
            directional.color = new Color(1f, 0.88f, 0.72f);
            directional.intensity = 1.3f;
            directional.shadows = LightShadows.Soft;
            RenderSettings.sun = directional;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.55f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.52f, 0.40f);
        RenderSettings.ambientGroundColor = new Color(0.19f, 0.15f, 0.12f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.61f, 0.55f, 0.48f);
        RenderSettings.fogStartDistance = 58f;
        RenderSettings.fogEndDistance = 105f;
    }

    static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var nested = FindChild(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    static Bounds GetBounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new System.InvalidOperationException("El modelo de gólem no tiene Renderer.");
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
