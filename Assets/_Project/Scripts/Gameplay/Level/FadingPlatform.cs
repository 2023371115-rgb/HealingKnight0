using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// BURBUJA DE MEMORIA (hipocampo): un recuerdo que se sostiene mientras lo "repasas". Si te quedas QUIETO encima
    /// se desvanece rápido (un recuerdo sin usar se olvida); si sigues moviéndote dura más. Al desaparecer vuelve a
    /// formarse unos segundos después.
    /// </summary>
    public class FadingPlatform : MonoBehaviour
    {
        [SerializeField] private float width = 2.2f;
        [SerializeField] private float life = 1.4f;
        [SerializeField] private float respawn = 2.8f;
        [SerializeField] private Color fill = new Color(1f, 0.78f, 0.4f, 0.85f);

        private BoxCollider2D box;
        private Transform visual;
        private SpriteRenderer[] parts;
        private float[] baseAlpha;
        private float used, hidden;

        public void Configure(float platformWidth, float duration) { width = platformWidth; life = duration; }

        private void Start()
        {
            box = CodeSprites.OneWay(gameObject, width);
            visual = new GameObject("Burbuja").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            int n = Mathf.Max(2, Mathf.RoundToInt(width / 0.75f));
            for (int i = 0; i < n; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * width / n, s = width / n * 1.3f;
                CodeSprites.Add(visual, c, new Color(0.35f, 0.12f, 0.05f, 0.9f), -11, new Vector2(x, -s * 0.32f), new Vector2(s + 0.1f, s + 0.1f));
                CodeSprites.Add(visual, c, fill, -10, new Vector2(x, -s * 0.32f), new Vector2(s, s));
                CodeSprites.Add(visual, c, new Color(1f, 1f, 1f, 0.6f), -9, new Vector2(x - s * 0.2f, -s * 0.12f), new Vector2(s * 0.28f, s * 0.2f));
            }
            // Destellito adentro (el recuerdo).
            CodeSprites.Add(visual, c, new Color(1f, 0.95f, 0.75f, 0.9f), -9, new Vector2(0f, -0.3f), new Vector2(0.3f, 0.3f));
            parts = visual.GetComponentsInChildren<SpriteRenderer>();
            baseAlpha = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) baseAlpha[i] = parts[i].color.a;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (hidden > 0f)
            {
                hidden -= dt;
                float k = hidden < 0.5f ? 1f - hidden / 0.5f : 0f;   // se vuelve a formar
                SetAlpha(k * 0.6f);
                visual.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
                if (hidden <= 0f) { box.enabled = true; used = 0f; SetAlpha(1f); visual.localScale = Vector3.one; }
                return;
            }

            PlayerController player = PlayerController.Current;
            if (CodeSprites.Standing(player, transform.position, width))
            {
                bool still = Mathf.Abs(player.Motor.Velocity.x) < 1f;
                used += dt * (still ? 1f : 0.35f);
            }
            else used = Mathf.Max(0f, used - dt * 0.5f);

            float left = 1f - used / life;
            SetAlpha(Mathf.Lerp(0.25f, 1f, left));
            visual.localPosition = used > 0f ? new Vector3(Mathf.Sin(Time.time * 40f) * 0.04f * (1f - left), 0f, 0f) : Vector3.zero;
            visual.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2.5f + transform.position.x) * 0.03f);
            if (used >= life)
            {
                box.enabled = false;
                hidden = respawn;
                SetAlpha(0f);
                Sfx.EffectAt("Memoria_Desvanece", transform.position, 0.6f);
            }
        }

        private void SetAlpha(float a)
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                Color c = parts[i].color;
                c.a = baseAlpha[i] * a;
                parts[i].color = c;
            }
        }
    }
}
