using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// FRAGMENTO DE MEMORIA (hipocampo): el hipocampo guarda los recuerdos nuevos y la rabia los está destruyendo.
    /// Recógelos antes de que se apaguen: cuando te acercas empieza a "olvidarse" (se va apagando) y si tardas mucho
    /// desaparece. Cada uno cura un poco; si recoges TODOS los del nivel ganas una bomba extra.
    /// </summary>
    public class MemoryFragment : MonoBehaviour
    {
        private static readonly List<MemoryFragment> all = new List<MemoryFragment>();
        private static int collected, lost;

        [SerializeField] private float forgetTime = 14f;
        [SerializeField] private float heal = 6f;
        [SerializeField, TextArea] private string memory = "";
        [SerializeField] private Sprite art;

        private Transform visual;
        private SpriteRenderer glow;
        private float left;
        private bool started, done;
        private Vector2 home;

        public void Configure(string text, Sprite drawing) { memory = text; art = drawing; }

        private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        private void OnDisable() => all.Remove(this);

        private void Start()
        {
            if (all.Count > 0 && all[0] == this) { collected = 0; lost = 0; }
            home = transform.position;
            left = forgetTime;
            visual = new GameObject("Recuerdo").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            glow = CodeSprites.Add(visual, c, new Color(1f, 0.85f, 0.4f, 0.35f), 20, Vector2.zero, new Vector2(1.3f, 1.3f));
            if (art != null) CodeSprites.Art(visual, art, 0.8f, 22, Vector2.zero);
            else
            {
                // Pieza de rompecabezas dorada (un pedacito de recuerdo).
                CodeSprites.Add(visual, CodeSprites.Square, new Color(0.35f, 0.15f, 0.05f, 1f), 21, Vector2.zero, new Vector2(0.62f, 0.62f)).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                CodeSprites.Add(visual, CodeSprites.Square, new Color(1f, 0.8f, 0.35f, 1f), 22, Vector2.zero, new Vector2(0.5f, 0.5f)).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                CodeSprites.Add(visual, c, new Color(1f, 0.8f, 0.35f, 1f), 22, new Vector2(0f, 0.34f), new Vector2(0.24f, 0.24f));
                CodeSprites.Add(visual, c, new Color(1f, 1f, 0.9f, 0.9f), 23, new Vector2(-0.08f, 0.08f), new Vector2(0.14f, 0.14f));
            }
        }

        private void Update()
        {
            if (done) return;
            float dt = Time.deltaTime, t = Time.time + home.x;
            transform.position = home + new Vector2(0f, Mathf.Sin(t * 2f) * 0.2f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 12f);

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            float d = Vector2.Distance(player.Position + Vector2.up * 0.9f, transform.position);
            if (!started && d < 9f) started = true;
            if (started)
            {
                left -= dt;
                float k = Mathf.Clamp01(left / forgetTime);
                glow.color = new Color(1f, 0.85f, 0.4f, 0.1f + 0.3f * k);
                visual.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, k);
                if (left < 3f) visual.gameObject.SetActive(Mathf.Sin(Time.time * 18f) > -0.3f);
                if (left <= 0f)
                {
                    done = true;
                    lost++;
                    visual.gameObject.SetActive(false);
                    GameplayEvents.RaiseMessage("Un recuerdo se perdió... la rabia lo borró.");
                    return;
                }
            }
            if (d > 1f) return;

            done = true;
            collected++;
            visual.gameObject.SetActive(false);
            player.Health.Heal(heal);
            int total = all.Count;
            Sfx.Effect("Bomba_Ganada", 0.6f);
            string head = $"Recuerdo recuperado ({collected}/{total})";
            if (collected == total && lost == 0)
            {
                player.Bombs.Add(1);
                GameplayEvents.RaiseMessage(head + ". ¡Todos a salvo! +1 bomba.");
            }
            else GameplayEvents.RaiseMessage(string.IsNullOrEmpty(memory) ? head : head + ": " + memory);
        }
    }
}
