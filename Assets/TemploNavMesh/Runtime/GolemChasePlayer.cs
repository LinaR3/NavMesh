using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider), typeof(Rigidbody))]
public sealed class GolemChasePlayer : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField, Min(0.05f)] float refreshInterval = 0.25f;
    [SerializeField, Min(0.1f)] float targetSampleDistance = 1.15f;
    [SerializeField, Min(0.8f)] float approachRadius = 1.8f;
    [SerializeField, Min(0.8f)] float minimumPlayerClearance = 1.18f;

    NavMeshAgent agent;
    CapsuleCollider solidCollider;
    Rigidbody body;
    float nextRefreshTime;
    [SerializeField, Min(0)] int orbitSlot;
    [SerializeField, Min(1)] int orbitSlotCount = 8;

    public Transform Target { get => target; set => target = value; }

    public void ConfigureOrbitSlot(int slot, int slotCount)
    {
        orbitSlotCount = Mathf.Max(1, slotCount);
        orbitSlot = Mathf.Abs(slot) % orbitSlotCount;
    }

    public void ConfigureEngagement(float newApproachRadius, float newPlayerClearance)
    {
        approachRadius = Mathf.Max(1.1f, newApproachRadius);
        minimumPlayerClearance = Mathf.Clamp(newPlayerClearance, 1.05f, approachRadius);
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        solidCollider = GetComponent<CapsuleCollider>();
        body = GetComponent<Rigidbody>();

        // NavMeshAgent controls movement; a solid, kinematic capsule handles contact.
        // Mirror the agent dimensions so its navigation footprint and collision shape agree.
        solidCollider.isTrigger = false;
        solidCollider.direction = 1;
        solidCollider.radius = agent.radius;
        solidCollider.height = Mathf.Max(agent.height, agent.radius * 2f);
        solidCollider.center = new Vector3(0f, solidCollider.height * 0.5f, 0f);
        solidCollider.sharedMaterial = null;

        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.baseOffset = 0f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        agent.stoppingDistance = 0.08f;
        agent.avoidancePriority = 35 + (orbitSlot * 6) % 60;
        nextRefreshTime = 0f;
    }

    void Update()
    {
        if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh || Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshInterval;

        var angle = (Mathf.PI * 2f * orbitSlot) / orbitSlotCount;
        var slotOffset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * approachRadius;
        var desiredApproach = target.position + slotOffset;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(desiredApproach, out var approachHit, targetSampleDistance, filter)) return;

        var flatSeparation = approachHit.position - target.position;
        flatSeparation.y = 0f;
        if (flatSeparation.magnitude < minimumPlayerClearance) return;
        agent.SetDestination(approachHit.position);
    }
}
