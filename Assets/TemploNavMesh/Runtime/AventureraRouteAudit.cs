using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(AventureraPlayerController))]
public sealed class AventureraRouteAudit : MonoBehaviour
{
    [SerializeField] Key startKey = Key.F8;
    [SerializeField, Min(0.5f)] float auditSpeed = 3.2f;
    [SerializeField, Min(0.1f)] float arrivalRadius = 0.42f;
    [SerializeField, Min(1f)] float stuckTimeout = 5f;

    static readonly Vector3[] Route =
    {
        new Vector3(-19f, 0f, -16f), new Vector3(-19f, 0f, -3f),
        new Vector3(0f, 0f, -3f), new Vector3(19f, 0f, -3f), new Vector3(19f, 0f, -16f),
        new Vector3(0f, 0f, 3.5f), new Vector3(0f, 0f, 10.2f),
        new Vector3(0f, 0f, 12.2f), new Vector3(7.4f, 0f, 12.2f),
        new Vector3(7.4f, 0f, 16.3f), new Vector3(7.4f, 0f, 19f), new Vector3(0f, 0f, 19.2f),
        new Vector3(0f, 0f, 21.5f), new Vector3(0f, 0f, 27.2f),
        new Vector3(16f, 0f, 28f), new Vector3(16f, 0f, 38f),
        new Vector3(-16f, 0f, 38f), new Vector3(-16f, 0f, 28f), new Vector3(0f, 0f, 27.2f),
        new Vector3(0f, 0f, 21.5f), new Vector3(0f, 0f, 19.2f), new Vector3(7.4f, 0f, 19.2f),
        new Vector3(7.4f, 0f, 16.3f), new Vector3(7.4f, 0f, 12.2f), new Vector3(0f, 0f, 12.2f),
        new Vector3(0f, 0f, 10.2f), new Vector3(0f, 0f, 3.5f),
        new Vector3(0f, 0f, -12f)
    };

    CharacterController controller;
    AventureraPlayerController playerMotor;
    bool running;
    float verticalSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMotor = GetComponent<AventureraPlayerController>();
    }

    void Update()
    {
        if (!running && Keyboard.current != null && Keyboard.current[startKey].wasPressedThisFrame)
            StartCoroutine(RunAudit());
    }

    IEnumerator RunAudit()
    {
        running = true;
        var previousInput = playerMotor.InputEnabled;
        playerMotor.InputEnabled = false;
        verticalSpeed = -2f;
        var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        var chaseScripts = FindObjectsByType<GolemChasePlayer>(FindObjectsSortMode.None);
        foreach (var chase in chaseScripts) chase.enabled = false;
        foreach (var agent in agents)
        {
            if (!agent.enabled) continue;
            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        Debug.Log($"[Auditoría Player] Inicio de recorrido físico: {Route.Length} destinos. El Player usa CharacterController; los golems están pausados durante la prueba.", this);
        var passed = 0;
        var failure = string.Empty;
        for (var i = 0; i < Route.Length; i++)
        {
            var point = Route[i];
            var bestDistance = HorizontalDistance(transform.position, point);
            var lastProgress = Time.time;
            var reached = false;

            while (Time.time - lastProgress < stuckTimeout)
            {
                var remaining = HorizontalDistance(transform.position, point);
                if (remaining <= arrivalRadius)
                {
                    reached = true;
                    break;
                }
                if (remaining < bestDistance - 0.12f)
                {
                    bestDistance = remaining;
                    lastProgress = Time.time;
                }

                var deltaTime = Time.deltaTime;
                var flatDirection = new Vector3(point.x - transform.position.x, 0f, point.z - transform.position.z).normalized;
                if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
                verticalSpeed -= playerMotor.Gravity * deltaTime;
                if (flatDirection.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flatDirection, Vector3.up), 720f * deltaTime);
                var flags = controller.Move((flatDirection * auditSpeed + Vector3.up * verticalSpeed) * deltaTime);
                if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -2f;
                yield return null;
            }

            if (!reached)
            {
                failure = $"Destino {i + 1}/{Route.Length} no alcanzado en ({point.x:F1}, {point.z:F1}); quedó a {HorizontalDistance(transform.position, point):F2} m. Posición final {transform.position:F2}.";
                Debug.LogError("[Auditoría Player] FALLO: " + failure, this);
                break;
            }

            yield return null;
            var hasNavMeshFloor = NavMesh.SamplePosition(transform.position, out var navMeshHit, 0.9f, NavMesh.AllAreas);
            if (!hasNavMeshFloor || !controller.isGrounded)
            {
                failure = $"Destino {i + 1}/{Route.Length}: alcanzó el punto, pero no confirmó piso/malla (grounded={controller.isGrounded}, muestraNavMesh={hasNavMeshFloor}, posición={transform.position:F2}).";
                Debug.LogError("[Auditoría Player] FALLO: " + failure, this);
                break;
            }

            passed++;
            Debug.Log($"[Auditoría Player] OK {passed}/{Route.Length}: ({point.x:F1}, {point.z:F1}); CharacterController grounded; NavMesh y={navMeshHit.position.y:F2}; Player y={transform.position.y:F2}.", this);
        }

        foreach (var agent in agents)
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        foreach (var chase in chaseScripts)
            if (chase != null) chase.enabled = true;
        playerMotor.InputEnabled = previousInput;
        running = false;

        if (passed == Route.Length)
            Debug.Log($"[Auditoría Player] RESULTADO: APROBADA. La aventurera recorrió físicamente los tres niveles, el mini laberinto y volvió al punto de inicio; {passed}/{Route.Length} destinos con CharacterController grounded y NavMesh bajo sus pies.", this);
        else
            Debug.LogError($"[Auditoría Player] RESULTADO: NO APROBADA ({passed}/{Route.Length}). {failure}", this);
    }

    static float HorizontalDistance(Vector3 position, Vector3 target)
    {
        var dx = position.x - target.x;
        var dz = position.z - target.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
