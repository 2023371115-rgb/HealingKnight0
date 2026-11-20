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
    /// <summary>LINFOCITO T que sale disparado contra el virus y le pega un golpazo al llegar.</summary>
    public class TCellStrike : MonoBehaviour
    {
        private EnemyHealth target;
        private float damage, life = 3f;

        public void Launch(EnemyHealth enemy, float amount)
        {
            target = enemy;
            damage = amount;
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(transform, CodeSprites.Glow, new Color(0.4f, 1f, 0.6f, 0.5f), 36, Vector2.zero, Vector2.one * 2f, "Brillo");
            CodeSprites.Add(transform, c, new Color(0.1f, 0.3f, 0.15f), 37, Vector2.zero, Vector2.one * 0.95f, "Contorno");
            CodeSprites.Add(transform, c, new Color(0.55f, 0.95f, 0.6f), 38, Vector2.zero, Vector2.one * 0.82f, "Cuerpo");
            CodeSprites.Add(transform, c, new Color(0.25f, 0.6f, 0.35f), 39, new Vector2(0.05f, -0.04f), Vector2.one * 0.5f, "Nucleo");
            foreach (float ex in new[] { -0.12f, 0.14f })
                CodeSprites.Add(transform, c, Color.white, 40, new Vector2(ex, 0.1f), new Vector2(0.14f, 0.1f), "OjoEnojado");
            Sfx.Effect("Jugador_Dash", 0.6f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f || target == null || !target.IsAlive) { Destroy(gameObject); return; }
            Vector2 p = transform.position, d = (Vector2)target.transform.position - p;
            if (d.magnitude < 0.8f)
            {
                target.ApplyDamage(DamageInfo.Create(damage, DamageKind.Projectile, Team.Player, p));
                Sfx.EffectAt("Coagulo_Disuelve", p, 0.8f);
                Destroy(gameObject);
                return;
            }
            transform.position = p + d.normalized * 14f * dt;
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 20f) * 0.08f);
        }
    }
}
