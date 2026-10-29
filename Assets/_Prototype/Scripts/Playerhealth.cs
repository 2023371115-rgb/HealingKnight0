using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Salud del jugador (GDD 5.9).
/// - 100 puntos de salud con barra en el HUD.
/// - Invulnerabilidad de 1.2 s tras recibir dano, con parpadeo.
/// - Empuje horizontal con limite vertical.
/// - Si la salud llega a 0: reaparece en el ultimo checkpoint con salud completa
///   (o muestra Game Over / reinicia escena si se desactiva respawnAtCheckpoint).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Salud")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float Health01 => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;

    [Header("Escudo")]
    public PlayerShield shield;

    [Header("Invulnerabilidad (GDD: 1.2 s)")]
    public float invulnerabilityTime = 1.2f;
    public float blinkInterval = 0.1f;
    [Tooltip("Sprites que parpadean. Si queda vacio se buscan en los hijos.")]
    public SpriteRenderer[] blinkRenderers;

    [Header("Empuje (GDD: limite vertical 4 u/s)")]
    public Rigidbody2D body;
    public float knockbackHorizontal = 6f;
    public float knockbackVertical = 3f;
    public float maxKnockbackVertical = 4f;

    [Header("Muerte y reaparicion")]
    [Tooltip("Si esta activo, al morir reaparece en el ultimo checkpoint con salud completa (GDD). Si no, usa Game Over.")]
    public bool respawnAtCheckpoint = true;
    public float respawnDelay = 0.5f;

    [Header("UI (opcional)")]
    [Tooltip("Barra de salud del HUD (un Slider con valores 0 a 1).")]
    public Slider healthBar;

    [Tooltip("Destello rojo en los bordes al recibir dano (Image a pantalla completa con alpha 0).")]
    public Image damageFlash;
    public float damageFlashAlpha = 0.35f;
    public float damageFlashFade = 3f;

    [Tooltip("Panel de Game Over. Solo se usa si respawnAtCheckpoint esta desactivado. Si queda vacio, la escena se reinicia sola.")]
    public GameObject gameOverPanel;

    [Tooltip("Segundos antes de reiniciar cuando no hay panel.")]
    public float restartDelay = 1.5f;

    public event Action<float, float> OnHealthChanged; // (actual, maxima)
    public event Action OnDamaged;
    public event Action OnDied;
    public event Action OnRespawned;

    private bool invulnerable;
    private Vector3 checkpointPosition;
    private Coroutine invulnerabilityRoutine;

    void Awake()
    {
        currentHealth = maxHealth;
        Time.timeScale = 1f; // por si veniamos de un Game Over anterior

        if (body == null) body = GetComponent<Rigidbody2D>();
        if (blinkRenderers == null || blinkRenderers.Length == 0)
            blinkRenderers = GetComponentsInChildren<SpriteRenderer>();

        checkpointPosition = transform.position;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateHealthBar();
    }

    void Update()
    {
        // Desvanece el destello rojo
        if (damageFlash != null && damageFlash.color.a > 0f)
        {
            Color c = damageFlash.color;
            c.a = Mathf.MoveTowards(c.a, 0f, damageFlashFade * Time.unscaledDeltaTime * damageFlashAlpha);
            damageFlash.color = c;
        }
    }

    // ---------- Dano ----------

    /// <summary>Version simple (compatible con el codigo anterior). Sin empuje.</summary>
    public void TakeDamage(float damage)
    {
        ApplyDamage(damage, null, false, true);
    }

    /// <summary>
    /// Version completa: con la posicion de la fuente para el arco del escudo y el empuje.
    /// Pon blockable = false para dano que el escudo no detiene (acido, rayos, caidas).
    /// </summary>
    public void TakeDamage(float damage, Vector2 sourcePosition, bool isProjectile = false, bool blockable = true)
    {
        ApplyDamage(damage, sourcePosition, isProjectile, blockable);
    }

    private void ApplyDamage(float damage, Vector2? source, bool isProjectile, bool blockable)
    {
        if (isDead || invulnerable || damage <= 0f) return;

        if (blockable && shield != null)
        {
            bool blocked = source.HasValue
                ? shield.TryBlock(damage, source.Value, isProjectile)
                : shield.TryBlock(damage);
            if (blocked) return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateHealthBar();
        OnDamaged?.Invoke();

        if (damageFlash != null)
        {
            Color c = damageFlash.color;
            c.a = damageFlashAlpha;
            damageFlash.color = c;
        }

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        if (source.HasValue) ApplyKnockback(source.Value);
        StartInvulnerability();
    }

    private void ApplyKnockback(Vector2 source)
    {
        if (body == null) return;

        float dir = transform.position.x >= source.x ? 1f : -1f;
        float vy = Mathf.Min(knockbackVertical, maxKnockbackVertical);
        body.linearVelocity = new Vector2(dir * knockbackHorizontal, vy);
        // Unity 2022 o anterior: usa body.velocity en lugar de body.linearVelocity
    }

    // ---------- Curacion y checkpoints ----------

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();
    }

    /// <summary>Llamar desde los checkpoints o desde el jefe derrotado.</summary>
    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    // ---------- Invulnerabilidad ----------

    private void StartInvulnerability()
    {
        if (invulnerabilityRoutine != null) StopCoroutine(invulnerabilityRoutine);
        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine());
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        invulnerable = true;
        float end = Time.time + invulnerabilityTime;
        bool visible = false;

        while (Time.time < end)
        {
            SetSpritesVisible(visible);
            visible = !visible;
            yield return new WaitForSeconds(blinkInterval);
        }

        SetSpritesVisible(true);
        invulnerable = false;
        invulnerabilityRoutine = null;
    }

    private void SetSpritesVisible(bool visible)
    {
        foreach (var r in blinkRenderers)
            if (r != null) r.enabled = visible;
    }

    // ---------- UI ----------

    void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = Health01;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ---------- Muerte ----------

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
            invulnerabilityRoutine = null;
        }
        SetSpritesVisible(true);
        invulnerable = false;

        Debug.Log("Healing Knight ha caido.");
        OnDied?.Invoke();

        if (respawnAtCheckpoint)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
        else if (gameOverPanel != null)
        {
            // Muestra el panel y congela el juego hasta que el jugador pulse reiniciar
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // Sin panel: reinicia la escena tras una breve pausa
            Invoke(nameof(RestartLevel), restartDelay);
        }
    }

    /// <summary>Reaparece en el ultimo checkpoint con salud completa (GDD 5.9).</summary>
    public void Respawn()
    {
        transform.position = checkpointPosition;
        if (body != null) body.linearVelocity = Vector2.zero;

        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthBar();

        StartInvulnerability(); // breve proteccion al reaparecer
        OnRespawned?.Invoke();
        // Aqui puedes reiniciar la carga de Linfocito y los enemigos del tramo (escuchando OnRespawned).
    }

    // Conecta este metodo al evento OnClick del boton "Reintentar"
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}