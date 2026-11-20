using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// BOTÓN que se activa a balazos (una arteriola, la hormona ADH...). Prendido brilla verde. Con 'holdTime' > 0 se
    /// apaga solo después de ese tiempo; con 0 cada balazo lo prende/apaga.
    /// </summary>
    public class ShootSwitch : MonoBehaviour
    {
        [SerializeField] private float holdTime = 6f;
        [SerializeField] private float radius = 1f;
        [SerializeField] private string label = "";

        private SpriteRenderer lamp, ring, halo;
        private float timer, cooldown;

        public bool On { get; private set; }
        public event System.Action<bool> Changed;

        public void Configure(float seconds, string text)
        {
            holdTime = seconds;
            label = text;
        }

        private void Start()
        {
            // Halo que late mientras está APAGADO, para que se vea que hay que dispararle.
            halo = CodeSprites.Add(transform, CodeSprites.Glow, new Color(1f, 0.85f, 0.4f, 0.7f), 19, Vector2.zero, Vector2.one * 3f, "Halo");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.25f, 0.1f, 0.08f), 20, Vector2.zero, Vector2.one * 1.5f, "Contorno");
            ring = CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.85f, 0.5f, 0.35f), 21, Vector2.zero, Vector2.one * 1.3f, "Valvula");
            lamp = CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.4f, 0.2f, 0.15f), 22, Vector2.zero, Vector2.one * 0.7f, "Luz");
            if (!string.IsNullOrEmpty(label))
                WorldText.Create(transform, label, new Vector2(0f, 1.2f), new Vector2(3f, 0.6f), Color.white, 23, null, 3f);
        }

        private void Set(bool on)
        {
            if (On == on) return;
            On = on;
            lamp.color = on ? new Color(0.5f, 1f, 0.55f) : new Color(0.4f, 0.2f, 0.15f);
            ring.color = on ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.85f, 0.5f, 0.35f);
            Sfx.EffectAt("Recoger_Vida", transform.position, 0.5f);
            Changed?.Invoke(on);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (cooldown > 0f) cooldown -= dt;
            if (halo != null)
            {
                halo.enabled = !On || holdTime <= 0f;
                halo.transform.localScale = Vector3.one * (2.6f + 0.5f * Mathf.Sin(Time.time * 5f));
            }
            if (On && holdTime > 0f)
            {
                timer -= dt;
                lamp.transform.localScale = Vector3.one * (0.7f + 0.15f * Mathf.Sin(Time.time * 10f));
                if (timer <= 0f) Set(false);
            }
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile p = Projectile.Flying[i];
                if (p == null || p.SourceTeam != Team.Player) continue;
                if (((Vector2)p.Position - (Vector2)transform.position).sqrMagnitude > radius * radius) continue;
                p.Consume();
                if (cooldown > 0f) continue;
                cooldown = 0.25f;
                if (holdTime > 0f) { timer = holdTime; Set(true); }
                else Set(!On);
            }
        }
    }
}
