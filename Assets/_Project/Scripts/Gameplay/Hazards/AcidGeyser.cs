using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Chorro de ácido (las células parietales de la pared del estómago fabrican el ácido clorhídrico).
    /// Cada cierto tiempo burbujea como AVISO y luego dispara una columna de ácido hacia arriba que quema.
    /// Con omeprazol (AcidLake.Neutralize) no dispara.
    /// El objeto se pone donde nace el chorro (el fondo del lago o el piso); se dibuja solo por código.
    /// Con SetColors sirve también para la bilis del intestino delgado (sale por la ampolla de Vater).
    /// </summary>
    public class AcidGeyser : MonoBehaviour
    {
        [SerializeField] private float height = 4.5f;
        [SerializeField] private float width = 0.9f;
        [SerializeField] private float interval = 3.5f;
        [SerializeField] private float offset = 0f;          // para que varios chorros no disparen a la vez
        [SerializeField] private float warningTime = 1f;
        [SerializeField] private float eruptTime = 1.3f;
        [SerializeField] private float damage = 12f;

        [Header("Aspecto")]
        [SerializeField] private Color body = new Color(0.52f, 0.86f, 0.22f, 0.9f);
        [SerializeField] private Color outline = new Color(0.12f, 0.26f, 0.08f, 1f);
        [SerializeField] private Color foam = new Color(0.86f, 1f, 0.6f, 1f);
        [SerializeField] private int sortingOrder = 31;

        private enum Phase { Idle, Warning, Erupt }
        private Phase phase = Phase.Idle;
        private float timer;
        private float current;            // altura actual de la columna
        private bool hitThisErupt;
        private SpriteRenderer column, columnOutline, cap, capOutline;
        private SpriteRenderer[] bubbles;
        // Dibujo caricatura: columna de "bolas" que se bambolean, corona de espuma, gotas que salpican y charco en la base.
        private const int Lumps = 8, CrownBits = 5, Drops = 6;
        private SpriteRenderer[] lumpFill, lumpLine, crownFill, crownLine, drops;
        private SpriteRenderer shine, splashFill, splashLine;

        /// <summary>¿Está disparando ahora? (las gotas de grasa del intestino se rompen con el chorro de bilis)</summary>
        public bool IsErupting => phase == Phase.Erupt;
        /// <summary>Altura (Y del mundo) a la que llega la punta del chorro en este momento.</summary>
        public float TopY => transform.position.y + current;
        public float Width => width;

        /// <summary>Colores del chorro (por ejemplo, bilis amarilla en el intestino delgado).</summary>
        public void SetColors(Color columnColor, Color outlineColor, Color foamColor)
        {
            body = columnColor;
            outline = outlineColor;
            foam = foamColor;
        }

        public void Configure(float columnHeight, float period, float startOffset)
        {
            height = columnHeight;
            interval = period;
            offset = startOffset;
        }

        private void Start()
        {
            timer = interval + offset;
            Sprite square = MakeSquare();
            Sprite circle = MakeCircle();
            columnOutline = Make("Contorno", square, outline, sortingOrder);
            column = Make("Chorro", square, body, sortingOrder + 1);
            capOutline = Make("EspumaContorno", circle, outline, sortingOrder + 3);
            cap = Make("Espuma", circle, foam, sortingOrder + 4);
            lumpLine = new SpriteRenderer[Lumps];
            lumpFill = new SpriteRenderer[Lumps];
            for (int i = 0; i < Lumps; i++)
            {
                lumpLine[i] = Make("BolaContorno", circle, outline, sortingOrder);
                lumpFill[i] = Make("Bola", circle, body, sortingOrder + 1);
            }
            shine = Make("Brillo", circle, new Color(1f, 1f, 1f, 0.55f), sortingOrder + 2);
            crownLine = new SpriteRenderer[CrownBits];
            crownFill = new SpriteRenderer[CrownBits];
            for (int i = 0; i < CrownBits; i++)
            {
                crownLine[i] = Make("CoronaContorno", circle, outline, sortingOrder + 3);
                crownFill[i] = Make("Corona", circle, foam, sortingOrder + 4);
            }
            drops = new SpriteRenderer[Drops];
            for (int i = 0; i < Drops; i++) drops[i] = Make("Gota", circle, body, sortingOrder + 2);
            splashLine = Make("CharcoContorno", circle, outline, sortingOrder + 2);
            splashFill = Make("Charco", circle, foam, sortingOrder + 3);
            bubbles = new SpriteRenderer[5];
            for (int i = 0; i < bubbles.Length; i++) bubbles[i] = Make("Burbuja", circle, foam, sortingOrder + 3);
            Show(0f, false);
        }

        private SpriteRenderer Make(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            timer -= dt;
            switch (phase)
            {
                case Phase.Idle:
                    current = Mathf.MoveTowards(current, 0f, dt * height * 2.5f);
                    if (timer <= 0f)
                    {
                        if (AcidLake.IsNeutralized) { timer = 0.5f; break; }
                        phase = Phase.Warning;
                        timer = warningTime;
                        Sfx.EffectAt("Acido_Burbujeo", transform.position, 0.8f);
                    }
                    break;
                case Phase.Warning:
                    current = Mathf.MoveTowards(current, 0.35f, dt * 2f);   // se asoma un poquito: el aviso
                    if (timer <= 0f)
                    {
                        phase = Phase.Erupt;
                        timer = eruptTime;
                        hitThisErupt = false;
                        Sfx.EffectAt("Lombriz_Sale", transform.position, 0.7f);
                    }
                    break;
                case Phase.Erupt:
                    current = Mathf.MoveTowards(current, height, dt * height * 6f);
                    TryDamage();
                    if (timer <= 0f)
                    {
                        phase = Phase.Idle;
                        timer = interval;
                    }
                    break;
            }
            Show(current, phase != Phase.Idle || current > 0.05f);
        }

        private void TryDamage()
        {
            if (hitThisErupt) return;
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 c = transform.position;
            Vector2 feet = player.Position;
            // El jugador mide ~1.9 de alto: si cualquier parte de él toca la columna, quema.
            bool inside = Mathf.Abs(feet.x - c.x) <= width * 0.5f + 0.35f && feet.y < c.y + current && feet.y + 1.8f > c.y;
            if (!inside) return;
            hitThisErupt = true;
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, c);
            info.blockable = false;
            info.knockback = 6f;
            player.Health.ApplyDamage(info);
            Sfx.Effect("Acido_Quema", 0.9f);
        }

        private void Show(float h, bool visible)
        {
            if (column == null) return;
            float t = Time.time;
            bool erupting = phase == Phase.Erupt;
            bool warning = phase == Phase.Warning;

            // Columna recta de fondo (rellena los huecos entre las bolas).
            column.enabled = columnOutline.enabled = visible && h > 0.4f;
            float w = width * (0.82f + Mathf.Sin(t * 8f) * 0.05f);
            column.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            column.transform.localScale = new Vector3(w, h, 1f);
            columnOutline.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            columnOutline.transform.localScale = new Vector3(w + 0.16f, h, 1f);

            // Bolas que suben por la columna y se bambolean (se ve líquido y gordito, no un rectángulo).
            for (int i = 0; i < Lumps; i++)
            {
                bool on = visible && h > 0.3f;
                lumpFill[i].enabled = lumpLine[i].enabled = on;
                if (!on) continue;
                float k = Mathf.Repeat(i / (float)Lumps + t * (erupting ? 1.6f : 0.6f), 1f);
                float y = h * k;
                float x = Mathf.Sin(t * 5f + i * 1.7f) * width * 0.12f;
                float size = width * (1.05f + Mathf.Sin(t * 7f + i) * 0.12f) * Mathf.Lerp(1.1f, 0.85f, k);
                lumpFill[i].transform.localPosition = new Vector3(x, y, 0f);
                lumpFill[i].transform.localScale = new Vector3(size, size * 0.9f, 1f);
                lumpLine[i].transform.localPosition = new Vector3(x, y, 0f);
                lumpLine[i].transform.localScale = new Vector3(size + 0.16f, size * 0.9f + 0.16f, 1f);
            }

            // Brillo vertical del lado izquierdo.
            shine.enabled = visible && h > 0.8f;
            shine.transform.localPosition = new Vector3(-width * 0.22f, h * 0.55f, 0f);
            shine.transform.localScale = new Vector3(width * 0.16f, h * 0.6f, 1f);

            // Corona de espuma arriba: una bola grande y cinco chicas alrededor que palpitan.
            cap.enabled = capOutline.enabled = visible;
            float capSize = width * (1.45f + Mathf.Sin(t * 10f) * 0.08f);
            cap.transform.localPosition = new Vector3(0f, h, 0f);
            cap.transform.localScale = new Vector3(capSize, capSize * 0.75f, 1f);
            capOutline.transform.localPosition = new Vector3(0f, h, 0f);
            capOutline.transform.localScale = new Vector3(capSize + 0.16f, capSize * 0.75f + 0.16f, 1f);
            for (int i = 0; i < CrownBits; i++)
            {
                bool on = visible && h > 0.5f;
                crownFill[i].enabled = crownLine[i].enabled = on;
                if (!on) continue;
                float a = Mathf.Lerp(20f, 160f, i / (float)(CrownBits - 1)) * Mathf.Deg2Rad;
                float r = width * 0.75f;
                float pulse = 1f + Mathf.Sin(t * 12f + i * 1.3f) * 0.15f;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, h + Mathf.Sin(a) * r * 0.6f, 0f);
                float size = width * 0.55f * pulse;
                crownFill[i].transform.localPosition = p;
                crownFill[i].transform.localScale = new Vector3(size, size, 1f);
                crownLine[i].transform.localPosition = p;
                crownLine[i].transform.localScale = new Vector3(size + 0.14f, size + 0.14f, 1f);
            }

            // Gotas que salpican desde arriba en arcos (solo cuando dispara).
            for (int i = 0; i < Drops; i++)
            {
                drops[i].enabled = erupting && h > 1f;
                if (!drops[i].enabled) continue;
                float k = Mathf.Repeat(t * 1.4f + i * 0.37f, 1f);
                float side = i % 2 == 0 ? 1f : -1f;
                float spread = (0.6f + (i % 3) * 0.35f) * side;
                Vector3 p = new Vector3(spread * k * 1.6f, h + 1.4f * k - 3.2f * k * k, 0f);
                float size = width * (0.32f - 0.12f * k);
                drops[i].transform.localPosition = p;
                drops[i].transform.localScale = new Vector3(size, size * 1.2f, 1f);
            }

            // Charco de espuma en la base (en el aviso se infla: ahí va a salir).
            bool splash = warning || erupting || h > 0.05f;
            splashFill.enabled = splashLine.enabled = splash;
            float sw = width * (warning ? 1.4f + Mathf.Sin(t * 30f) * 0.12f : 1.9f);
            splashFill.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            splashFill.transform.localScale = new Vector3(sw, sw * 0.32f, 1f);
            splashLine.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            splashLine.transform.localScale = new Vector3(sw + 0.16f, sw * 0.32f + 0.16f, 1f);

            for (int i = 0; i < bubbles.Length; i++)
            {
                bool on = warning || erupting;
                bubbles[i].enabled = on;
                if (!on) continue;
                float k = Mathf.Repeat(t * (1.2f + i * 0.17f) + i * 0.37f, 1f);
                float y = warning ? k * 0.9f : h * k;
                float x = Mathf.Sin(i * 2.3f + t * 3f) * width * 0.6f;
                float s2 = 0.12f + 0.06f * (i % 3);
                bubbles[i].transform.localPosition = new Vector3(x, y, 0f);
                bubbles[i].transform.localScale = new Vector3(s2, s2, 1f);
            }
        }

        private static Sprite square, circle;

        private static Sprite MakeSquare()
        {
            if (square != null) return square;
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return square;
        }

        private static Sprite MakeCircle()
        {
            if (circle != null) return circle;
            const int n = 64;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 24f));
                }
            tex.SetPixels(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return circle;
        }
    }
}
