using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class GolemChasePlayer : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField, Min(0.05f)] float refreshInterval = 0.25f;
    [SerializeField, Min(0f)] float targetSampleDistance = 2f;

    NavMeshAgent agent;
    float nextRefreshTime;

    public Transform Target { get => target; set => target = value; }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        nextRefreshTime = 0f;
    }

    void Update()
    {
        if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh || Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshInterval;

        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.SamplePosition(target.position, out var targetHit, targetSampleDistance, filter))
            agent.SetDestination(targetHit.position);
    }
}
