using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// MUELA QUE MASTICA (Boca): una muela de arriba que baja de golpe y aplasta, a ritmo.
    /// Avisa temblando antes de bajar. Si te agarra debajo: daño y te escupe a un lado. Cruza entre mordidas.
    /// Biología: las muelas (molares) muelen la comida; la mordida humana aprieta con mucha fuerza.
    /// </summary>
    public class ChewingMolar : MonoBehaviour
    {
        [SerializeField] private float width = 3f;
        [SerializeField] private float topY = 8f;          // altura de reposo (arriba)
        [SerializeField] private float period = 2.6f;
        [SerializeField] private float offset;
        [SerializeField] private float damage = 18f;
        [SerializeField] private Sprite art;
        [Tooltip("Color de la encía de la que cuelga la muela (alfa 0 = sin encía). Sube hasta la encía de arriba.")]
        [SerializeField] private Color gum = new Color(0f, 0f, 0f, 0f);

        private Transform tooth;
        private float time;
        private bool hitThisBite;

        public void Configure(float w, float top, float cycle, float phase, Sprite drawing, Color gumColor = default(Color))
        {
            width = w;
            topY = top;
            period = cycle;
            offset = phase;
            art = drawing;
            gum = gumColor;
        }

        private void Start()
        {
            time = offset;
            tooth = new GameObject("Muela").transform;
            tooth.SetParent(transform, false);
            if (gum.a > 0f)
            {
                // La muela es de la quijada de arriba: un pedazo de encía la sostiene y sube hasta el techo
                // (lo que pasa arriba de la encía del techo queda tapado por ella).
                float crown = art != null ? width * 1.1f * art.rect.height / Mathf.Max(1f, art.rect.width) * 0.85f : 2.4f;
                Color gumLine = new Color(gum.r * 0.45f, gum.g * 0.3f, gum.b * 0.4f);
                CodeSprites.Add(tooth, CodeSprites.Square, gumLine, 3, new Vector2(0f, crown + 6f), new Vector2(width * 0.92f + 0.14f, 12f), "EnciaContorno");
                CodeSprites.Add(tooth, CodeSprites.Square, gum, 4, new Vector2(0f, crown + 6f), new Vector2(width * 0.92f, 12f), "Encia");
            }
            if (art != null) { CodeSprites.Art(tooth, art, width * 1.1f, 5, Vector2.zero); return; }
            Color line = new Color(0.25f, 0.18f, 0.2f), enamel = new Color(0.98f, 0.96f, 0.9f);
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            // Corona (abajo) con dos raíces que suben.
            CodeSprites.Add(tooth, sq, line, 4, new Vector2(0f, 1f), new Vector2(width + 0.16f, 2.1f));
            CodeSprites.Add(tooth, sq, enamel, 5, new Vector2(0f, 1f), new Vector2(width, 1.95f));
            for (int s = -1; s <= 1; s += 2)
            {
                CodeSprites.Add(tooth, c, line, 4, new Vector2(s * width * 0.3f, 0.05f), new Vector2(width * 0.45f, 0.7f));
                CodeSprites.Add(tooth, c, enamel, 5, new Vector2(s * width * 0.3f, 0.1f), new Vector2(width * 0.4f, 0.6f));
                CodeSprites.Add(tooth, sq, line, 3, new Vector2(s * width * 0.28f, 2.6f), new Vector2(width * 0.26f, 1.4f));
                CodeSprites.Add(tooth, sq, new Color(0.95f, 0.88f, 0.8f), 3, new Vector2(s * width * 0.28f, 2.6f), new Vector2(width * 0.2f, 1.35f));
            }
            CodeSprites.Add(tooth, c, new Color(1f, 1f, 1f, 0.7f), 6, new Vector2(-width * 0.3f, 1.5f), new Vector2(width * 0.2f, 0.5f));
        }

        // 0 = arriba, 1 = abajo (mordiendo).
        private float Bite(float t)
        {
            float ph = Mathf.Repeat(t, period) / period;
            if (ph < 0.55f) return 0f;                                  // arriba
            if (ph < 0.65f) return Mathf.SmoothStep(0f, 1f, (ph - 0.55f) / 0.1f); // baja rápido
            if (ph < 0.8f) return 1f;                                   // aprieta
            return 1f - Mathf.SmoothStep(0f, 1f, (ph - 0.8f) / 0.2f);   // sube
        }

        private void Update()
        {
            time += Time.deltaTime;
            float k = Bite(time);
            float ph = Mathf.Repeat(time, period) / period;
            float shake = ph > 0.4f && ph < 0.55f ? Mathf.Sin(time * 60f) * 0.06f : 0f;
            float bottom = Mathf.Lerp(topY, 0f, k);
            tooth.localPosition = new Vector3(shake, bottom, 0f);

            if (k >= 0.99f && !hitThisBite)
            {
                hitThisBite = true;
                Sfx.EffectAt("Muela_Mastica", transform.position, 0.8f);
                Camera cam = Camera.main;
                if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.15f, 0.15f);
            }
            if (k < 0.5f) hitThisBite = false;

            PlayerController p = PlayerController.Current;
            if (p == null || !p.Health.IsAlive || k < 0.5f) return;
            Vector2 f = p.Position;
            float local = f.y - transform.position.y;
            if (Mathf.Abs(f.x - transform.position.x) > width * 0.5f || local > bottom + 0.2f || local < -0.5f) return;
            float side = f.x < transform.position.x ? -1f : 1f;
            p.Motor.Teleport(new Vector2(transform.position.x + side * (width * 0.5f + 0.6f), f.y));
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, transform.position);
            info.blockable = false;
            info.knockback = 6f;
            p.Health.ApplyDamage(info);
        }
    }
}
