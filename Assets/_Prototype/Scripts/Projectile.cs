using UnityEngine;

public enum MedicationType
{
    Antibiotico,
    Antiviral,
    Antiinflamatorio,
    Antihistaminico,
    Antiparasitario // cápsula única del estómago
}

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 2.5f;
    public float baseDamage = 10f; // daño base del GDD (5.6)

    [Header("Rebote (medicamento ineficaz)")]
    public float bounceSpeedMultiplier = 0.5f;
    public float bounceLifeTime = 0.4f;
    public float bounceAngleVariation = 40f; // grados de desvío aleatorio

    [HideInInspector] public MedicationType type;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool hasHit;
    private Vector2 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        startPos = transform.position;                         // <-- NUEVA
        rb.linearVelocity = (Vector2)transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    // NUEVO: pégalo debajo de Start()
    void OnDestroy()
    {
        Debug.Log("Alcance: " + Vector2.Distance(startPos, transform.position).ToString("F1"));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (other.CompareTag("Player")) return;

        // El escenario destruye la cápsula (GDD 5.6). Requiere una capa llamada "Ground".
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            Effectiveness result = enemy.ReceiveHit(type, baseDamage);

            if (result == Effectiveness.Ineffective)
            {
                Bounce();
                return;
            }
        }

        Destroy(gameObject);
    }

    void Bounce()
    {
        hasHit = true;

        // Evita que el proyectil rebotado vuelva a golpear al enemigo
        if (col != null) col.enabled = false;

        // Sale en dirección contraria con un pequeño desvío aleatorio
        Vector2 back = -rb.linearVelocity.normalized;
        float angle = Random.Range(-bounceAngleVariation, bounceAngleVariation);
        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * back;

        rb.linearVelocity = dir * speed * bounceSpeedMultiplier;

        // Feedback: se vuelve gris, como indica el GDD
        if (sr != null) sr.color = Color.gray;

        // Destruye pronto (el Destroy original de Start queda pendiente, pero este vence antes)
        Destroy(gameObject, bounceLifeTime);
    }
}