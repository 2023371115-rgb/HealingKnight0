using UnityEngine;

public class ShieldBubble : MonoBehaviour
{
    public PlayerShield shield;

    void OnTriggerEnter2D(Collider2D other)
    {
        shield.HandleHit(other);
    }
}