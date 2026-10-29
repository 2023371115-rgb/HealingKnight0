using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    public float damage = 10f;
    public float damageCooldown = 1f; // evita daño cada frame mientras se tocan

    private float lastHitTime = -999f;

    // Usa este si el collider del enemigo NO es trigger (choca físicamente)
    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    // Usa este si el collider del enemigo SÍ es trigger
    void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastHitTime < damageCooldown) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            lastHitTime = Time.time;
        }
    }
}