using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
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
}
