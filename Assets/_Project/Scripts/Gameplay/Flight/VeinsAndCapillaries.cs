using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// CÉLULA SIN OXÍGENO en la pared de un capilar (tramo 4). Está AZUL y triste; si pasas cerca un momento le llevas
    /// oxígeno, se pone ROSITA y te cura un poquito. Si se lo llevas a casi todas, premio (vida y una bomba).
    /// Biología: en los capilares los glóbulos rojos sueltan el oxígeno a las células y se llevan el CO2.
    /// </summary>
    public class OxygenCell : MonoBehaviour
    {
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private float needed = 0.35f;
        [SerializeField] private float heal = 3f;

        private static readonly List<OxygenCell> all = new List<OxygenCell>();
        private static int done;
        private static bool rewarded;

        private float progress;
        private bool oxygenated;
        private SpriteRenderer body, outline;
        private Transform face, smile, frown, sparkle;
        private float t;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { all.Clear(); done = 0; rewarded = false; }

        private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        private void OnDisable() => all.Remove(this);

        private void Start()
        {
            var art = new GameObject("Dibujo").transform;
            art.SetParent(transform, false);
            outline = CodeSprites.Add(art, CodeSprites.Circle, new Color(0.1f, 0.15f, 0.35f), 1, Vector2.zero, new Vector2(1.05f, 0.95f), "Contorno");
            body = CodeSprites.Add(art, CodeSprites.Circle, new Color(0.45f, 0.6f, 0.95f), 2, Vector2.zero, new Vector2(0.92f, 0.82f), "Celula");
            CodeSprites.Add(art, CodeSprites.Circle, new Color(0.3f, 0.35f, 0.7f), 3, new Vector2(0.16f, 0.12f), new Vector2(0.3f, 0.26f), "Nucleo");
            face = new GameObject("Cara").transform;
            face.SetParent(art, false);
            CodeSprites.Add(face, CodeSprites.Circle, Color.black, 4, new Vector2(-0.18f, 0.08f), Vector2.one * 0.09f, "Ojo");
            CodeSprites.Add(face, CodeSprites.Circle, Color.black, 4, new Vector2(0.02f, 0.08f), Vector2.one * 0.09f, "Ojo");
            frown = CodeSprites.Add(face, CodeSprites.Square, Color.black, 4, new Vector2(-0.08f, -0.14f), new Vector2(0.2f, 0.05f), "Boca").transform;
            frown.localRotation = Quaternion.Euler(0f, 0f, 12f);
            smile = new GameObject("Sonrisa").transform;
            smile.SetParent(face, false);
            CodeSprites.Add(smile, CodeSprites.Square, Color.black, 4, new Vector2(-0.15f, -0.12f), new Vector2(0.12f, 0.05f), "Boca").transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            CodeSprites.Add(smile, CodeSprites.Square, Color.black, 4, new Vector2(0f, -0.12f), new Vector2(0.12f, 0.05f), "Boca").transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            smile.gameObject.SetActive(false);
            sparkle = CodeSprites.Add(art, CodeSprites.Glow, new Color(1f, 0.75f, 0.85f, 0f), 0, Vector2.zero, Vector2.one * 2.2f, "Brillo").transform;
        }

        private void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            t += dt;
            // Respira despacito (más contenta cuando ya tiene oxígeno).
            float breathe = 1f + Mathf.Sin(t * (oxygenated ? 3f : 1.4f)) * 0.04f;
            body.transform.parent.localScale = Vector3.one * breathe;
            if (oxygenated)
            {
                SpriteRenderer glow = sparkle.GetComponent<SpriteRenderer>();
                Color g = glow.color;
                g.a = Mathf.MoveTowards(g.a, 0f, dt * 0.5f);
                glow.color = g;
                return;
            }

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 ship = player.Position + new Vector2(0f, 0.6f) * player.Size;
            bool near = Vector2.Distance(ship, transform.position) < radius;
            progress = Mathf.Clamp(progress + (near ? dt : -dt * 0.5f), 0f, needed);
            // Se va pintando de rosa mientras le llevas oxígeno.
            float k = progress / needed;
            body.color = Color.Lerp(new Color(0.45f, 0.6f, 0.95f), new Color(1f, 0.6f, 0.7f), k);
            if (progress >= needed) Oxygenate(player);
        }

        private void Oxygenate(PlayerController player)
        {
            oxygenated = true;
            body.color = new Color(1f, 0.62f, 0.72f);
            outline.color = new Color(0.5f, 0.15f, 0.25f);
            frown.gameObject.SetActive(false);
            smile.gameObject.SetActive(true);
            SpriteRenderer glow = sparkle.GetComponent<SpriteRenderer>();
            glow.color = new Color(1f, 0.75f, 0.85f, 0.7f);
            player.Health.Heal(heal);
            done++;
            Sfx.Effect("Recoger_Vida", 0.5f);
            int total = Mathf.Max(done, all.Count);
            SpeedBanner.Show("OXÍGENO " + done + "/" + total, new Color(1f, 0.7f, 0.8f), 0.9f);
            if (done == 1)
                GameplayEvents.RaiseMessage("¡Eso! Le llevaste OXÍGENO y se puso rosita. Así trabajan los glóbulos rojos en los capilares.");
            if (!rewarded && done >= Mathf.CeilToInt(total * 0.8f) && total >= 5)
            {
                rewarded = true;
                player.Health.Heal(20f);
                if (player.Bombs != null) player.Bombs.Add(1);
                GameplayEvents.RaiseMessage("¡Casi todas las células tienen oxígeno! Premio: +vida y +1 bomba.");
            }
        }
    }

    /// <summary>
    /// BOMBA MUSCULAR de la pantorrilla (tramo 5): mientras la pantalla va por aquí, el músculo aprieta la vena cada
    /// 'period' segundos. Al apretar, las VÁLVULAS se abren y la sangre (y tú) sale disparada hacia arriba; al soltar,
    /// las válvulas se cierran para que la sangre no se regrese.
    /// Biología: por eso caminar ayuda a la circulación de las piernas.
    /// </summary>
    public class MusclePump : MonoBehaviour
    {
        [SerializeField] private float fromX, toX;
        [SerializeField] private float period = 4.2f;
        [Tooltip("Parte del ciclo en la que las válvulas están abiertas (0..1).")]
        [SerializeField] private float openPart = 0.58f;

        public static MusclePump Current { get; private set; }
        private float time;
        private int squeezes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        public void Configure(float startX, float endX, float seconds)
        {
            fromX = startX;
            toX = endX;
            period = seconds;
        }

        /// <summary>Qué tan abierta está una válvula con ese retraso (0 cerrada, 1 abierta). Sin bomba: abierta.</summary>
        public static float Open01(float delay, float x)
        {
            MusclePump p = Current;
            if (p == null || x < p.fromX || x > p.toX) return 1f;
            float phase = Mathf.Repeat((p.time - delay) / p.period, 1f);
            // Abre rápido, se queda abierta, cierra rápido.
            const float edge = 0.08f;
            if (phase < edge) return phase / edge;
            if (phase < p.openPart) return 1f;
            if (phase < p.openPart + edge) return 1f - (phase - p.openPart) / edge;
            return 0f;
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            float cx = route.CameraPosition.x;
            bool inside = cx > fromX - 6f && cx < toX;
            if (!inside) { if (Current == this) Current = null; return; }
            if (Current != this) { Current = this; time = 0f; }
            float before = Mathf.Repeat(time / period, 1f);
            time += Time.deltaTime;
            float after = Mathf.Repeat(time / period, 1f);
            if (after < before) Squeeze(route);
        }

        /// <summary>El músculo aprieta: empujón hacia arriba y la pantalla avanza un rato más rápido.</summary>
        private void Squeeze(FlightRoute route)
        {
            squeezes++;
            route.ApplySpeedEffect(1.6f, period * openPart * 0.8f, 0f);
            PlayerController player = PlayerController.Current;
            if (player != null && player.Flight != null && player.Health.IsAlive) player.Flight.Kick(new Vector2(4.5f, 0f));
            Sfx.Effect("Latido_Lub", 0.9f);
            if (squeezes <= 3) SpeedBanner.Show("¡BOMBA MUSCULAR!", new Color(1f, 0.7f, 0.6f), 1f);
        }
    }

    /// <summary>
    /// VÁLVULA DE LA VENA (tramo 5): dos aletas que salen de las paredes. Abiertas (pegadas a la pared) dejan pasar;
    /// cerradas se juntan en medio y TAPAN la vena: la pantalla se detiene hasta que vuelvan a abrir. Si se cierran
    /// contigo adentro, te lastiman y te empujan para abajo. Se abren con la BOMBA MUSCULAR.
    /// Rota (várices): las aletas cuelgan flojas, no cierran y la sangre se regresa.
    /// </summary>
    public class VenousValve : MonoBehaviour, IFlightBlocker
    {
        [SerializeField] private float radius = 4.6f;
        [SerializeField] private float delay;
        [SerializeField] private bool broken;
        [SerializeField] private float contactDamage = 8f;

        private Transform upper, lower;
        private PolygonCollider2D wall;
        private float open = 1f, hurtCooldown;
        private Hurtbox playerBox;
        private bool told;

        private static bool toldClosed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => toldClosed = false;

        public float BlockX => transform.position.x - 0.5f;
        /// <summary>Ya la pasaste si vas más arriba que la punta de las aletas.</summary>
        public float PassX => transform.position.x + Tip + 0.4f;
        public bool Blocking => !broken && open < 0.5f;
        public int Branch => 0;
        /// <summary>Qué tan adelante quedan las puntas cuando se cierra (forma de "V").</summary>
        private float Tip => radius * 0.44f;

        public void Configure(float vesselRadius, float pumpDelay, bool isBroken)
        {
            radius = vesselRadius;
            delay = pumpDelay;
            broken = isBroken;
        }

        private void OnEnable() => FlightBlockers.Add(this);
        private void OnDisable() => FlightBlockers.Remove(this);

        private void Start()
        {
            upper = BuildFlap("AletaArriba", 1f);
            lower = BuildFlap("AletaAbajo", -1f);
            gameObject.layer = PhysicsLayers.Ground;
            // Sólido con forma de las aletas cerradas (una "V" acostada que apunta hacia adelante, gruesita).
            wall = gameObject.AddComponent<PolygonCollider2D>();
            wall.points = new[]
            {
                new Vector2(0f, radius + 0.5f), new Vector2(Tip, 0f), new Vector2(0f, -radius - 0.5f),
                new Vector2(0.6f, -radius - 0.5f), new Vector2(Tip + 0.6f, 0f), new Vector2(0.6f, radius + 0.5f)
            };
            wall.enabled = false;
            Pose(broken ? 0.55f : 1f);
        }

        /// <summary>Aleta: pivote en la pared, un "pétalo" blanco-rosado con su orilla.</summary>
        private Transform BuildFlap(string name, float side)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, side * radius, 0f);
            float len = radius * 1.08f;
            var leaf = new GameObject("Hoja").transform;
            leaf.SetParent(pivot, false);
            leaf.localPosition = new Vector3(len * 0.5f, 0f, 0f);
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(0.5f, 0.12f, 0.2f), 6, Vector2.zero, new Vector2(len + 0.16f, 0.62f), "Orilla");
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(1f, 0.78f, 0.8f), 7, Vector2.zero, new Vector2(len, 0.46f), "Hoja");
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(1f, 0.95f, 0.95f, 0.7f), 8, new Vector2(-len * 0.1f, 0.06f), new Vector2(len * 0.6f, 0.12f), "Brillo");
            return pivot;
        }

        /// <summary>0 = cerrada (las puntas se juntan en medio, apuntando hacia arriba); 1 = abierta (pegadas a la pared).</summary>
        private void Pose(float k)
        {
            // Cerrada: de la pared hacia el centro inclinada hacia adelante (como una V). Abierta: casi pegada a la pared.
            float closedAngle = 66f, openAngle = 8f;
            float a = Mathf.Lerp(closedAngle, openAngle, k);
            if (upper != null) upper.localRotation = Quaternion.Euler(0f, 0f, -a);
            if (lower != null) lower.localRotation = Quaternion.Euler(0f, 0f, a);
        }

        private void Update()
        {
            if (upper == null) return;
            float dt = Time.deltaTime;
            if (hurtCooldown > 0f) hurtCooldown -= dt;
            FlightRoute route = FlightRoute.Current;
            if (broken)
            {
                // Floja: se mece con la sangre que se regresa.
                Pose(0.55f + Mathf.Sin(Time.time * 2.3f) * 0.12f);
                return;
            }
            float before = open;
            open = MusclePump.Open01(delay, transform.position.x);
            Pose(open);
            if (wall != null) wall.enabled = open < 0.2f;

            if (route != null && !told && route.View.xMax > transform.position.x)
            {
                told = true;
                if (!toldClosed && open < 0.5f)
                {
                    toldClosed = true;
                    GameplayEvents.RaiseMessage("Válvula CERRADA: espera a que el músculo apriete (¡bomba!) y se abra.");
                }
            }

            // Se cierra contigo adentro: te lastima y te avienta hacia abajo (atrás).
            if (open < before && open < 0.6f && hurtCooldown <= 0f)
            {
                PlayerController player = PlayerController.Current;
                if (player == null || !player.Health.IsAlive) return;
                if (playerBox == null) playerBox = player.GetComponent<Hurtbox>();
                if (playerBox == null) return;
                // ¿Está entre la pared y donde quedan las aletas al cerrarse? (ahí te atrapan)
                Vector2 c = playerBox.Center;
                float dy = Mathf.Abs(c.y - transform.position.y);
                if (dy > radius) return;
                float flapX = transform.position.x + Tip * (1f - dy / radius);
                if (c.x < transform.position.x - 0.3f || c.x > flapX + 0.8f) return;
                DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, transform.position);
                playerBox.Damageable.ApplyDamage(info);
                hurtCooldown = 0.8f;
                if (player.Flight != null) player.Flight.Kick(new Vector2(-7f, 0f));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = broken ? new Color(0.6f, 0.6f, 0.6f) : new Color(1f, 0.8f, 0.8f);
            Gizmos.DrawLine(transform.position + Vector3.up * radius, transform.position - Vector3.up * radius);
        }
    }

    /// <summary>
    /// GRAVEDAD en las venas de la pierna (tramo 5): mientras vas por aquí, algo te jala hacia ABAJO (hacia atrás del
    /// camino). La sangre sube contra la gravedad; por eso las venas tienen válvulas.
    /// </summary>
    public class GravityPull : MonoBehaviour
    {
        [SerializeField] private float fromX, toX;
        [SerializeField] private float pull = 1.3f;

        public void Configure(float startX, float endX, float strength)
        {
            fromX = startX;
            toX = endX;
            pull = strength;
        }

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Flight == null || !player.Health.IsAlive) return;
            float x = player.Position.x;
            if (x < fromX || x > toX) return;
            player.Flight.AddPush(new Vector2(-pull, 0f));
        }
    }

    /// <summary>
    /// REMOLINO (várices): la sangre estancada da vueltas y te arrastra en círculo (y un poco hacia el centro).
    /// </summary>
    public class FlowSwirl : MonoBehaviour
    {
        [SerializeField] private float radius = 3f;
        [SerializeField] private float strength = 5f;
        [SerializeField] private bool clockwise = true;

        private Transform[] arms;
        private float spin;

        public void Configure(float swirlRadius, float force, bool turnClockwise)
        {
            radius = swirlRadius;
            strength = force;
            clockwise = turnClockwise;
        }

        private void Start()
        {
            arms = new Transform[10];
            for (int i = 0; i < arms.Length; i++)
            {
                SpriteRenderer r = CodeSprites.Add(transform, CodeSprites.Square, new Color(0.7f, 0.15f, 0.25f, 0.45f), -6, Vector2.zero,
                    new Vector2(radius * (0.35f + 0.06f * i), 0.07f), "Remolino");
                arms[i] = r.transform;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float dir = clockwise ? -1f : 1f;
            spin += dt * 140f * dir;
            if (arms != null)
                for (int i = 0; i < arms.Length; i++)
                {
                    float a = (spin + i * 36f) * Mathf.Deg2Rad;
                    float r = radius * (0.25f + 0.07f * i);
                    arms[i].localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                    arms[i].localRotation = Quaternion.Euler(0f, 0f, (spin + i * 36f) + 90f);
                }

            PlayerController player = PlayerController.Current;
            if (player == null || player.Flight == null || !player.Health.IsAlive) return;
            Vector2 d = (player.Position + new Vector2(0f, 0.6f)) - (Vector2)transform.position;
            float dist = d.magnitude;
            if (dist > radius || dist < 0.01f) return;
            Vector2 tangent = new Vector2(-d.y, d.x) / dist * dir;
            float k = 1f - dist / radius;
            player.Flight.AddPush((tangent * strength - d / dist * strength * 0.3f) * (0.4f + k));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
