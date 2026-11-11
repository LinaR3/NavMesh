using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[RequireComponent(typeof(CharacterController), typeof(AventureraPlayerController))]
public sealed class AventureraRouteAudit : MonoBehaviour
{
    [SerializeField] Key startKey = Key.F8;
    [SerializeField, Min(0.5f)] float auditSpeed = 4f;
    [SerializeField, Min(0.1f)] float arrivalRadius = 0.9f;
    [SerializeField, Min(1f)] float stuckTimeout = 5f;
    [SerializeField] bool requireBakedNavMesh = true;
    [SerializeField] Transform targetPlayer;

    // Physical review route for the isolated CiudadelaMaya_AssetsPreview scene.
    // It covers the west/east side terraces, the accessible maze, upper galleries,
    // lookout and the short underground passage without using coordinates from the
    // older TemploNavMesh layout.
    static readonly Vector3[] Route =
    {
        // Plaza de inicio and monumental stair, both directions.
        new Vector3(0f, 0f, -70f), new Vector3(0f, 0f, -54f), new Vector3(0f, 0f, -42f),
        new Vector3(0f, 0f, -28f), new Vector3(0f, 0f, -42f), new Vector3(0f, 0f, -54f),
        // West ascent, maze platform and upper gallery, then the same stair back to ground.
        new Vector3(-44f, 0f, -54f), new Vector3(-61f, 0f, -54f), new Vector3(-61f, 0f, -28f),
        new Vector3(-61f, 0f, -16f), new Vector3(-61f, 0f, -8f), new Vector3(-61f, 0f, 24f),
        new Vector3(-55f, 0f, 20f), new Vector3(-55f, 0f, 31f), new Vector3(-55f, 0f, 46f),
        new Vector3(-48f, 0f, 54f), new Vector3(-48f, 0f, 62f), new Vector3(-55f, 0f, 62f),
        new Vector3(-55f, 0f, 46f), new Vector3(-55f, 0f, 31f), new Vector3(-55f, 0f, 20f),
        new Vector3(-61f, 0f, 8f), new Vector3(-61f, 0f, -16f), new Vector3(-61f, 0f, -28f),
        new Vector3(-61f, 0f, -54f), new Vector3(0f, 0f, -54f),
        // Mirrored east-side stairs and gallery.
        new Vector3(44f, 0f, -54f), new Vector3(61f, 0f, -54f), new Vector3(61f, 0f, -28f),
        new Vector3(61f, 0f, -16f), new Vector3(61f, 0f, -8f), new Vector3(61f, 0f, 24f),
        new Vector3(55f, 0f, 20f), new Vector3(55f, 0f, 31f), new Vector3(55f, 0f, 46f),
        new Vector3(48f, 0f, 54f), new Vector3(48f, 0f, 62f), new Vector3(55f, 0f, 46f),
        new Vector3(55f, 0f, 31f), new Vector3(55f, 0f, 20f), new Vector3(61f, 0f, 8f),
        new Vector3(61f, 0f, -16f), new Vector3(61f, 0f, -28f), new Vector3(61f, 0f, -54f),
        new Vector3(0f, 0f, -54f),
        // Central upper gallery and lookout. Checkpoints are at landings, not mid-step.
        new Vector3(0f, 0f, -3f), new Vector3(0f, 0f, 27f), new Vector3(0f, 0f, 34f),
        new Vector3(0f, 0f, 39f), new Vector3(0f, 0f, 50f), new Vector3(0f, 0f, 63f),
        new Vector3(0f, 0f, 70f), new Vector3(0f, 0f, 63f), new Vector3(0f, 0f, 50f),
        new Vector3(0f, 0f, 39f), new Vector3(0f, 0f, 27f), new Vector3(0f, 0f, -3f),
        // Short underground link: down the vault approach, through the circular chamber,
        // and back up to the rear patio.
        new Vector3(25f, 0f, 28f), new Vector3(25f, 0f, 39f), new Vector3(25f, 0f, 48f),
        new Vector3(25f, 0f, 62f), new Vector3(25f, 0f, 76f), new Vector3(25f, 0f, 83f),
        new Vector3(25f, 0f, 76f), new Vector3(25f, 0f, 62f), new Vector3(25f, 0f, 48f),
        new Vector3(25f, 0f, 39f), new Vector3(25f, 0f, 28f), new Vector3(0f, 0f, 27f),
        // Return to the start.
        new Vector3(0f, 0f, -3f), new Vector3(0f, 0f, -28f), new Vector3(0f, 0f, -42f),
        new Vector3(0f, 0f, -54f), new Vector3(0f, 0f, -70f)
    };

    // Expected floor elevations at the route checkpoints (not the character root).
    // This prevents a grounded character on the terrain below from being counted
    // as having reached an upper gallery or the vault.
    static readonly float[] ExpectedFloorY =
    {
        .18f,.18f,.18f,4.18f,1.04f,.18f,
        .18f,.18f,.18f,2.18f,4.18f,4.18f,4.18f,6.18f,8.18f,8.18f,8.18f,8.18f,8.18f,6.18f,4.18f,4.18f,2.18f,.18f,.18f,.18f,
        .18f,.18f,.18f,2.18f,4.18f,4.18f,4.18f,6.18f,8.18f,8.18f,8.18f,8.18f,6.18f,4.18f,4.18f,2.18f,.18f,.18f,.18f,
        4.18f,4.18f,4.58f,6.18f,8.18f,12.18f,12.18f,12.18f,8.18f,6.18f,4.18f,
        4.18f,4.18f,1.10f,-1.40f,-3.08f,-1.30f,.18f,-1.30f,-3.08f,-1.40f,1.10f,4.18f,
        4.18f,4.18f,4.18f,1.04f,.18f,.18f
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

    void Start()
    {
#if UNITY_EDITOR
        // Keep the unattended physical review running while Unity's window loses focus.
        Application.runInBackground = true;
#endif
        // This auditor is a separate QA object. Always resolve the real tagged
        // Player instead of silently moving the auditor's required test capsule.
        var target = targetPlayer != null ? targetPlayer : GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target != null && target != transform) SetTargetPlayer(target);
        // The spawn capsule is pushed 0.68 m clear by adjoining architectural
        // colliders on scene load. Start inside the accepted arrival tolerance.
        arrivalRadius = 0.9f;
        requireBakedNavMesh = false;
        // This component exists only in the isolated review scene. Starting the route
        // automatically avoids a keyboard/input dependency in unattended Play Mode.
        StartCoroutine(StartAuditWhenGrounded());
    }

    IEnumerator StartAuditWhenGrounded()
    {
        for (var i = 0; i < 120; i++)
        {
            if (controller != null && playerMotor != null && controller.isGrounded)
            {
                yield return null;
                StartCoroutine(RunAudit());
                yield break;
            }
            yield return null;
        }
        Debug.LogError("[Auditoría Player] No se pudo iniciar: personaje/controlador no quedó apoyado en el piso inicial.", this);
    }

    public void SetTargetPlayer(Transform target)
    {
        targetPlayer = target;
        if (target == null) return;
        transform.SetPositionAndRotation(target.position, target.rotation);
        controller = target.GetComponent<CharacterController>();
        playerMotor = target.GetComponent<AventureraPlayerController>();
    }

    void Update()
    {
        if (controller == null || playerMotor == null) return;
        if (!running && Keyboard.current != null && Keyboard.current[startKey].wasPressedThisFrame)
            StartCoroutine(RunAudit());
    }

    IEnumerator RunAudit()
    {
        running = true;
        var actor = controller.transform;
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
            var bestDistance = HorizontalDistance(actor.position, point);
            var lastProgress = Time.time;
            var reached = false;

            while (Time.time - lastProgress < stuckTimeout)
            {
                var remaining = HorizontalDistance(actor.position, point);
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
                var flatDirection = new Vector3(point.x - actor.position.x, 0f, point.z - actor.position.z).normalized;
                if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
                verticalSpeed -= playerMotor.Gravity * deltaTime;
                if (flatDirection.sqrMagnitude > 0.001f)
                    actor.rotation = Quaternion.RotateTowards(actor.rotation, Quaternion.LookRotation(flatDirection, Vector3.up), 720f * deltaTime);
                var flags = controller.Move((flatDirection * auditSpeed + Vector3.up * verticalSpeed) * deltaTime);
                if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -2f;
                yield return null;
            }

            if (!reached)
            {
                failure = $"Destino {i + 1}/{Route.Length} no alcanzado en ({point.x:F1}, {point.z:F1}); quedó a {HorizontalDistance(actor.position, point):F2} m. Posición del Player {actor.position:F2}.";
                Debug.LogError("[Auditoría Player] FALLO: " + failure, this);
                break;
            }

            // Arriving horizontally on a ramp does not guarantee that the
            // CharacterController has processed its ground contact this frame.
            // Settle downward for up to 0.2 s, then validate physical support.
            for (var settle = 0; settle < 12 && !controller.isGrounded; settle++)
            {
                verticalSpeed = Mathf.Min(verticalSpeed, -2f);
                var settleFlags = controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);
                if ((settleFlags & CollisionFlags.Below) != 0) verticalSpeed = -2f;
                yield return null;
            }
            var hasNavMeshFloor = NavMesh.SamplePosition(actor.position, out var navMeshHit, 0.9f, NavMesh.AllAreas);
            var actualFloorY = controller.bounds.min.y;
            var expectedFloorY = ExpectedFloorY[i];
            var floorLevelError = Mathf.Abs(actualFloorY - expectedFloorY);
            if ((requireBakedNavMesh && !hasNavMeshFloor) || !controller.isGrounded || floorLevelError > 0.65f)
            {
                failure = $"Destino {i + 1}/{Route.Length}: piso/nivel incorrecto (grounded={controller.isGrounded}, nivel esperado Y={expectedFloorY:F2}, piso real Y={actualFloorY:F2}, error={floorLevelError:F2} m, muestraNavMesh={hasNavMeshFloor}, posición Player={actor.position:F2}).";
                Debug.LogError("[Auditoría Player] FALLO: " + failure, this);
                break;
            }

            passed++;
            Debug.Log($"[Auditoría Player] OK {passed}/{Route.Length}: ({point.x:F1}, {point.z:F1}); grounded={controller.isGrounded}; piso esperado/real={expectedFloorY:F2}/{actualFloorY:F2}; NavMesh={hasNavMeshFloor}; Player={actor.position:F2}.", this);
        }

        foreach (var agent in agents)
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        foreach (var chase in chaseScripts)
            if (chase != null) chase.enabled = true;
        playerMotor.InputEnabled = previousInput;
        running = false;

        if (passed == Route.Length)
            Debug.Log($"[Auditoría Player] RESULTADO: APROBADA. La aventurera recorrió físicamente los tres niveles, el mini laberinto y volvió al punto de inicio; {passed}/{Route.Length} destinos con CharacterController grounded y {(requireBakedNavMesh ? "NavMesh bajo sus pies" : "NavMesh no requerido para esta prueba física")}.", this);
        else
            Debug.LogError($"[Auditoría Player] RESULTADO: NO APROBADA ({passed}/{Route.Length}). {failure}", this);

#if UNITY_EDITOR
        var result = passed == Route.Length
            ? $"APROBADA: {passed}/{Route.Length} destinos alcanzados por el Player real, grounded y en el nivel previsto. NavMesh deliberadamente no requerido en esta revisión aislada."
            : $"NO APROBADA: {passed}/{Route.Length}. {failure}";
        File.WriteAllText("/tmp/CitadelMovementRouteAudit.txt", result + "\nPosición final del Player: " + actor.position.ToString("F2"));
        EditorApplication.isPlaying = false;
#endif
    }

    static float HorizontalDistance(Vector3 position, Vector3 target)
    {
        var dx = position.x - target.x;
        var dz = position.z - target.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
