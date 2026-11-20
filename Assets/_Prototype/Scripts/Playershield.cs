using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escudo de Healing Knight (GDD 5.8).
/// - Arco frontal de bloqueo (120 grados por defecto).
/// - Energia: cada bloqueo consume energia; si llega a 0 el escudo se rompe.
/// - Regeneracion tras un retardo sin recibir golpes.
/// - Parry: bloquear dentro de la ventana inicial no cuesta energia y da recompensa.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Escudo")]
    public float parryWindow = 0.2f;
    public KeyCode blockKey = KeyCode.K;   // GDD: K / LT / B (Q se usa para cambiar de medicamento)

    [Header("Energia (GDD 5.8)")]
    public float maxEnergy = 100f;
    public float projectileCost = 20f;     // por proyectil bloqueado
    public float contactCost = 30f;        // por contacto bloqueado
    public float regenDelay = 1.5f;        // segundos sin recibir golpes antes de regenerar
    public float regenPerSecond = 15f;
    public float breakCooldown = 2.5f;     // inutilizable tras romperse
    [Range(10f, 360f)]
    public float arcDegrees = 120f;        // cobertura frontal

    [Header("Referencias")]
    public PlayerShooting shooting;
    public GameObject bubble;
    [Tooltip("Opcional: objeto cuya escala X indica hacia donde mira el jugador. Si queda vacio se usa este objeto.")]
    public Transform facingReference;

    [Header("UI (opcional)")]
    [Tooltip("Barra de energia del escudo (Slider 0 a 1).")]
    public Slider energyBar;

    [Header("Visual")]
    public Color normalColor = new Color(0.3f, 0.8f, 1f, 0.35f);
    public Color lowEnergyColor = new Color(1f, 0.35f, 0.25f, 0.35f);
    public Color parryColor = new Color(1f, 1f, 1f, 0.75f);
    public float parryFlashTime = 0.15f;

    public event Action OnShieldBroken;
    public event Action OnBlocked;

    public bool IsBlocking => isBlocking;
    public bool IsBroken => isBroken;
    public float Energy01 => maxEnergy > 0f ? energy / maxEnergy : 0f;

    private float energy;
    private bool isBlocking;
    private bool isBroken;
    private float brokenUntil;
    private float lastHitTime = -999f;
    private float blockStartTime;
    private float flashUntil;
    private SpriteRenderer bubbleRenderer;

    void Awake()
    {
        energy = maxEnergy;

        if (bubble == null)
        {
            Debug.LogError("PlayerShield: falta asignar el campo 'Bubble'.", this);
            enabled = false;
            return;
        }

        // Busca el SpriteRenderer en la burbuja o en sus hijos (incluso si esta desactivada)
        bubbleRenderer = bubble.GetComponentInChildren<SpriteRenderer>(true);
        if (bubbleRenderer == null)
            Debug.LogError("PlayerShield: 'Bubble' no tiene SpriteRenderer en si ni en sus hijos.", bubble);

        bubble.SetActive(false);
    }

    void Update()
    {
        // Fin del enfriamiento tras ruptura
        if (isBroken && Time.time >= brokenUntil)
            isBroken = false;

        bool wantsBlock = Input.GetKey(blockKey) && !isBroken && energy > 0f;

        // Inicio del bloqueo: arranca la ventana de parry
        if (wantsBlock && !isBlocking)
            blockStartTime = Time.time;

        isBlocking = wantsBlock;

        // Regeneracion despues de un tiempo sin recibir golpes
        if (!isBroken && energy < maxEnergy && Time.time >= lastHitTime + regenDelay)
            energy = Mathf.Min(maxEnergy, energy + regenPerSecond * Time.deltaTime);

        if (bubble.activeSelf != isBlocking)
            bubble.SetActive(isBlocking);

        if (isBlocking && bubbleRenderer != null)
        {
            Color baseColor = Color.Lerp(lowEnergyColor, normalColor, Energy01);
            bubbleRenderer.color = Time.time < flashUntil ? parryColor : baseColor;
        }

        if (energyBar != null)
            energyBar.value = Energy01;
    }

    /// <summary>Llamar desde el trigger del escudo cuando toca un proyectil enemigo.</summary>
    public void HandleHit(Collider2D other)
    {
        if (!other.CompareTag("EnemyProjectile")) return;
        if (!isBlocking) return;

        RegisterBlock(projectileCost);
        Destroy(other.gameObject);
    }

    /// <summary>Version con direccion: solo bloquea si el ataque llega por el frente (arco).</summary>
    public bool TryBlock(float incomingDamage, Vector2 sourcePosition, bool isProjectile)
    {
        if (!isBlocking) return false;
        if (!IsInFrontArc(sourcePosition)) return false;

        RegisterBlock(isProjectile ? projectileCost : contactCost);
        return true;
    }

    /// <summary>Version compatible con el codigo anterior (sin direccion, cuenta como contacto).</summary>
    public bool TryBlock(float incomingDamage)
    {
        if (!isBlocking) return false;

        RegisterBlock(contactCost);
        return true;
    }

    private bool IsInFrontArc(Vector2 sourcePosition)
    {
        Vector2 toSource = sourcePosition - (Vector2)transform.position;
        if (toSource.sqrMagnitude < 0.0001f) return true;

        Transform reference = facingReference != null ? facingReference : transform;
        Vector2 facing = new Vector2(reference.lossyScale.x >= 0f ? 1f : -1f, 0f);

        return Vector2.Angle(facing, toSource) <= arcDegrees * 0.5f;
    }

    private void RegisterBlock(float cost)
    {
        lastHitTime = Time.time;
        OnBlocked?.Invoke();

        bool isParry = (Time.time - blockStartTime) <= parryWindow;
        if (isParry)
        {
            // El parry no consume energia y da recompensa
            if (shooting != null) shooting.RewardParry();
            flashUntil = Time.time + parryFlashTime;
            return;
        }

        energy = Mathf.Max(0f, energy - cost);
        if (energy <= 0f)
            Break();
    }

    private void Break()
    {
        isBlocking = false;
        isBroken = true;
        brokenUntil = Time.time + breakCooldown;
        bubble.SetActive(false);
        OnShieldBroken?.Invoke();
    }
}