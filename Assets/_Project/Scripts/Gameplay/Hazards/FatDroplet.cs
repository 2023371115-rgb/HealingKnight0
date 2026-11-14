using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// GOTA DE GRASA flotando sobre la bilis (duodeno). Sirve de plataforma de un solo sentido.
    /// Biología: la bilis EMULSIONA la grasa: rompe las gotas grandes en gotitas chiquitas para que las enzimas la digieran.
    /// En el juego: si un chorro de bilis (AcidGeyser) le pega, la gota se rompe en gotitas y deja de sostenerte;
    /// unos segundos después se vuelve a juntar. Hay que cruzarla antes de que llegue el chorro.
    /// El objeto marca la parte de ARRIBA de la gota (donde se pisa). Se dibuja por código.
    /// </summary>
    public class FatDroplet : MonoBehaviour
    {
        [SerializeField] private float width = 1.7f;
        [SerializeField] private float height = 0.9f;
        [SerializeField] private float reformTime = 2.8f;
        [Tooltip("Busca el chorro de bilis más cercano a esta distancia (en X).")]
        [SerializeField] private float geyserRange = 1.2f;
        [SerializeField] private AcidGeyser geyser;
        [SerializeField] private Color fill = new Color(1f, 0.93f, 0.55f, 0.95f);
        [SerializeField] private Color outline = new Color(0.36f, 0.24f, 0.05f, 1f);
        [SerializeField] private Color shine = new Color(1f, 1f, 0.92f, 0.95f);

        private BoxCollider2D floor;
        private Transform whole;
        private Transform[] bits;
        private Vector3[] bitDir;
        private float brokenTimer, breakAnim, seed;
        private Vector3 basePos;
        private bool broken;

        public bool IsBroken => broken;

        public void Configure(float dropWidth, AcidGeyser linkedGeyser)
        {
            width = dropWidth;
            geyser = linkedGeyser;
        }

        private void Start()
        {
            basePos = transform.position;
            seed = basePos.x * 1.3f;
            if (geyser == null) geyser = FindGeyser();
            Build();
        }

        private AcidGeyser FindGeyser()
        {
            AcidGeyser best = null;
            float bestDx = geyserRange;
#pragma warning disable CS0618
            foreach (AcidGeyser g in FindObjectsByType<AcidGeyser>(FindObjectsSortMode.None))
#pragma warning restore CS0618
            {
                float dx = Mathf.Abs(g.transform.position.x - transform.position.x);
                if (dx <= bestDx && g.transform.position.y < transform.position.y) { best = g; bestDx = dx; }
            }
            return best;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            // Se mece arriba y abajo y lleva al jugador con ella.
            Vector3 before = transform.position;
            float bob = broken ? 0f : Mathf.Sin(Time.time * 1.6f + seed) * 0.12f;
            transform.position = basePos + new Vector3(0f, bob, 0f);
            float dy = transform.position.y - before.y;
            PlayerController player = PlayerController.Current;
            if (!broken && dy != 0f && OnTop(player)) player.Motor.Body.position += new Vector2(0f, dy);

            if (!broken)
            {
                if (geyser != null && geyser.IsErupting && geyser.TopY >= transform.position.y - height) Break();
            }
            else
            {
                brokenTimer -= dt;
                if (brokenTimer <= 0f) Reform();
            }
        }

        private bool OnTop(PlayerController player)
        {
            if (player == null || player.Motor == null) return false;
            Vector2 feet = player.Position;
            return Mathf.Abs(feet.x - transform.position.x) <= width * 0.5f + 0.1f
                   && Mathf.Abs(feet.y - transform.position.y) < 0.2f && player.Motor.Velocity.y <= 0.5f;
        }

        private void Break()
        {
            broken = true;
            brokenTimer = reformTime;
            breakAnim = 0f;
            floor.enabled = false;
            Sfx.EffectAt("Alveolo_Desinfla", transform.position, 0.7f);
        }

        private void Reform()
        {
            broken = false;
            floor.enabled = true;
            Sfx.EffectAt("Alveolo_Rebote", transform.position, 0.4f);
        }

        private void Update()
        {
            if (whole == null) return;
            float t = Time.time;
            if (broken)
            {
                // Se dispersa en gotitas, y al final del tiempo se vuelven a juntar.
                breakAnim += Time.deltaTime;
                float spread = Mathf.Clamp01(breakAnim / 0.35f);
                float regroup = Mathf.Clamp01(1f - brokenTimer / 0.6f);
                float k = spread * (1f - regroup);
                whole.gameObject.SetActive(false);
                for (int i = 0; i < bits.Length; i++)
                {
                    bits[i].gameObject.SetActive(true);
                    bits[i].localPosition = bitDir[i] * k * 1.3f + new Vector3(0f, -height * 0.5f + Mathf.Sin(t * 3f + i) * 0.05f, 0f);
                }
            }
            else
            {
                whole.gameObject.SetActive(true);
                foreach (Transform b in bits) b.gameObject.SetActive(false);
                // Gelatinosa: se aplasta un poquito.
                float s = Mathf.Sin(t * 3f + seed) * 0.04f;
                whole.localScale = new Vector3(1f + s, 1f - s, 1f);
            }
        }

        // ---------- dibujo por código ----------

        private void Build()
        {
            gameObject.layer = PhysicsLayers.Ground;
            floor = gameObject.AddComponent<BoxCollider2D>();
            floor.size = new Vector2(width * 0.85f, 0.25f);
            floor.offset = new Vector2(0f, -0.125f);
            floor.usedByEffector = true;
            var effector = gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            Sprite circle = Circle();
            whole = new GameObject("Gota").transform;
            whole.SetParent(transform, false);
            Vector2 c = new Vector2(0f, -height * 0.5f);
            Add(whole, circle, outline, 1, c, new Vector2(width + 0.12f, height + 0.12f));
            Add(whole, circle, fill, 2, c, new Vector2(width, height));
            Add(whole, circle, shine, 3, c + new Vector2(-width * 0.22f, height * 0.2f), new Vector2(width * 0.3f, height * 0.18f));

            bits = new Transform[5];
            bitDir = new Vector3[5];
            for (int i = 0; i < bits.Length; i++)
            {
                var b = new GameObject("Gotita" + i).transform;
                b.SetParent(transform, false);
                float size = 0.35f + (i % 2) * 0.12f;
                Add(b, circle, outline, 1, Vector2.zero, new Vector2(size + 0.08f, size + 0.08f));
                Add(b, circle, fill, 2, Vector2.zero, new Vector2(size, size));
                float a = (i / (float)(bits.Length - 1)) * Mathf.PI;
                bitDir[i] = new Vector3(Mathf.Cos(a) * width * 0.5f, Mathf.Sin(a) * 0.3f, 0f);
                bits[i] = b;
                b.gameObject.SetActive(false);
            }
        }

        private static void Add(Transform parent, Sprite sprite, Color color, int order, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Capa");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
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

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            Vector3 p = transform.position;
            Gizmos.DrawLine(p + new Vector3(-width * 0.5f, 0f, 0f), p + new Vector3(width * 0.5f, 0f, 0f));
        }
    }
}
