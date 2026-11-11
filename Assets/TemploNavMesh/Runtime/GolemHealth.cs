using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class GolemHealth : MonoBehaviour
{
    [SerializeField, Min(1)] int maximumHealth = 3;
    [SerializeField, Min(0.1f)] float despawnDelay = 1.7f;

    int currentHealth;
    bool dying;
    Renderer[] renderers;
    Color[] originalColors;
    int[] colorProperties;
    MaterialPropertyBlock propertyBlock;
    NavMeshAgent agent;
    CapsuleCollider solidCollider;
    GolemChasePlayer chase;
    Animator animator;

    public int CurrentHealth => currentHealth;
    public int MaximumHealth => maximumHealth;
    public bool IsDying => dying;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        currentHealth = maximumHealth;
        agent = GetComponent<NavMeshAgent>();
        solidCollider = GetComponent<CapsuleCollider>();
        chase = GetComponent<GolemChasePlayer>();
        animator = GetComponentInChildren<Animator>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        colorProperties = new int[renderers.Length];

        for (var i = 0; i < renderers.Length; i++)
        {
            var material = renderers[i] != null ? renderers[i].sharedMaterial : null;
            if (material != null && material.HasProperty("_BaseColor"))
            {
                colorProperties[i] = Shader.PropertyToID("_BaseColor");
                originalColors[i] = material.GetColor(colorProperties[i]);
            }
            else if (material != null && material.HasProperty("_Color"))
            {
                colorProperties[i] = Shader.PropertyToID("_Color");
                originalColors[i] = material.GetColor(colorProperties[i]);
            }
        }
    }

    public void TakeDamage(int damage, Vector3 impactPoint)
    {
        if (dying || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (animator != null && animator.HasState(0, Animator.StringToHash("GetHit")))
            animator.Play("GetHit", 0, 0f);
        StartCoroutine(FlashRed());

        if (currentHealth == 0)
            StartCoroutine(Die());
    }

    IEnumerator FlashRed()
    {
        var flashColor = new Color(1f, 0.24f, 0.16f, 1f);
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || colorProperties[i] == 0) continue;
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperties[i], Color.Lerp(originalColors[i], flashColor, 0.8f));
            renderers[i].SetPropertyBlock(propertyBlock);
        }

        yield return new WaitForSeconds(0.12f);
        RestoreColors();
    }

    IEnumerator Die()
    {
        dying = true;
        if (chase != null) chase.enabled = false;
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }
        if (solidCollider != null) solidCollider.enabled = false;
        if (animator != null && animator.HasState(0, Animator.StringToHash("Die")))
            animator.Play("Die", 0, 0f);

        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
    }

    void RestoreColors()
    {
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || colorProperties[i] == 0) continue;
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperties[i], originalColors[i]);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    void OnGUI()
    {
        if (dying || Camera.main == null || Vector3.Distance(Camera.main.transform.position, transform.position) > 24f)
            return;

        var screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.1f);
        if (screen.z <= 0f) return;
        var rect = new Rect(screen.x - 24f, Screen.height - screen.y, 48f, 6f);
        var previousColor = GUI.color;
        GUI.color = new Color(0.09f, 0.07f, 0.06f, 0.88f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.Lerp(new Color(0.78f, 0.18f, 0.13f), new Color(0.34f, 0.77f, 0.33f), (float)currentHealth / maximumHealth);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, (rect.width - 2f) * currentHealth / maximumHealth, rect.height - 2f), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
