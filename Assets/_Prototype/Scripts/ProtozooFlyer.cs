using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ProtozooFlyer : MonoBehaviour
{
    private enum State { Orbit, Windup, Dash, Recover, Barrage }

    [Header("Orbita alrededor del jugador")]
    public Transform player;                   // si queda vacío, busca el tag "Player"
    public float orbitRadius = 4f;
    public float orbitAngularSpeed = 60f;    // grados por segundo
    public float heightOffset = 1.5f;        // vuela un poco por encima del jugador
    public float moveSpeed = 5f;
    public float steering = 6f;              // qué tan rápido corrige su velocidad
    public float bobAmplitude = 0.4f;        // vaivén para que se vea "flotando"
    public float bobFrequency = 2f;

    [Header("Ataque (embestida)")]
    public float attackInterval = 4f;        // segundos entre ataques
    public float windupTime = 0.6f;          // se detiene y "apunta"
    public float dashSpeed = 14f;
    public float dashDuration = 0.45f;
    public float recoverTime = 0.8f;

    [Header("Esquivar ataques")]
    public LayerMask projectileLayers;       // capa de los proyectiles del jugador
    public float detectRadius = 3f;
    [Range(0f, 1f)] public float dodgeChance = 0.3f;  // probabilidad de esquivar cada proyectil
    public float dodgeSpeed = 9f;
    public float dodgeDuration = 0.2f;
    public float dodgeCooldown = 2.5f;

    [Header("Ataque en todas direcciones (en el punto más alto)")]
    public GameObject enemyProjectilePrefab;
    [Range(0f, 1f)] public float barrageChance = 0.5f; // probabilidad al llegar arriba
    public float barrageCooldown = 8f;
    public float barrageWindup = 0.5f;                     // se queda quieto antes de disparar
    public int projectileCount = 8;                      // proyectiles por oleada
    public int waves = 3;                                // <--- Ajustado a 3 oleadas
    public float waveInterval = 0.6f;
    public float projectileSpeed = 6f;

    private Rigidbody2D rb;
    private State state = State.Orbit;
    private float stateTimer;
    private float nextAttackTime;
    private float angle;
    private float orbitDir = 1f;
    private Vector2 dashDirection;
    private float dodgeUntil;
    private float nextDodgeTime;
    private Vector2 dodgeVelocity;
    private float nextBarrageTime;
    private float nextWaveTime;
    private int wavesFired;
    private bool barrageQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Empieza en el ángulo en el que ya está respecto al jugador
        if (player != null)
        {
            Vector2 d = (Vector2)transform.position - (Vector2)player.position;
            angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }
        nextAttackTime = Time.time + attackInterval;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // El esquive tiene prioridad, salvo mientras embiste
        if (Time.time < dodgeUntil && state != State.Dash)
        {
            rb.linearVelocity = dodgeVelocity;
            return;
        }

        switch (state)
        {
            case State.Orbit: DoOrbit(); break;
            case State.Windup: DoWindup(); break;
            case State.Dash: DoDash(); break;
            case State.Recover: DoRecover(); break;
            case State.Barrage: DoBarrage(); break;
        }
    }

    // ---------- ORBITA ----------
    void DoOrbit()
    {
        // Cuando termina el enfriamiento, decide si hará el ataque aéreo
        if (!barrageQueued && enemyProjectilePrefab != null && Time.time >= nextBarrageTime)
        {
            if (Random.value < barrageChance) barrageQueued = true;
            else nextBarrageTime = Time.time + 2f; // si no sale, reintenta en 2 s
        }

        float step = orbitAngularSpeed * Time.fixedDeltaTime;
        float diffToTop = Mathf.DeltaAngle(angle, 90f);

        if (barrageQueued)
            angle += Mathf.Clamp(diffToTop, -step, step); // va por el camino más corto hacia arriba
        else
            angle += step * orbitDir;

        Vector2 center = (Vector2)player.position + Vector2.up * heightOffset;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 target = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        target.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;

        // No baja del nivel del jugador: es un enemigo volador
        target.y = Mathf.Max(target.y, player.position.y + 0.5f);

        Vector2 desired = (target - rb.position) * moveSpeed;
        desired = Vector2.ClampMagnitude(desired, moveSpeed * 2f);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desired, steering * Time.fixedDeltaTime);

        CheckProjectiles();

        // Llegó al punto más alto: se queda quieto y empieza el ataque
        if (barrageQueued && Mathf.Abs(diffToTop) < 3f && Vector2.Distance(rb.position, target) < 1f)
        {
            barrageQueued = false;
            state = State.Barrage;
            wavesFired = 0;
            nextWaveTime = Time.time + barrageWindup;
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            state = State.Windup;
            stateTimer = windupTime;
        }
    }

    // ---------- PREPARA EL ATAQUE ----------
    void DoWindup()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            // Apunta a donde está el jugador justo al lanzarse
            dashDirection = ((Vector2)player.position - rb.position).normalized;
            state = State.Dash;
            stateTimer = dashDuration;
        }
    }

    // ---------- EMBESTIDA ----------
    void DoDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            state = State.Recover;
            stateTimer = recoverTime;
        }
    }

    // ---------- RECUPERACIÓN ----------
    void DoRecover()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            orbitDir = Random.value > 0.5f ? 1f : -1f; // cambia el sentido para no ser predecible
            nextAttackTime = Time.time + attackInterval;
            state = State.Orbit;
        }
    }

    // ---------- DISPARO EN TODAS DIRECCIONES ----------
    void DoBarrage()
    {
        // Se queda estático (y vulnerable: no esquiva mientras dispara)
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);

        if (Time.time < nextWaveTime) return;

        if (wavesFired < waves)
        {
            FireWave(wavesFired);
            wavesFired++;
            nextWaveTime = Time.time + waveInterval;
        }
        else
        {
            // Terminó: vuelve a orbitar
            nextBarrageTime = Time.time + barrageCooldown;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 1.5f); // no embiste de inmediato
            state = State.Orbit;
        }
    }

    void FireWave(int waveIndex)
    {
        // Las oleadas impares se desfasan para dejar huecos distintos
        float offset = (waveIndex % 2 == 0) ? 0f : 180f / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            float a = offset + i * (360f / projectileCount);
            GameObject go = Instantiate(enemyProjectilePrefab, transform.position, Quaternion.Euler(0f, 0f, a));

            EnemyProjectile ep = go.GetComponent<EnemyProjectile>();
            if (ep != null) ep.speed = projectileSpeed;
        }
    }

    // ---------- ESQUIVAR PROYECTILES ----------
    void CheckProjectiles()
    {
        if (Time.time < nextDodgeTime) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, detectRadius, projectileLayers);
        foreach (Collider2D h in hits)
        {
            Rigidbody2D prb = h.attachedRigidbody;
            if (prb == null) continue;

            Vector2 vel = prb.linearVelocity;
            if (vel.sqrMagnitude < 0.01f) continue;

            // ¿El proyectil se acerca hacia mí?
            Vector2 toMe = rb.position - (Vector2)h.transform.position;
            if (Vector2.Dot(vel.normalized, toMe.normalized) < 0.7f) continue;

            // A veces "no lo ve venir": así el jugador puede acertar
            if (Random.value > dodgeChance)
            {
                nextDodgeTime = Time.time + 0.6f; // evita volver a tirar el dado cada frame
                return;
            }

            // Esquiva de lado, perpendicular a la trayectoria
            Vector2 perp = new Vector2(-vel.y, vel.x).normalized;
            if (Vector2.Dot(perp, toMe) < 0f) perp = -perp;
            // Un poco hacia arriba también, para que se sienta volador
            dodgeVelocity = (perp + Vector2.up * 0.5f).normalized * dodgeSpeed;

            dodgeUntil = Time.time + dodgeDuration;
            nextDodgeTime = Time.time + dodgeCooldown;
            return;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (player != null)
            Gizmos.DrawWireSphere((Vector2)player.position + Vector2.up * heightOffset, orbitRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}