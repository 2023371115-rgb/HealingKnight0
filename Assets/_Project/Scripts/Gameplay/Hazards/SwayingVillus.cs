using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// VELLOSIDAD intestinal gigante: un "dedo" de tejido plantado en el piso que se estira y se encoge despacio.
    /// Te paras en la punta (plataforma de un solo sentido que sube y baja y te lleva). Biología: las vellosidades cubren el intestino delgado y absorben
    /// los nutrientes; el ROTAVIRUS las daña y las aplana (por eso da diarrea: el intestino ya no absorbe el agua).
    /// En el juego: si un rotavirus ('flattener') anda cerca de la punta, la vellosidad se va encogiendo;
    /// cuando ya no hay rotavirus cerca, se recupera despacio.
    /// El objeto se pone en la base (el piso o el fondo del lago). Se dibuja por código.
    /// </summary>
    public class SwayingVillus : MonoBehaviour
    {
        [SerializeField] private float height = 3f;
        [SerializeField] private float tipWidth = 1.5f;
        [Tooltip("Vaivén de lado (0 = bien plantada; solo sube y baja).")]
        [SerializeField] private float swayAmount = 0f;
        [Tooltip("Cuánto sube y baja la punta (se estira y se encoge como si respirara).")]
        [SerializeField] private float bobAmount = 0.2f;
        [SerializeField] private float swaySpeed = 1.1f;
        [Header("Rotavirus")]
        [SerializeField] private EnemyData flattener;
        [SerializeField] private float flattenRadius = 3f;
        [SerializeField] private float flattenSpeed = 0.18f;   // por segundo con un rotavirus cerca
        [SerializeField] private float recoverSpeed = 0.05f;
        [SerializeField, Range(0f, 0.9f)] private float maxFlatten = 0.65f;
        [Header("Aspecto")]
        [SerializeField] private Color tissue = new Color(1f, 0.62f, 0.55f, 1f);
        [SerializeField] private Color shade = new Color(0.86f, 0.42f, 0.42f, 1f);
        [SerializeField] private Color outline = new Color(0.3f, 0.07f, 0.1f, 1f);
        [SerializeField] private Color sick = new Color(0.72f, 0.5f, 0.42f, 1f);
        [Header("Dibujo (opcional; pivote abajo al centro)")]
        [SerializeField] private Sprite healthyArt;
        [SerializeField] private Sprite damagedArt;
        [SerializeField] private float artWidth = 2.1f;

        private Transform tip;
        private Transform[] stalk;
        private SpriteRenderer[] tissueRenderers;
        private float flatten, seed;
        private Vector2 lastTip;
        private Transform artRoot;
        private SpriteRenderer healthyRenderer, damagedRenderer;

        public float Flatten01 => flatten;

        public void SetArt(Sprite healthy, Sprite damaged)
        {
            healthyArt = healthy;
            damagedArt = damaged;
        }

        public void Configure(float villusHeight, float phase, EnemyData rotavirus)
        {
            height = villusHeight;
            seed = phase;
            flattener = rotavirus;
        }

        private void Start()
        {
            if (seed == 0f) seed = transform.position.x * 0.9f;
            Build();
            lastTip = TipPosition();
            tip.position = lastTip;
        }

        private float CurrentHeight => height * (1f - flatten);

        private Vector2 TipPosition()
        {
            float sway = Mathf.Sin(Time.time * swaySpeed + seed) * swayAmount * (1f - flatten * 0.5f);
            float bob = Mathf.Sin(Time.time * swaySpeed * 1.3f + seed) * bobAmount * (1f - flatten);
            return (Vector2)transform.position + new Vector2(sway, CurrentHeight + bob);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateFlatten(dt);

            Vector2 now = TipPosition();
            Vector2 delta = now - lastTip;
            PlayerController player = PlayerController.Current;
            if (player != null && player.Motor != null)
            {
                Vector2 feet = player.Position;
                bool onTop = Mathf.Abs(feet.x - lastTip.x) <= tipWidth * 0.5f + 0.1f && Mathf.Abs(feet.y - lastTip.y) < 0.2f
                             && player.Motor.Velocity.y <= 0.5f;
                if (onTop) player.Motor.Body.position += delta;
            }
            tip.position = now;
            lastTip = now;
        }

        private void UpdateFlatten(float dt)
        {
            if (flattener == null) return;
            bool near = false;
            Vector2 top = lastTip;
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive || e.Data != flattener) continue;
                if (Vector2.Distance(e.transform.position, top) <= flattenRadius) { near = true; break; }
            }
            flatten = near ? Mathf.Min(maxFlatten, flatten + flattenSpeed * dt) : Mathf.Max(0f, flatten - recoverSpeed * dt);
        }

        private void Update()
        {
            if (artRoot != null) { UpdateArt(); return; }
            if (stalk == null) return;
            // El tallo es una curva de la base a la punta (se dobla con el vaivén).
            Vector2 b = transform.position;
            Vector2 t = tip.position;
            for (int i = 0; i < stalk.Length; i++)
            {
                float k = (i + 0.5f) / stalk.Length;
                float bend = k * k;                 // la base casi no se mueve; la punta sí
                Vector2 p = new Vector2(Mathf.Lerp(b.x, t.x, bend), Mathf.Lerp(b.y, t.y - 0.25f, k));
                stalk[i].position = p;
            }
            Color c = Color.Lerp(tissue, sick, flatten / Mathf.Max(0.01f, maxFlatten));
            foreach (SpriteRenderer r in tissueRenderers) r.color = c;
        }

        /// <summary>Con dibujo: se estira a la altura actual, se inclina hacia la punta y se pasa al dibujo dañado al aplanarse.</summary>
        private void UpdateArt()
        {
            Vector2 b = transform.position;
            Vector2 t = tip.position;
            float h = Mathf.Max(0.3f, t.y - b.y);
            float angle = -Mathf.Atan2(t.x - b.x, h) * Mathf.Rad2Deg;
            artRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            float k = Mathf.Clamp01(flatten / Mathf.Max(0.01f, maxFlatten));
            Fit(healthyRenderer, h, 1f - k);
            Fit(damagedRenderer, h, k);
        }

        private void Fit(SpriteRenderer r, float h, float alpha)
        {
            if (r == null || r.sprite == null) return;
            Vector2 size = r.sprite.bounds.size;
            r.transform.localScale = new Vector3(artWidth / size.x, h / size.y, 1f);
            Color c = r.color;
            c.a = alpha;
            r.color = c;
            r.enabled = alpha > 0.02f;
        }

        // ---------- dibujo por código ----------

        private void Build()
        {
            Sprite circle = Circle();
            tip = new GameObject("Punta").transform;
            tip.SetParent(transform, false);
            tip.gameObject.layer = PhysicsLayers.Ground;
            var box = tip.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(tipWidth * 0.85f, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            box.usedByEffector = true;
            var effector = tip.gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            if (healthyArt != null)
            {
                artRoot = new GameObject("Dibujo").transform;
                artRoot.SetParent(transform, false);
                healthyRenderer = ArtChild(artRoot, healthyArt, -7);
                if (damagedArt != null) damagedRenderer = ArtChild(artRoot, damagedArt, -6);
                return;
            }

            var tissueList = new System.Collections.Generic.List<SpriteRenderer>();
            int n = Mathf.Max(4, Mathf.CeilToInt(height / 0.45f));
            stalk = new Transform[n];
            float w = tipWidth * 0.7f;
            for (int i = 0; i < n; i++)
            {
                var s = new GameObject("Tallo" + i).transform;
                s.SetParent(transform, false);
                Add(s, circle, outline, -8, Vector2.zero, new Vector2(w + 0.14f, 0.7f));
                tissueList.Add(Add(s, circle, tissue, -7, Vector2.zero, new Vector2(w, 0.62f)));
                Add(s, circle, shade, -6, new Vector2(w * 0.22f, 0f), new Vector2(w * 0.35f, 0.5f));
                stalk[i] = s;
            }
            // Cabeza redonda de la vellosidad (donde te paras).
            Add(tip, circle, outline, -5, new Vector2(0f, -0.3f), new Vector2(tipWidth + 0.14f, 0.8f));
            tissueList.Add(Add(tip, circle, tissue, -4, new Vector2(0f, -0.3f), new Vector2(tipWidth, 0.7f)));
            Add(tip, circle, new Color(1f, 0.88f, 0.84f, 0.9f), -3, new Vector2(-tipWidth * 0.2f, -0.15f), new Vector2(tipWidth * 0.3f, 0.14f));
            tissueRenderers = tissueList.ToArray();
        }

        private static SpriteRenderer ArtChild(Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(sprite.name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        private static SpriteRenderer Add(Transform parent, Sprite sprite, Color color, int order, Vector2 pos, Vector2 size)
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

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.55f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * height);
        }
    }
}
