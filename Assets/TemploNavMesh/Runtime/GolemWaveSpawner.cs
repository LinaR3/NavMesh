using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class GolemWaveSpawner : MonoBehaviour
{
    [SerializeField] GameObject golemActorPrefab;
    [SerializeField] Transform player;
    [SerializeField] Transform golemParent;
    [SerializeField] Vector3[] spawnPoints = new Vector3[0];
    [SerializeField, Min(1)] int waveSize = 3;
    [SerializeField, Min(0.5f)] float activationRadius = 2.25f;
    [SerializeField, Min(0.5f)] float sampleDistance = 2f;
    [SerializeField, Min(0f)] float intervalBetweenSpawns = 0.5f;
    [SerializeField] bool activated;

    public GameObject GolemActorPrefab { get => golemActorPrefab; set => golemActorPrefab = value; }
    public Transform Player { get => player; set => player = value; }
    public Transform GolemParent { get => golemParent; set => golemParent = value; }
    public Vector3[] SpawnPoints { get => spawnPoints; set => spawnPoints = value ?? new Vector3[0]; }
    public int WaveSize { get => waveSize; set => waveSize = Mathf.Max(1, value); }

    void Update()
    {
        if (activated) return;
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (player == null || Mathf.Abs(player.position.y - transform.position.y) > 1.8f) return;
        var horizontal = player.position - transform.position;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude <= activationRadius * activationRadius)
        {
            activated = true;
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        if (golemActorPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("La oleada de gólems necesita un prefab de actor y puntos de aparición.", this);
            yield break;
        }

        var templateAgent = golemActorPrefab.GetComponent<NavMeshAgent>();
        if (templateAgent == null)
        {
            Debug.LogError("El prefab de oleada no tiene NavMeshAgent en la raíz.", golemActorPrefab);
            yield break;
        }

        var filter = new NavMeshQueryFilter { agentTypeID = templateAgent.agentTypeID, areaMask = templateAgent.areaMask };
        var spawned = 0;
        for (var i = 0; i < spawnPoints.Length && spawned < waveSize; i++)
        {
            if (!NavMesh.SamplePosition(spawnPoints[i], out var navHit, sampleDistance, filter))
            {
                Debug.LogWarning($"Punto de aparición {i + 1} no está sobre un NavMesh: {spawnPoints[i]}.", this);
                continue;
            }

            if (Physics.CheckSphere(navHit.position + Vector3.up * 0.9f, 0.72f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                continue;

            var parent = golemParent != null ? golemParent : transform.parent;
            var actor = Instantiate(golemActorPrefab, navHit.position, Quaternion.identity, parent);
            actor.name = $"Golem_Oleada_{spawned + 1:00}";

            var agent = actor.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.Warp(navHit.position);
                if (agent.isOnNavMesh) agent.isStopped = false;
            }

            var chase = actor.GetComponent<GolemChasePlayer>();
            if (chase != null)
            {
                chase.Target = player;
                chase.ConfigureOrbitSlot(8 + spawned, 12);
            }

            spawned++;
            CreateSpawnPulse(navHit.position);
            Debug.Log($"[Oleada] Apareció {actor.name} sobre el NavMesh en {navHit.position:F2}.", actor);
            if (intervalBetweenSpawns > 0f) yield return new WaitForSeconds(intervalBetweenSpawns);
        }

        Debug.Log($"[Oleada] Terminada: {spawned}/{Mathf.Min(waveSize, spawnPoints.Length)} gólems aparecieron sobre el NavMesh.", this);
    }

    static void CreateSpawnPulse(Vector3 position)
    {
        var pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pulse.name = "Efecto_Aparicion_Golem";
        pulse.transform.position = position + Vector3.up * 0.04f;
        pulse.transform.localScale = new Vector3(1.35f, 0.045f, 1.35f);
        var collider = pulse.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        var renderer = pulse.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var material = new Material(shader) { name = "GolemSpawnPulse" };
                var color = new Color(0.2f, 0.9f, 0.72f, 1f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                renderer.material = material;
            }
        }
        Destroy(pulse, 0.65f);
    }
}
