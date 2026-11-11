using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class ThirdPersonAdventureCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField, Min(1f)] float distance = 6.5f;
    [SerializeField, Min(0.5f)] float minDistance = 2.4f;
    [SerializeField, Min(1f)] float maxDistance = 10f;
    [SerializeField] float pitch = 13f;
    [SerializeField, Min(0f)] float sensitivity = 0.14f;
    [SerializeField, Min(0.01f)] float followSharpness = 16f;
    [SerializeField, Min(0.05f)] float collisionRadius = 0.24f;
    [SerializeField] float collisionPadding = 0.16f;

    float yaw;
    bool initialized;

    public Transform Target { get => target; set => target = value; }
    public float Distance { get => distance; set => distance = Mathf.Clamp(value, minDistance, maxDistance); }
    public float Pitch { get => pitch; set => pitch = Mathf.Clamp(value, -8f, 48f); }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            initialized = true;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (!initialized)
        {
            yaw = target.eulerAngles.y;
            initialized = true;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -8f, 48f);
            }
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance - scroll * 0.004f, minDistance, maxDistance);
        }

        var pivot = target.position + targetOffset;
        var orbit = Quaternion.Euler(pitch, yaw, 0f);
        var direction = orbit * Vector3.back;
        var desiredDistance = distance;
        var hits = Physics.SphereCastAll(pivot, collisionRadius, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == target || hit.collider.transform.IsChildOf(target)) continue;
            desiredDistance = Mathf.Min(desiredDistance, Mathf.Max(0.35f, hit.distance - collisionPadding));
        }

        var desiredPosition = pivot + direction * desiredDistance;
        var blend = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }
}
