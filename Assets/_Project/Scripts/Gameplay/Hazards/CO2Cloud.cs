using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// NUBE DE CO₂ (dióxido de carbono): el gas de desecho que la sangre deja en los alvéolos para que lo exhales.
    /// Si te quedas dentro te asfixia poco a poco (daño cada 0.5 s). Flota con la respiración: la exhalación la
    /// empuja hacia atrás y la inhalación hacia adelante, dentro de su zona. Se dibuja por código (o 'art').
    /// </summary>
    public class CO2Cloud : MonoBehaviour
    {
        [SerializeField] private float radius = 1.3f;
        [SerializeField] private float drift = 2.5f;
        [SerializeField] private float damagePerTick = 3f;
        [SerializeField] private Sprite art;

        private Vector2 home;
        private float tick, seed;
        private Transform visual;

        public void Configure(float size, float range, Sprite drawing)
        {
            radius = size;
            drift = range;
            art = drawing;
        }

        private void Start()
        {
            home = transform.position;
            seed = Random.value * 10f;
            visual = new GameObject("Nube").transform;
            visual.SetParent(transform, false);
            if (art != null) { CodeSprites.Art(visual, art, radius * 2.3f, 30, Vector2.zero); return; }
            Color dark = new Color(0.32f, 0.3f, 0.38f, 0.55f), light = new Color(0.55f, 0.52f, 0.6f, 0.5f);
            Sprite c = CodeSprites.Circle;
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * radius * 0.55f;
                CodeSprites.Add(visual, c, dark, 30, p, Vector2.one * radius * 1.1f);
            }
            CodeSprites.Add(visual, c, light, 31, Vector2.zero, Vector2.one * radius * 1.3f);
            // Cara de "gas tóxico" (dos ojitos y boca torcida) para que se lea como peligro.
            CodeSprites.Add(visual, c, new Color(0.12f, 0.1f, 0.16f, 0.9f), 32, new Vector2(-0.25f, 0.12f), new Vector2(0.16f, 0.2f));
            CodeSprites.Add(visual, c, new Color(0.12f, 0.1f, 0.16f, 0.9f), 32, new Vector2(0.25f, 0.12f), new Vector2(0.16f, 0.2f));
            CodeSprites.Add(visual, CodeSprites.Square, new Color(0.12f, 0.1f, 0.16f, 0.9f), 32, new Vector2(0f, -0.2f), new Vector2(0.4f, 0.07f))
                .transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        }

        private void Update()
        {
            float dt = Time.deltaTime, t = Time.time + seed;
            Vector2 p = transform.position;
            float targetX = home.x + BreathingCycle.Flow * drift;
            p.x = Mathf.MoveTowards(p.x, targetX, 1.8f * dt);
            p.y = home.y + Mathf.Sin(t * 1.3f) * 0.3f;
            transform.position = p;
            if (visual != null)
            {
                visual.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.1f) * 0.06f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * 6f);
            }

            tick -= dt;
            PlayerController player = PlayerController.Current;
            if (tick > 0f || player == null || !player.Health.IsAlive) return;
            Vector2 body = player.Position + Vector2.up * 0.9f;
            if (Vector2.Distance(body, p) > radius) return;
            tick = 0.5f;
            DamageInfo info = DamageInfo.Create(damagePerTick, DamageKind.Contact, Team.Enemy, p);
            info.blockable = false;
            info.knockback = 0f;
            player.Health.ApplyDamage(info);
        }
    }
}
