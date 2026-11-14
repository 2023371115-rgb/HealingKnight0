using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// PLACA DE PEYER: grupos de células de defensa (linfocitos) en la pared del intestino delgado, sobre todo en el íleon.
    /// "Vigilan" lo que pasa por el intestino. En el juego es un punto de guardado: al pasar se enciende,
    /// te cura un poco y, si mueres más adelante, reapareces aquí.
    /// Se dibuja por código: bolitas moradas (folículos) que brillan al activarse.
    /// </summary>
    public class PeyerPatch : MonoBehaviour
    {
        [SerializeField] private float heal = 20f;
        [SerializeField, TextArea] private string message = "NODO: Placa de Peyer. Aquí vigilan los linfocitos del intestino. Punto de guardado.";
        [SerializeField] private Color off = new Color(0.55f, 0.42f, 0.7f, 1f);
        [SerializeField] private Color on = new Color(0.85f, 0.75f, 1f, 1f);
        [SerializeField] private Color outline = new Color(0.2f, 0.08f, 0.25f, 1f);
        [Header("Dibujo (opcional; pivote abajo al centro)")]
        [SerializeField] private Sprite offArt;
        [SerializeField] private Sprite onArt;
        [SerializeField] private float artWidth = 2.8f;

        private SpriteRenderer[] follicles;
        private SpriteRenderer glow;
        private bool active;
        private float pulse;
        private SpriteRenderer offRenderer, onRenderer;

        public void SetArt(Sprite dim, Sprite lit)
        {
            offArt = dim;
            onArt = lit;
        }

        private void Start() => Build();

        private void Update()
        {
            float t = Time.time;
            if (!active)
            {
                PlayerController player = PlayerController.Current;
                if (player != null && player.Health.IsAlive && player.Position.x >= transform.position.x - 0.5f) Activate(player);
            }
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime);
            if (offRenderer != null)
            {
                // Con dibujo: cambia del dibujo apagado al encendido y late un poco.
                offRenderer.enabled = !active || onRenderer == null;
                if (onRenderer != null) onRenderer.enabled = active;
                float sc = 1f + pulse * 0.25f + (active ? Mathf.Sin(t * 3f) * 0.02f : 0f);
                offRenderer.transform.parent.localScale = new Vector3(sc, sc, 1f);
                Color gg = glow.color;
                gg.a = active ? 0.2f + Mathf.Sin(t * 2f) * 0.06f + pulse * 0.4f : 0f;
                glow.color = gg;
                return;
            }
            for (int i = 0; i < follicles.Length; i++)
            {
                float k = active ? 0.85f + Mathf.Sin(t * 3f + i) * 0.15f : 0f;
                follicles[i].color = Color.Lerp(off, on, k);
                float s = 1f + pulse * 0.4f;
                follicles[i].transform.parent.localScale = new Vector3(s, s, 1f);
            }
            Color g = glow.color;
            g.a = active ? 0.25f + Mathf.Sin(t * 2f) * 0.08f + pulse * 0.4f : 0f;
            glow.color = g;
        }

        private void Activate(PlayerController player)
        {
            active = true;
            pulse = 1f;
            if (LevelManager.Current != null) LevelManager.Current.SetCheckpoint(transform.position.x + 1f);
            player.Health.Heal(heal);
            GameplayEvents.RaiseMessage(message);
            Sfx.Effect("Bomba_Ganada", 0.7f);
        }

        private void Build()
        {
            Sprite circle = Circle();
            glow = Make(transform, circle, new Color(on.r, on.g, on.b, 0f), -12, new Vector2(0f, 0.8f), new Vector2(4f, 2.4f));
            if (offArt != null)
            {
                var holder = new GameObject("Dibujo").transform;
                holder.SetParent(transform, false);
                float k = artWidth / Mathf.Max(0.01f, offArt.bounds.size.x);
                offRenderer = Make(holder, offArt, Color.white, -10, Vector2.zero, new Vector2(k, k));
                if (onArt != null)
                {
                    onRenderer = Make(holder, onArt, Color.white, -10, Vector2.zero, new Vector2(k, k));
                    onRenderer.enabled = false;
                }
                follicles = new SpriteRenderer[0];
                return;
            }
            Vector2[] pos = { new Vector2(-1.1f, 0.35f), new Vector2(-0.35f, 0.55f), new Vector2(0.45f, 0.5f), new Vector2(1.15f, 0.3f), new Vector2(0.05f, 1.15f), new Vector2(-0.75f, 1.05f), new Vector2(0.8f, 1.05f) };
            follicles = new SpriteRenderer[pos.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                var f = new GameObject("Foliculo" + i).transform;
                f.SetParent(transform, false);
                f.localPosition = pos[i];
                float size = i >= 4 ? 0.65f : 0.8f;
                Make(f, circle, outline, -11, Vector2.zero, new Vector2(size + 0.12f, size + 0.12f));
                follicles[i] = Make(f, circle, off, -10, Vector2.zero, new Vector2(size, size));
                Make(f, circle, new Color(1f, 1f, 1f, 0.7f), -9, new Vector2(-size * 0.2f, size * 0.2f), new Vector2(size * 0.25f, size * 0.15f));
            }
        }

        private static SpriteRenderer Make(Transform parent, Sprite sprite, Color color, int order, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Capa");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        private static Sprite circle;

        private static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 64;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 30f));
                }
            tex.SetPixels(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return circle;
        }
    }
}
