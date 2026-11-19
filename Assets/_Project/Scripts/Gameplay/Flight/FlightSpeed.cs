using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Letrero grande que aparece un momento arriba de la pantalla ("¡MÁS RÁPIDO!", "¡ADRENALINA!"...). Va pegado a
    /// la cámara; se crea solo la primera vez.
    /// </summary>
    public class SpeedBanner : MonoBehaviour
    {
        private static SpeedBanner instance;
        private TextMeshPro text;
        private float timer, life;
        private Color color;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static void Show(string message, Color tint, float seconds = 1.6f)
        {
            if (instance == null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                var go = new GameObject("LetreroVelocidad");
                go.transform.SetParent(cam.transform, false);
                go.transform.localPosition = new Vector3(0f, cam.orthographicSize * 0.62f, 5f);
                instance = go.AddComponent<SpeedBanner>();
                instance.text = WorldText.Create(go.transform, "", Vector2.zero, new Vector2(12f, 1.6f), Color.white, 950, null, 9f);
                instance.text.outlineWidth = 0.25f;
                instance.text.outlineColor = new Color32(20, 6, 12, 255);
            }
            instance.text.text = message;
            instance.color = tint;
            instance.timer = instance.life = seconds;
            instance.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (text == null) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) { gameObject.SetActive(false); return; }
            float age = life - timer;
            // Entra con un "pop" y se desvanece al final.
            float pop = age < 0.15f ? Mathf.Lerp(1.5f, 1f, age / 0.15f) : 1f;
            text.transform.localScale = Vector3.one * pop;
            Color c = color;
            c.a = Mathf.Clamp01(timer / 0.35f);
            text.color = c;
        }
    }

    /// <summary>
    /// ANILLO DE VELOCIDAD: pasas por adentro con la nave y cambia la velocidad un rato.
    /// - ADRENALINA (naranja): la hormona del susto acelera el corazón. La pantalla va MUCHO más rápido unos segundos,
    ///   la nave queda invencible y EMBISTE a los enemigos que toque (pero el corazón late más rápido y disparan más).
    /// - BETABLOQUEADOR (azul): el medicamento que baja el ritmo del corazón. Todo va más lento unos segundos (sirve
    ///   antes de un tramo difícil).
    /// Si mueres y regresas antes del anillo, vuelve a aparecer.
    /// </summary>
    public class FlightSpeedPad : MonoBehaviour
    {
        public enum Kind { Adrenaline, BetaBlocker }

        [SerializeField] private Kind kind;
        [Tooltip("0 = siempre; 1/2 = solo si vas por esa rama de la bifurcación.")]
        [SerializeField] private int branch;
        [SerializeField] private float radius = 1.35f;
        [SerializeField, TextArea] private string message;

        private bool used;
        private float usedAt, spin;
        private Transform art;
        private SpriteRenderer[] glows = new SpriteRenderer[0];
        private Hurtbox playerBox;

        public Kind Type => kind;

        private static bool toldAdrenaline, toldBeta;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { toldAdrenaline = false; toldBeta = false; }

        public void Configure(Kind padKind, string firstMessage, int routeBranch = 0)
        {
            kind = padKind;
            message = firstMessage;
            branch = routeBranch;
        }

        private Color Main => kind == Kind.Adrenaline ? new Color(1f, 0.62f, 0.15f) : new Color(0.35f, 0.7f, 1f);
        private Color Light => kind == Kind.Adrenaline ? new Color(1f, 0.92f, 0.45f) : new Color(0.75f, 0.95f, 1f);

        private void Start()
        {
            // Dibujo: un aro brillante con flechitas (adrenalina: hacia adelante; betabloqueador: hacia atrás).
            art = new GameObject("Dibujo").transform;
            art.SetParent(transform, false);
            float d = radius * 2f;
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            list.Add(CodeSprites.Add(art, CodeSprites.Glow, new Color(Main.r, Main.g, Main.b, 0.45f), 1, Vector2.zero, Vector2.one * d * 1.7f, "Brillo"));
            CodeSprites.Add(art, CodeSprites.Ring, new Color(0.12f, 0.05f, 0.1f), 2, Vector2.zero, new Vector2(d * 0.62f, d) * 1.08f, "Contorno");
            list.Add(CodeSprites.Add(art, CodeSprites.Ring, Main, 3, Vector2.zero, new Vector2(d * 0.62f, d), "Aro"));
            list.Add(CodeSprites.Add(art, CodeSprites.Ring, Light, 4, Vector2.zero, new Vector2(d * 0.5f, d * 0.86f), "AroClaro"));
            float dir = kind == Kind.Adrenaline ? 1f : -1f;
            for (int i = 0; i < 3; i++)
            {
                // Flechita hecha con dos palitos.
                var arrow = new GameObject("Flecha" + i).transform;
                arrow.SetParent(art, false);
                arrow.localPosition = new Vector3((i - 1) * 0.42f * dir, 0f, 0f);
                foreach (int s in new[] { 1, -1 })
                {
                    SpriteRenderer stick = CodeSprites.Add(arrow, CodeSprites.Square, Light, 5, new Vector2(-0.08f * dir, s * 0.13f), new Vector2(0.34f, 0.09f), "Palito");
                    stick.transform.localRotation = Quaternion.Euler(0f, 0f, -s * 45f * dir);
                }
            }
            glows = list.ToArray();
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null || art == null) return;
            float dt = Time.deltaTime;
            spin += dt;

            if (used)
            {
                // Se apaga; si mueres y la pantalla regresa antes de él, vuelve a estar.
                float k = Mathf.Clamp01((Time.time - usedAt) / 0.35f);
                art.localScale = Vector3.one * (1f + k * 0.6f);
                foreach (SpriteRenderer r in glows) if (r != null) { Color c = r.color; c.a = (1f - k) * 0.9f; r.color = c; }
                if (k >= 1f) art.gameObject.SetActive(false);
                if (route.View.xMax < transform.position.x - 1f) Rearm();
                return;
            }

            float pulse = 1f + Mathf.Sin(spin * 6f) * 0.06f + Heartbeat.SurgeNow * 0.08f;
            art.localScale = Vector3.one * pulse;
            if (!route.InBranch(branch)) return;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (playerBox == null) playerBox = player.GetComponent<Hurtbox>();
            Vector2 center = playerBox != null ? playerBox.Center : player.Position + new Vector2(0f, 0.6f);
            if (Vector2.Distance(center, transform.position) > radius) return;
            Trigger(route, player);
        }

        private void Rearm()
        {
            used = false;
            art.gameObject.SetActive(true);
            art.localScale = Vector3.one;
            foreach (SpriteRenderer r in glows) if (r != null) { Color c = r.color; c.a = r.name == "Brillo" ? 0.45f : 1f; r.color = c; }
        }

        private void Trigger(FlightRoute route, PlayerController player)
        {
            used = true;
            usedAt = Time.time;
            if (kind == Kind.Adrenaline)
            {
                const float seconds = 2.6f;
                route.ApplySpeedEffect(1.75f, seconds, 30f);
                if (player.Flight != null) player.Flight.StartBoost(seconds);
                Sfx.Effect("Nave_Despegue", 0.8f);
                SpeedBanner.Show("¡ADRENALINA!", new Color(1f, 0.75f, 0.25f));
                if (!toldAdrenaline)
                {
                    toldAdrenaline = true;
                    GameplayEvents.RaiseMessage(!string.IsNullOrEmpty(message) ? message :
                        "¡ADRENALINA! La hormona del susto acelera el corazón: vas rapidísimo, eres invencible y EMBISTES a los enemigos.");
                }
            }
            else
            {
                route.ApplySpeedEffect(0.5f, 4f, -26f);
                Sfx.Effect("Latido_Dub", 0.9f);
                SpeedBanner.Show("BETABLOQUEADOR: más lento", new Color(0.55f, 0.85f, 1f));
                if (!toldBeta)
                {
                    toldBeta = true;
                    GameplayEvents.RaiseMessage(!string.IsNullOrEmpty(message) ? message :
                        "BETABLOQUEADOR: un medicamento que baja el ritmo del corazón. Todo va más lento unos segundos.");
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind == Kind.Adrenaline ? new Color(1f, 0.6f, 0.1f) : new Color(0.3f, 0.7f, 1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }

    /// <summary>
    /// SANGRE ESPESA: un tramo con tanta grasa y proteínas que la sangre se pone espesa (hiperviscosidad). Mientras la
    /// pantalla va por aquí, todo avanza más lento y la nave se mueve con trabajo (sin dash).
    /// </summary>
    public class ThickBloodZone : MonoBehaviour
    {
        [SerializeField] private float fromX, toX;
        [SerializeField] private float halfHeight = 3.6f;
        [Tooltip("Cuánto se alenta la pantalla (1 = nada).")]
        [SerializeField] private float screenFactor = 0.6f;
        [Tooltip("Cuánto se alenta la nave (1 = nada).")]
        [SerializeField] private float shipFactor = 0.55f;
        [SerializeField] private int branch;
        [SerializeField, TextArea] private string message;

        private bool told, inside;
        private float t;
        private Transform blobs;
        private SpriteRenderer[] drops = new SpriteRenderer[0];
        private Vector2[] dropBase = new Vector2[0];

        public void Configure(float startX, float endX, float vesselHalfHeight, string firstMessage, int routeBranch = 0)
        {
            fromX = startX;
            toX = endX;
            halfHeight = vesselHalfHeight;
            message = firstMessage;
            branch = routeBranch;
        }

        private void Start()
        {
            // Una niebla amarillenta y gotitas de grasa flotando (de adorno: no estorban).
            blobs = new GameObject("Dibujo").transform;
            blobs.SetParent(transform, false);
            float w = toX - fromX;
            var rnd = new System.Random(Mathf.RoundToInt(fromX * 3f));
            for (float x = 0.8f; x < w; x += 2.4f)
                CodeSprites.Add(blobs, CodeSprites.Glow, new Color(0.95f, 0.75f, 0.3f, 0.22f), -60, new Vector2(x - w * 0.5f, ((float)rnd.NextDouble() - 0.5f) * halfHeight * 0.5f),
                    new Vector2(4.5f, halfHeight * 1.5f), "Niebla");
            int n = Mathf.RoundToInt(w * 1.4f);
            drops = new SpriteRenderer[n];
            dropBase = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                dropBase[i] = new Vector2((float)rnd.NextDouble() * w - w * 0.5f, ((float)rnd.NextDouble() * 2f - 1f) * halfHeight * 0.8f);
                float s = 0.15f + (float)rnd.NextDouble() * 0.25f;
                drops[i] = CodeSprites.Add(blobs, CodeSprites.Circle, new Color(1f, 0.82f, 0.35f, 0.75f), -59, dropBase[i], Vector2.one * s, "Grasa");
            }
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            t += Time.deltaTime;
            for (int i = 0; i < drops.Length; i++)
                if (drops[i] != null)
                    drops[i].transform.localPosition = dropBase[i] + new Vector2(Mathf.Sin(t * 0.7f + i) * 0.25f, Mathf.Cos(t * 0.5f + i * 1.3f) * 0.2f);
            if (!route.InBranch(branch)) return;

            float cx = route.CameraPosition.x;
            bool nowInside = cx > fromX && cx < toX;
            if (nowInside)
            {
                route.SlowZone(screenFactor);
                PlayerController player = PlayerController.Current;
                if (player != null && player.Flight != null && player.Position.x > fromX - 1f && player.Position.x < toX + 1f)
                    player.Flight.Slow(shipFactor);
            }
            if (nowInside && !inside)
            {
                SpeedBanner.Show("SANGRE ESPESA", new Color(1f, 0.82f, 0.4f));
                if (!told && !string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
                told = true;
            }
            inside = nowInside;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(new Vector3((fromX + toX) * 0.5f, transform.position.y, 0f), new Vector3(toX - fromX, halfHeight * 2f, 0f));
        }
    }
}
