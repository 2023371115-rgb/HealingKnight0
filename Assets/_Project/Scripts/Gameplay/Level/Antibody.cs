using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>Anticuerpo en forma de Y que vuela al enemigo y le hace daño al llegar.</summary>
    public class Antibody : MonoBehaviour
    {
        private EnemyHealth target;
        private float damage, life = 2.5f;
        private Vector2 lastTargetPos;

        public void Launch(EnemyHealth enemy, float amount)
        {
            target = enemy; damage = amount;
            lastTargetPos = enemy.transform.position;
            Sprite sq = CodeSprites.Square;
            Color col = new Color(1f, 0.92f, 0.4f);
            CodeSprites.Add(transform, sq, col, 35, new Vector2(0f, -0.12f), new Vector2(0.08f, 0.28f));
            SpriteRenderer a = CodeSprites.Add(transform, sq, col, 35, new Vector2(-0.08f, 0.1f), new Vector2(0.07f, 0.24f));
            a.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            SpriteRenderer b = CodeSprites.Add(transform, sq, col, 35, new Vector2(0.08f, 0.1f), new Vector2(0.07f, 0.24f));
            b.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (target != null && target.IsAlive) lastTargetPos = (Vector2)target.transform.position + Vector2.up * 0.4f;
            Vector2 p = transform.position;
            Vector2 d = lastTargetPos - p;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            if (d.magnitude < 0.5f)
            {
                if (target != null && target.IsAlive)
                {
                    AbscessBoss abscess = target.GetComponent<AbscessBoss>();
                    if (abscess != null) abscess.AntibodyHit(damage, p);
                    else target.ApplyDamage(DamageInfo.Create(damage, DamageKind.Projectile, Team.Player, p));
                }
                Destroy(gameObject);
                return;
            }
            transform.position = p + d.normalized * 13f * dt;
        }
    }
}
