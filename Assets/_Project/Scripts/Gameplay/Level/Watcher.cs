using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// VIGILANTE (Ojo): un macrófago del ojo que patrulla de un lado a otro con un CONO DE VISIÓN.
    /// Si te ve (y no estás escondido ni hay una pared en medio) se detiene, pone "!" y sube la inflamación.
    /// No es un enemigo: es una defensa del cuerpo, no se le dispara (y dispararle hace mucho ruido).
    /// </summary>
    public class Watcher : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float speed = 1.4f;
        [SerializeField] private float viewDistance = 7f;
        [SerializeField] private float viewAngle = 28f;
        [SerializeField] private float spotRate = 0.9f;
        [SerializeField] private Sprite art;

        private int facing = 1;
        private float pause;
        private Transform body, cone, alert;
        private SpriteRenderer coneRenderer;
        private static Sprite coneSprite;

        public void Configure(float from, float to, float walk, Sprite drawing) { minX = from; maxX = to; speed = walk; art = drawing; }

        private static Sprite Cone()
        {
            if (coneSprite != null) return coneSprite;
            const int w = 128, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1), v = Mathf.Abs(y / (float)(h - 1) - 0.5f) * 2f;
                    float a = v <= u ? (1f - u * 0.6f) : 0f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            coneSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w);   // 1 unidad de largo
            return coneSprite;
        }

        private void Start()
        {
            body = new GameObject("Macrofago").transform;
            body.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            if (art != null) CodeSprites.Art(body, art, 1.9f, 10, Vector2.zero);   // pivote abajo
            else
            {
                CodeSprites.Add(body, c, new Color(0.2f, 0.25f, 0.4f), 9, new Vector2(0f, 0.9f), new Vector2(1.9f, 1.75f));
                CodeSprites.Add(body, c, new Color(0.75f, 0.85f, 1f), 10, new Vector2(0f, 0.9f), new Vector2(1.75f, 1.6f));
                CodeSprites.Add(body, c, new Color(0.55f, 0.6f, 0.95f), 11, new Vector2(-0.3f, 0.7f), new Vector2(0.7f, 0.55f));     // núcleo
                CodeSprites.Add(body, c, Color.white, 12, new Vector2(0.35f, 1.15f), new Vector2(0.62f, 0.62f));                       // ojo
                CodeSprites.Add(body, c, new Color(0.15f, 0.2f, 0.4f), 13, new Vector2(0.45f, 1.15f), new Vector2(0.28f, 0.32f));
                for (int i = 0; i < 4; i++)
                    CodeSprites.Add(body, c, new Color(0.75f, 0.85f, 1f), 9, new Vector2(-0.9f + i * 0.6f, 0.1f), new Vector2(0.5f, 0.4f)); // patitas
            }
            cone = new GameObject("Vision").transform;
            cone.SetParent(transform, false);
            cone.localPosition = new Vector3(0f, 1.15f, 0f);
            coneRenderer = CodeSprites.Add(cone, Cone(), new Color(1f, 0.95f, 0.5f, 0.22f), 8, Vector2.zero, Vector2.one);
            float halfH = Mathf.Tan(viewAngle * Mathf.Deg2Rad) * viewDistance * 2f;
            coneRenderer.transform.localScale = new Vector3(viewDistance, halfH, 1f);
            alert = new GameObject("Alerta").transform;
            alert.SetParent(transform, false);
            alert.localPosition = new Vector3(0f, 2.4f, 0f);
            CodeSprites.Add(alert, CodeSprites.Square, new Color(1f, 0.25f, 0.25f), 30, new Vector2(0f, 0.15f), new Vector2(0.18f, 0.5f));
            CodeSprites.Add(alert, c, new Color(1f, 0.25f, 0.25f), 30, new Vector2(0f, -0.25f), new Vector2(0.2f, 0.2f));
            alert.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            StealthMeter meter = StealthMeter.Current;
            bool sees = p != null && p.Health.IsAlive && meter != null && CanSee(p) && !meter.PlayerHidden;
            alert.gameObject.SetActive(sees);
            coneRenderer.color = sees ? new Color(1f, 0.3f, 0.3f, 0.35f) : new Color(1f, 0.95f, 0.5f, 0.22f);

            if (sees)
            {
                meter.Spot(spotRate);
                if (Mathf.Abs(p.Position.x - transform.position.x) < 1.3f) meter.Noise(dt * 2f);   // te topaste con él
            }
            else
            {
                if (pause > 0f) pause -= dt;
                else
                {
                    Vector3 pos = transform.position;
                    pos.x += facing * speed * dt;
                    if (pos.x >= maxX) { pos.x = maxX; facing = -1; pause = 1.2f; }
                    if (pos.x <= minX) { pos.x = minX; facing = 1; pause = 1.2f; }
                    transform.position = pos;
                }
            }
            body.localScale = new Vector3(facing, 1f, 1f);
            cone.localScale = new Vector3(facing, 1f, 1f);
            body.localPosition = new Vector3(0f, sees ? 0f : Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.06f, 0f);
        }

        private bool CanSee(PlayerController p)
        {
            Vector2 eye = (Vector2)transform.position + Vector2.up * 1.15f;
            Vector2 target = p.Position + Vector2.up * (p.IsCrouching ? 0.4f : 0.9f);
            Vector2 d = target - eye;
            if (d.magnitude > viewDistance || Mathf.Sign(d.x) != facing) return false;
            float angle = Mathf.Abs(Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg);
            if (angle > viewAngle) return false;
            return Physics2D.Linecast(eye, target, 1 << PhysicsLayers.Ground).collider == null;
        }
    }

    /// <summary>
    /// RETINA (Ojo): conos y bastones, las células que captan la luz. Si pisas uno apagado se ENCIENDE (te delata un
    /// poco: sube la inflamación). Pasa por encima saltando o por las plataformas.
    /// </summary>
    public class NoisyFloor : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float noise = 0.12f;

        private Transform[] cells;
        private float[] lit;

        public void Configure(float from, float to) { minX = from; maxX = to; }

        private void Start()
        {
            int n = Mathf.Max(1, Mathf.RoundToInt((maxX - minX) / 0.55f));
            cells = new Transform[n];
            lit = new float[n];
            for (int i = 0; i < n; i++)
            {
                bool cone = i % 3 == 0;   // conos (color) y bastones (blanco y negro)
                Transform t = new GameObject(cone ? "Cono" : "Baston").transform;
                t.SetParent(transform, false);
                t.position = new Vector3(minX + (i + 0.5f) * (maxX - minX) / n, transform.position.y, 0f);
                CodeSprites.Add(t, CodeSprites.Square, new Color(0.25f, 0.15f, 0.3f), -30, new Vector2(0f, 0.3f), new Vector2(0.24f, cone ? 0.6f : 0.75f));
                CodeSprites.Add(t, CodeSprites.Circle, cone ? new Color(0.9f, 0.5f, 0.3f) : new Color(0.75f, 0.75f, 0.85f), -29, new Vector2(0f, cone ? 0.62f : 0.76f), new Vector2(0.3f, 0.3f));
                cells[i] = t;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            int step = -1;
            if (p != null && p.Motor.IsGrounded && p.Position.x >= minX && p.Position.x <= maxX && p.Position.y < transform.position.y + 0.4f)
                step = Mathf.Clamp(Mathf.FloorToInt((p.Position.x - minX) / (maxX - minX) * cells.Length), 0, cells.Length - 1);
            for (int i = 0; i < cells.Length; i++)
            {
                bool near = step >= 0 && Mathf.Abs(i - step) <= 1;
                if (near && lit[i] <= 0f)
                {
                    lit[i] = 3f;
                    if (i == step) { StealthMeter.Current?.Noise(noise); Sfx.EffectAt("Neurona_Golpe", cells[i].position, 0.25f); }
                }
                if (lit[i] > 0f) lit[i] -= dt;
                float k = Mathf.Clamp01(lit[i]);
                cells[i].localScale = Vector3.one * (1f + k * 0.25f);
                foreach (SpriteRenderer r in cells[i].GetComponentsInChildren<SpriteRenderer>())
                    if (r.sortingOrder == -29) r.color = Color.Lerp(i % 3 == 0 ? new Color(0.9f, 0.5f, 0.3f) : new Color(0.75f, 0.75f, 0.85f), new Color(1f, 1f, 0.6f), k);
            }
        }
    }

    /// <summary>HAZ DE LUZ (Ojo): la luz que entra por la pupila barre de un lado a otro. Si te alumbra y no estás escondido, te ven.</summary>
    public class LightBeam : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float period = 6f;
        [SerializeField] private float width = 1.6f;
        [SerializeField] private float height = 10f;

        private Transform beam;

        public void Configure(float from, float to, float cycle) { minX = from; maxX = to; period = cycle; }

        private void Start()
        {
            beam = new GameObject("Haz").transform;
            beam.SetParent(transform, false);
            CodeSprites.Add(beam, CodeSprites.Square, new Color(1f, 0.97f, 0.75f, 0.28f), 45, new Vector2(0f, height * 0.5f), new Vector2(width, height));
            CodeSprites.Add(beam, CodeSprites.Square, new Color(1f, 1f, 0.9f, 0.35f), 46, new Vector2(0f, height * 0.5f), new Vector2(width * 0.4f, height));
            CodeSprites.Add(beam, CodeSprites.Circle, new Color(1f, 1f, 0.8f, 0.45f), 46, new Vector2(0f, 0.1f), new Vector2(width * 1.6f, 0.5f));
        }

        private void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time / period * Mathf.PI * 2f);
            float x = Mathf.Lerp(minX, maxX, k);
            beam.position = new Vector3(x, transform.position.y, 0f);
            PlayerController p = PlayerController.Current;
            StealthMeter m = StealthMeter.Current;
            if (p == null || m == null || Mathf.Abs(p.Position.x - x) > width * 0.5f) return;
            m.Spot(1.6f);
        }
    }
}
