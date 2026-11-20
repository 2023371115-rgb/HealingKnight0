using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CRISTAL DE SAL que flota en el asa de Henle (allá abajo el agua está muy salada): tiene picos, gira y sube y baja.
    /// Si lo tocas te lastima; tres balazos lo rompen.
    /// </summary>
    public class SaltCrystal : MonoBehaviour
    {
        [SerializeField] private float bob = 1.2f;
        [SerializeField] private float damage = 8f;

        private Transform visual;
        private Vector3 home;
        private float seed, hurtCooldown;
        private int hits;

        public void Configure(float bobDistance) => bob = bobDistance;

        private void Start()
        {
            home = transform.position;
            seed = home.x * 0.7f;
            visual = new GameObject("Cristal").transform;
            visual.SetParent(transform, false);
            Color edge = new Color(0.35f, 0.3f, 0.25f);
            for (int i = 0; i < 4; i++)
            {
                Transform spike = new GameObject("Pico").transform;
                spike.SetParent(visual, false);
                spike.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                CodeSprites.Add(spike, CodeSprites.Square, edge, 34, Vector2.zero, new Vector2(1.3f, 0.3f), "Contorno");
                CodeSprites.Add(spike, CodeSprites.Square, new Color(0.97f, 0.97f, 1f), 35, Vector2.zero, new Vector2(1.15f, 0.18f), "Sal");
            }
            CodeSprites.Add(visual, CodeSprites.Square, edge, 36, Vector2.zero, Vector2.one * 0.6f, "Centro");
            CodeSprites.Add(visual, CodeSprites.Square, new Color(0.92f, 0.95f, 1f), 37, Vector2.zero, Vector2.one * 0.46f, "CentroClaro");
            CodeSprites.Add(visual, CodeSprites.Glow, new Color(1f, 1f, 1f, 0.35f), 33, Vector2.zero, Vector2.one * 1.6f, "Brillo");
        }

        private void Update()
        {
            if (visual == null) return;
            float dt = Time.deltaTime;
            if (hurtCooldown > 0f) hurtCooldown -= dt;
            transform.position = home + new Vector3(0f, Mathf.Sin(Time.time * 1.1f + seed) * bob, 0f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f + seed * 10f);

            Vector2 c = transform.position;
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile p = Projectile.Flying[i];
                if (p == null || p.SourceTeam != Team.Player || ((Vector2)p.Position - c).sqrMagnitude > 0.55f * 0.55f) continue;
                p.Consume();
                if (++hits >= 3)
                {
                    Sfx.EffectAt("Coagulo_Disuelve", c, 0.6f);
                    Destroy(gameObject);
                    return;
                }
                visual.localScale = Vector3.one * (1f - hits * 0.15f);
            }

            PlayerController player = PlayerController.Current;
            if (player == null || hurtCooldown > 0f || !player.Health.IsAlive) return;
            if ((player.Motor.Center - c).sqrMagnitude > 0.75f * 0.75f) return;
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, c);
            info.knockback = 6f;
            player.Health.ApplyDamage(info);
            hurtCooldown = 0.8f;
        }
    }
}
