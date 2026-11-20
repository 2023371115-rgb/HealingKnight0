using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>Bala sencilla (de un linfocito infectado): vuela derecho y lastima al jugador.</summary>
    public class SimpleShot : MonoBehaviour
    {
        private Vector2 velocity;
        private float damage, life = 3.5f;

        public void Launch(Vector2 v, float amount)
        {
            velocity = v;
            damage = amount;
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.3f, 0.1f, 0.4f), 30, Vector2.zero, Vector2.one * 0.42f, "Contorno");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.8f, 0.5f, 1f), 31, Vector2.zero, Vector2.one * 0.32f, "Bala");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            transform.position += (Vector3)(velocity * dt);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.OverlapsCircle(transform.position, 0.25f)) return;
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Projectile, Team.Enemy, transform.position);
            info.knockback = 2f;
            box.Damageable.ApplyDamage(info);
            Destroy(gameObject);
        }
    }
}
