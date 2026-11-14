using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// H. pylori con UREASA: en la vida real esta enzima convierte la urea en amoniaco, que neutraliza el ácido
    /// alrededor de la bacteria (así sobrevive en el estómago).
    /// En el juego: dentro de su burbuja el ácido NO te quema. Es un enemigo, pero te conviene pelear cerca de él.
    /// La burbuja se dibuja sola (círculo claro que late) y se apaga cuando la bacteria muere.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class UreaseAura : MonoBehaviour
    {
        private static readonly List<UreaseAura> active = new List<UreaseAura>();

        [SerializeField] private float radius = 2.2f;
        [SerializeField] private Color color = new Color(0.75f, 1f, 0.95f, 0.22f);

        private EnemyHealth health;
        private SpriteRenderer bubble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();

        /// <summary>¿Ese punto está dentro de la burbuja de alguna H. pylori con ureasa viva?</summary>
        public static bool Protects(Vector2 point)
        {
            foreach (UreaseAura a in active)
            {
                if (a == null || a.health == null || !a.health.IsAlive) continue;
                if (((Vector2)a.transform.position - point).sqrMagnitude <= a.radius * a.radius) return true;
            }
            return false;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            var go = new GameObject("BurbujaUreasa");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            bubble = go.AddComponent<SpriteRenderer>();
            bubble.sprite = SoftCircle();
            bubble.color = color;
            bubble.sortingOrder = 29; // justo detrás del ácido (30): se ve el "hueco" claro dentro del líquido
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Update()
        {
            bool alive = health != null && health.IsAlive;
            bubble.enabled = alive;
            if (!alive) return;
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
            float d = radius * 2f * pulse;
            bubble.transform.localScale = new Vector3(d, d, 1f);
        }

        private static Sprite cached;

        private static Sprite SoftCircle()
        {
            if (cached != null) return cached;
            const int n = 64;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Relleno suave con una orilla un poco más marcada (burbuja de caricatura).
                    float a = d > 1f ? 0f : Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0.75f, 0.95f, d)) * Mathf.Clamp01((1f - d) * 20f);
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            cached = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return cached;
        }
    }
}
