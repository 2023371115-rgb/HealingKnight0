using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// BURBUJA DE OXÍGENO (O₂): el aire bueno que llega a los alvéolos. Tócala y te cura un poco.
    /// Reaparece después de unos segundos. Se dibuja por código (o 'art').
    /// </summary>
    public class OxygenBubble : MonoBehaviour
    {
        [SerializeField] private float heal = 8f;
        [SerializeField] private float respawn = 9f;
        [SerializeField] private Sprite art;

        private Transform visual;
        private Vector2 home;
        private float hiddenFor, seed;

        public void Configure(float amount, Sprite drawing) { heal = amount; art = drawing; }

        private void Start()
        {
            home = transform.position;
            seed = Random.value * 10f;
            visual = new GameObject("Burbuja").transform;
            visual.SetParent(transform, false);
            if (art != null) { CodeSprites.Art(visual, art, 0.9f, 22, Vector2.zero); return; }
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(visual, c, new Color(0.3f, 0.75f, 1f, 0.35f), 20, Vector2.zero, new Vector2(1.1f, 1.1f));
            CodeSprites.Add(visual, c, new Color(0.08f, 0.25f, 0.45f, 1f), 21, Vector2.zero, new Vector2(0.8f, 0.8f));
            CodeSprites.Add(visual, c, new Color(0.6f, 0.92f, 1f, 1f), 22, Vector2.zero, new Vector2(0.7f, 0.7f));
            // "O₂": dos bolitas pegadas (molécula) en blanco.
            CodeSprites.Add(visual, c, Color.white, 23, new Vector2(-0.11f, 0f), new Vector2(0.26f, 0.26f));
            CodeSprites.Add(visual, c, Color.white, 23, new Vector2(0.11f, 0f), new Vector2(0.26f, 0.26f));
            CodeSprites.Add(visual, c, new Color(1f, 1f, 1f, 0.8f), 24, new Vector2(-0.18f, 0.2f), new Vector2(0.16f, 0.1f));
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (hiddenFor > 0f)
            {
                hiddenFor -= dt;
                if (hiddenFor <= 0f) visual.gameObject.SetActive(true);
                return;
            }
            float t = Time.time + seed;
            transform.position = home + new Vector2(BreathingCycle.Flow * 0.4f, Mathf.Sin(t * 2f) * 0.25f);
            visual.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.05f);

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (Vector2.Distance(player.Position + Vector2.up * 0.9f, transform.position) > 1f) return;
            if (player.Health.Current >= player.Health.Max) return;
            player.Health.Heal(heal);
            Sfx.EffectAt("Alveolo_Infla", transform.position, 0.8f);
            visual.gameObject.SetActive(false);
            hiddenFor = respawn;
        }
    }
}
