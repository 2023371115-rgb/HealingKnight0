using UnityEngine;

public class AcidZone : MonoBehaviour
{
    public float damagePerTick = 8f;
    public float tickInterval = 0.5f; // cada cuánto hace daño mientras el jugador está dentro

    private float timer;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damagePerTick);
            }
            timer = 0f;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) timer = 0f;
    }
}