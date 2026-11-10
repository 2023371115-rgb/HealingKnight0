using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Ácido gástrico sobre el piso, de cualquier ancho, dibujado y animado por código (no necesita arte):
    /// superficie con olas "pixeleadas", cresta brillante, burbujas que suben y revientan, vapor y brillo.
    ///
    /// Daña al jugador si tiene los pies dentro del ácido (el escudo no protege). Si está saltando por encima, no.
    ///
    /// Con 'tide' activado es una MAREA (contracción del estómago): normalmente está seca; retumba y tiembla
    /// la cámara como aviso, el ácido sube hasta 'highLevel', se queda un rato y vuelve a bajar.
    /// La marea arranca cuando el jugador se acerca.
    ///
    /// El objeto se coloca en el FONDO del lago (piso de la fosa), en el centro. En el Estómago los lagos están
    /// hundidos en el piso: la superficie queda un poco abajo del borde, como un líquido dentro de una cavidad.
    /// Se dibuja ENFRENTE de los personajes y semitransparente, para que se vean hundidos (pies dentro del ácido).
    /// Los enemigos no se dañan: la H. pylori neutraliza el ácido a su alrededor (ureasa), así sobrevive en el estómago.
    /// </summary>
    public class AcidLake : MonoBehaviour
    {
        [Header("Tamaño")]
        [SerializeField] private float width = 6f;
        [Tooltip("Altura de la superficie sobre el fondo cuando está tranquilo (con marea: el nivel más bajo; 0 = se seca).")]
        [SerializeField] private float lowLevel = 0.22f;

        [Header("Daño")]
        [SerializeField] private float damagePerTick = 6f;
        [SerializeField] private float tickInterval = 0.45f;

        [Header("Marea (opcional)")]
        [SerializeField] private bool tide;
        [SerializeField] private float highLevel = 1.3f;
        [SerializeField] private float dryTime = 6f;
        [SerializeField] private float warningTime = 1.7f;
        [SerializeField] private float riseTime = 1.1f;
        [SerializeField] private float holdTime = 3.5f;
        [SerializeField] private float fallTime = 1.6f;
        [TextArea]
        [SerializeField] private string firstWarning = "¡CONTRACCIÓN! El ácido va a subir: ¡quédate sobre la comida!";

        [Header("Aspecto")]
        [SerializeField] private Color deep = new Color(0.1f, 0.32f, 0.04f, 0.9f);
        [SerializeField] private Color middle = new Color(0.24f, 0.6f, 0.07f, 0.82f);
        [SerializeField] private Color surface = new Color(0.45f, 0.88f, 0.12f, 0.88f);
        [SerializeField] private Color crest = new Color(0.8f, 1f, 0.45f, 1f);
        [Tooltip("Enfrente de los personajes (0-12) para que se vean hundidos; detrás de las plataformas no importa (quedan más arriba).")]
        [SerializeField] private int sortingOrder = 30;
        [Tooltip("Ancho de cada columna de la ola (más chico = ola más suave, más grande = más pixel art).")]
        [SerializeField] private float columnWidth = 0.2f;

        private enum Phase { Waiting, Dry, Warning, Rising, Holding, Falling }

        private Phase phase;
        private float phaseTimer;
        private float level;
        private float agitation;      // 0 tranquilo, 1 muy agitado
        private float damageTimer;
        private bool warnedOnce;

        private SpriteRenderer body, depthShade;
        private SpriteRenderer[] columns, crests;
        private Bubble[] bubbles;
        private float seed;

        private struct Bubble
        {
            public SpriteRenderer renderer;
            public float x, y, speed, size, popTimer;
            public bool vapor;
        }

        private static Sprite square, circle;

        public float SurfaceY => transform.position.y + level;
        /// <summary>Altura del ácido sobre el fondo del lago (0 = seco).</summary>
        public float Level => level;
        public float Width => width;
        public bool IsHigh => tide && (phase == Phase.Rising || phase == Phase.Holding);

        /// <summary>Daño por tick (0 = solo se ve; por ejemplo en un hueco, donde la caída ya cuesta vida).</summary>
        public void SetDamage(float perTick) => damagePerTick = perTick;

        public void Configure(float lakeWidth, float low, bool isTide, float high)
        {
            width = lakeWidth;
            lowLevel = low;
            tide = isTide;
            highLevel = high;
        }

        private void Start()
        {
            seed = transform.position.x * 0.37f;
            level = lowLevel;
            phase = tide ? Phase.Waiting : Phase.Holding;
            BuildVisual();
            SfxLoop.Attach(gameObject, "Acido_Burbujeo", tide ? 0.35f : 0.5f, width * 0.5f, width * 0.5f + 7f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (tide) UpdateTide(dt);
            else agitation = 0.15f;

            UpdateDamage(dt);
            UpdateVisual(dt);
        }

        // ---------- Marea ----------

        private void UpdateTide(float dt)
        {
            phaseTimer += dt;
            switch (phase)
            {
                case Phase.Waiting:
                    PlayerController player = PlayerController.Current;
                    if (player != null && player.Position.x > transform.position.x - width * 0.5f - 3f) Enter(Phase.Dry);
                    level = lowLevel; agitation = 0.15f;
                    break;

                case Phase.Dry: // nivel bajo (con lowLevel = 0 se seca por completo)
                    level = lowLevel; agitation = 0.15f;
                    if (phaseTimer >= dryTime) Enter(Phase.Warning);
                    break;

                case Phase.Warning:
                    level = Mathf.Lerp(lowLevel, lowLevel + 0.12f, phaseTimer / warningTime); // se infla un poquito
                    agitation = 0.4f + 0.6f * (phaseTimer / warningTime);
                    if (phaseTimer >= warningTime) Enter(Phase.Rising);
                    break;

                case Phase.Rising:
                    level = Mathf.Lerp(lowLevel + 0.12f, highLevel, Mathf.SmoothStep(0f, 1f, phaseTimer / riseTime));
                    agitation = 1f;
                    if (phaseTimer >= riseTime) Enter(Phase.Holding);
                    break;

                case Phase.Holding:
                    level = highLevel; agitation = 0.6f;
                    if (phaseTimer >= holdTime) Enter(Phase.Falling);
                    break;

                case Phase.Falling:
                    level = Mathf.Lerp(highLevel, lowLevel, Mathf.SmoothStep(0f, 1f, phaseTimer / fallTime));
                    agitation = 0.4f;
                    if (phaseTimer >= fallTime) Enter(Phase.Dry);
                    break;
            }
        }

        private void Enter(Phase next)
        {
            phase = next;
            phaseTimer = 0f;
            Vector2 c = transform.position;
            switch (next)
            {
                case Phase.Warning:
                    Sfx.EffectAt("Lombriz_Retumbo", c, 0.9f);
                    Shake(0.12f, warningTime);
                    if (!warnedOnce && !string.IsNullOrEmpty(firstWarning))
                    {
                        warnedOnce = true;
                        GameplayEvents.RaiseMessage(firstWarning);
                    }
                    break;
                case Phase.Rising:
                    Sfx.EffectAt("Acido_Burbujeo", c, 1f);
                    Shake(0.2f, 0.35f);
                    break;
            }
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }

        // ---------- Daño ----------

        private void UpdateDamage(float dt)
        {
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive || level < 0.05f || damagePerTick <= 0f) { damageTimer = 0f; return; }

            Vector2 feet = player.Position;
            Vector2 c = transform.position;
            float top = c.y + level;
            bool overLake = Mathf.Abs(feet.x - c.x) <= width * 0.5f;
            // Parado en el piso dentro del ácido, o hundido (aunque esté en el aire) cuando la marea está alta.
            bool inside = overLake && feet.y > c.y - 1f
                          && (player.Motor.IsGrounded ? feet.y <= top + 0.05f : feet.y < top - 0.1f);

            if (!inside) { damageTimer = 0f; return; }

            damageTimer -= dt;
            if (damageTimer > 0f) return;
            damageTimer = tickInterval;

            DamageInfo info = DamageInfo.Create(damagePerTick, DamageKind.Environment, Team.Neutral, new Vector2(feet.x, top));
            info.blockable = false;
            player.Health.ApplyDamage(info);
            Sfx.Effect("Acido_Quema", 0.8f);
        }

        // ---------- Dibujo ----------

        private void BuildVisual()
        {
            EnsureSprites();

            body = Make("Liquido", square, middle, sortingOrder);
            depthShade = Make("Profundo", square, deep, sortingOrder);

            int count = Mathf.Max(2, Mathf.CeilToInt(width / columnWidth));
            columns = new SpriteRenderer[count];
            crests = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                columns[i] = Make("Ola", square, surface, sortingOrder + 1);
                crests[i] = Make("Cresta", square, crest, sortingOrder + 2);
            }

            int bubbleCount = Mathf.Clamp(Mathf.RoundToInt(width * 1.2f), 4, 60);
            bubbles = new Bubble[bubbleCount];
            for (int i = 0; i < bubbleCount; i++)
            {
                bubbles[i].renderer = Make("Burbuja", circle, crest, sortingOrder + 3);
                bubbles[i].vapor = i % 4 == 0;
                Respawn(ref bubbles[i], true);
            }
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

        /// <summary>Altura de la ola en x (local), en unidades; escalonada para que se vea pixel art.</summary>
        private float Wave(float x, float t)
        {
            float a = 0.05f + 0.1f * agitation;
            float h = Mathf.Sin(x * 1.7f + t * 2.3f + seed) * 0.55f
                      + Mathf.Sin(x * 3.9f - t * 3.1f + seed * 2f) * 0.3f
                      + Mathf.Sin(x * 0.6f + t * 1.1f) * 0.15f;
            h = (h * 0.5f + 0.5f) * a;
            return Mathf.Round(h * 32f) / 32f;
        }

        private void UpdateVisual(float dt)
        {
            bool visible = level > 0.02f;
            Camera cam = Camera.main;
            if (cam != null)
            {
                float half = cam.orthographicSize * cam.aspect + 2f;
                if (Mathf.Abs(cam.transform.position.x - transform.position.x) > half + width * 0.5f) visible = false;
            }
            SetVisible(visible);
            if (!visible) return;

            float t = Time.time;
            float left = -width * 0.5f;
            float tremble = phase == Phase.Warning ? Mathf.Sin(t * 60f) * 0.02f : 0f;

            // Cuerpo: del fondo hasta la superficie. Abajo más oscuro (se ve hondo).
            float bodyTop = level;
            float bodyBottom = -0.05f;
            body.transform.localPosition = new Vector3(0f, (bodyTop + bodyBottom) * 0.5f, 0f);
            body.transform.localScale = new Vector3(width, bodyTop - bodyBottom, 1f);
            float shadeTop = bodyBottom + (bodyTop - bodyBottom) * 0.55f;
            depthShade.enabled = level > 0.35f;
            depthShade.transform.localPosition = new Vector3(0f, (shadeTop + bodyBottom) * 0.5f, 0f);
            depthShade.transform.localScale = new Vector3(width, shadeTop - bodyBottom, 1f);

            float colW = width / columns.Length;
            for (int i = 0; i < columns.Length; i++)
            {
                float x = left + colW * (i + 0.5f);
                float h = 0.04f + Wave(x, t) + tremble;
                // Orillas: la ola baja suavemente para no verse cortada.
                float edge = Mathf.Clamp01(Mathf.Min(i + 0.5f, columns.Length - i - 0.5f) / 2f);
                h *= edge;
                // Columna: de un poco abajo de la superficie hasta la cresta de la ola.
                float top = level + h;
                columns[i].transform.localPosition = new Vector3(x, level + (h - 0.1f) * 0.5f, 0f);
                columns[i].transform.localScale = new Vector3(colW + 0.002f, h + 0.1f, 1f);
                crests[i].transform.localPosition = new Vector3(x, top - 0.02f, 0f);
                crests[i].transform.localScale = new Vector3(colW + 0.002f, 0.04f, 1f);
            }


            for (int i = 0; i < bubbles.Length; i++) UpdateBubble(ref bubbles[i], dt, t);
        }

        private void UpdateBubble(ref Bubble b, float dt, float t)
        {
            float speedUp = 1f + agitation * 1.5f;
            SpriteRenderer r = b.renderer;

            if (b.vapor)
            {
                // Vapor: sube por encima del ácido y se desvanece.
                b.y += b.speed * 0.5f * dt;
                float life = Mathf.Clamp01((b.y - level) / 1.4f);
                r.color = new Color(crest.r, crest.g, crest.b, 0.12f * (1f - life));
                float s = b.size * (1.5f + life * 2f);
                r.transform.localScale = new Vector3(s, s, 1f);
                r.transform.localPosition = new Vector3(b.x + Mathf.Sin(t * 1.5f + b.size * 30f) * 0.15f, b.y, 0f);
                if (life >= 1f) Respawn(ref b, false);
                return;
            }

            if (b.popTimer > 0f)
            {
                // Revienta: se agranda y desaparece.
                b.popTimer -= dt;
                float k = 1f - b.popTimer / 0.12f;
                float s = b.size * (1f + k * 1.2f);
                r.transform.localScale = new Vector3(s, s, 1f);
                r.color = new Color(crest.r, crest.g, crest.b, 0.9f * (1f - k));
                if (b.popTimer <= 0f) Respawn(ref b, false);
                return;
            }

            b.y += b.speed * speedUp * dt;
            float surfaceHere = level + Wave(b.x, t);
            if (b.y >= surfaceHere)
            {
                b.y = surfaceHere;
                b.popTimer = 0.12f;
            }
            r.color = new Color(crest.r, crest.g, crest.b, 0.75f);
            r.transform.localScale = new Vector3(b.size, b.size, 1f);
            r.transform.localPosition = new Vector3(b.x + Mathf.Sin(t * 4f + b.size * 50f) * 0.03f, b.y, 0f);
        }

        private void Respawn(ref Bubble b, bool randomHeight)
        {
            float margin = Mathf.Min(0.3f, width * 0.2f);
            b.x = Random.Range(-width * 0.5f + margin, width * 0.5f - margin);
            b.size = Random.Range(0.06f, 0.14f);
            b.speed = Random.Range(0.4f, 1.1f);
            b.popTimer = 0f;
            // Nacen dentro del ácido (nunca debajo del piso, donde se verían encima de él).
            float bottom = -0.1f;
            float high = Mathf.Max(bottom, randomHeight ? level : level * 0.3f);
            b.y = b.vapor ? level : Random.Range(bottom, high);
        }

        private void SetVisible(bool on)
        {
            if (body.enabled == on) return;
            body.enabled = on; depthShade.enabled = on;
            for (int i = 0; i < columns.Length; i++) { columns[i].enabled = on; crests[i].enabled = on; }
            for (int i = 0; i < bubbles.Length; i++) bubbles[i].renderer.enabled = on;
        }

        private static void EnsureSprites()
        {
            if (square == null)
            {
                var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[16];
                for (int i = 0; i < 16; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            if (circle == null)
            {
                const int n = 32;
                var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        pixels[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 3f));
                    }
                tex.SetPixels(pixels);
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 1f, 0.15f, 0.8f);
            float h = tide ? highLevel : lowLevel;
            Gizmos.DrawWireCube(transform.position + new Vector3(0f, h * 0.5f, 0f), new Vector3(width, Mathf.Max(h, 0.05f), 0f));
        }
    }
}
