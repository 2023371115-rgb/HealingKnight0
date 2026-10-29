using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float damage = 8f;
    public float lifeTime = 4f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    void Start()
    {
        // La dirección viene de la rotación con la que se instanció
        rb.linearVelocity = (Vector2)transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora otros triggers (el propio Protozoo, proyectiles del jugador, etc.)
        if (other.isTrigger) return;

        // Nace dentro del Protozoo: ignora a cualquier enemigo para no destruirse al instante
        if (other.GetComponent<Enemy>() != null) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }

        // Se destruye al tocar al jugador o cualquier superficie sólida (suelo)
        Destroy(gameObject);
    }
}