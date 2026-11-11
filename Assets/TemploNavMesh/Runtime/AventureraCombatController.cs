using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(AventureraPlayerController))]
public sealed class AventureraCombatController : MonoBehaviour
{
    [SerializeField] Camera aimCamera;
    [SerializeField] AventureraLocomotionPose locomotionPose;
    [SerializeField] Transform muzzle;
    [SerializeField, Min(1f)] float range = 45f;
    [SerializeField, Min(0.05f)] float fireInterval = 0.32f;
    [SerializeField, Min(1)] int damagePerShot = 1;
    [SerializeField] LayerMask hitMask = Physics.DefaultRaycastLayers;

    LineRenderer tracer;
    Material tracerMaterial;
    float nextShotTime;
    float hitMessageUntil;
    bool hitOnLastShot;

    public Camera AimCamera { get => aimCamera; set => aimCamera = value; }
    public Transform Muzzle { get => muzzle; set => muzzle = value; }

    void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (locomotionPose == null) locomotionPose = GetComponent<AventureraLocomotionPose>();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (locomotionPose != null)
            locomotionPose.SetAiming(mouse.rightButton.isPressed);

        if (mouse.leftButton.wasPressedThisFrame && Time.time >= nextShotTime)
            Shoot();
    }

    void Shoot()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null) return;

        nextShotTime = Time.time + fireInterval;
        var ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var endPoint = ray.origin + ray.direction * range;
        hitOnLastShot = false;

        var hits = Physics.RaycastAll(ray, range, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            endPoint = hit.point;
            var golem = hit.collider.GetComponentInParent<GolemHealth>();
            if (golem != null)
            {
                golem.TakeDamage(damagePerShot, hit.point);
                hitOnLastShot = true;
            }
            break;
        }

        ShowTracer(muzzle != null ? muzzle.position : aimCamera.transform.position + aimCamera.transform.forward * 0.45f, endPoint);
        hitMessageUntil = Time.unscaledTime + 0.28f;
    }

    void ShowTracer(Vector3 start, Vector3 end)
    {
        if (tracer == null)
        {
            var tracerObject = new GameObject("Player_Shot_Tracer");
            tracerObject.transform.SetParent(transform, false);
            tracer = tracerObject.AddComponent<LineRenderer>();
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.numCapVertices = 3;
            tracer.startWidth = 0.045f;
            tracer.endWidth = 0.012f;
            tracer.shadowCastingMode = ShadowCastingMode.Off;
            tracer.receiveShadows = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            tracerMaterial = new Material(shader) { name = "PlayerShot_Emissive" };
            if (tracerMaterial.HasProperty("_BaseColor")) tracerMaterial.SetColor("_BaseColor", new Color(1f, 0.66f, 0.17f, 1f));
            if (tracerMaterial.HasProperty("_Color")) tracerMaterial.SetColor("_Color", new Color(1f, 0.66f, 0.17f, 1f));
            tracer.material = tracerMaterial;
        }

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        tracer.enabled = true;
        StopAllCoroutines();
        StartCoroutine(HideTracer());
    }

    IEnumerator HideTracer()
    {
        yield return new WaitForSeconds(0.075f);
        if (tracer != null) tracer.enabled = false;
    }

    void OnGUI()
    {
        const float gap = 5f;
        const float length = 8f;
        const float thickness = 2f;
        var centerX = Screen.width * 0.5f;
        var centerY = Screen.height * 0.5f;
        var previousColor = GUI.color;
        GUI.color = new Color(1f, 0.83f, 0.42f, 0.95f);
        GUI.DrawTexture(new Rect(centerX - gap - length, centerY - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - gap - length, thickness, length), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY + gap, thickness, length), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(14f, Screen.height - 30f, 760f, 24f), "WASD mover  |  Shift correr  |  Clic izquierdo disparar  |  Clic derecho apuntar / cámara");
        if (Time.unscaledTime < hitMessageUntil)
            GUI.Label(new Rect(centerX - 45f, centerY + 24f, 90f, 24f), hitOnLastShot ? "¡Impacto!" : "");
        GUI.color = previousColor;
    }

    void OnDestroy()
    {
        if (tracerMaterial != null) Destroy(tracerMaterial);
    }
}
