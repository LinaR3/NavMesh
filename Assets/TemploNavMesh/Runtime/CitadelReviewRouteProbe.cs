using UnityEngine;

/// <summary>Play Mode-only physical route check for the isolated citadel review scene.</summary>
[RequireComponent(typeof(AventureraRouteAudit))]
public sealed class CitadelReviewRouteProbe : MonoBehaviour
{
    [SerializeField] Transform player;

    public void Initialize(Transform target) => player = target;

    void Awake()
    {
        var audit = GetComponent<AventureraRouteAudit>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (audit != null && player != null) audit.SetTargetPlayer(player);
    }
}
