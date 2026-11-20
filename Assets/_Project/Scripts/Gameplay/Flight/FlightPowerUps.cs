using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>Qué da un power-up de la carrera.</summary>
    public enum FlightPowerKind
    {
        /// <summary>TRIPLE DISPARO: tres cápsulas en abanico por un rato.</summary>
        TripleShot,
        /// <summary>+MISILES (aunque pases del máximo normal).</summary>
        Missiles,
        /// <summary>GLÓBULO BLANCO: lo atrapas y se vuelve un LINFOCITO que te escolta y dispara toda la carrera.</summary>
        WhiteCell
    }

    /// <summary>
    /// POWER-UP que flota en la sangre durante un nivel de vuelo. Cuando la orilla derecha de la pantalla llega a su X,
    /// entra por la derecha (a la altura 'screenY' respecto al centro de la pantalla) y se va quedando atrás despacio;
    /// la nave lo agarra al tocarlo. Al reaparecer se vuelven a poner los que quedaron adelante.
    /// Lo coloca SceneSetup (FlightPowerUpAt). Se dibuja con figuras.
    /// </summary>
    public class FlightPowerUp : MonoBehaviour
    {
        [SerializeField] private FlightPowerKind kind;
        [SerializeField] private float screenY;
        [Tooltip("0 = sale siempre; 1/2 = solo en esa rama de la bifurcación.")]
        [SerializeField] private int branch;

        private const float Drift = 1.4f, PickRadius = 1.25f;
        private static readonly List<FlightPowerUp> all = new List<FlightPowerUp>();
        private enum State { Armed, Flying, Done }
        private State state;
        private Vector2 screen;
        private float age;
        private Transform visual, glow;

        public float TriggerX => transform.position.x;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        public void Configure(FlightPowerKind powerKind, float entryY, int routeBranch = 0)
        {
            kind = powerKind;
            screenY = entryY;
            branch = routeBranch;
        }

        private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        private void OnDisable() => all.Remove(this);

        /// <summary>Al reaparecer: los de adelante se vuelven a poner; los que estaban en pantalla se quitan.</summary>
        public static void RearmFrom(float viewMinX)
        {
            foreach (FlightPowerUp p in all)
            {
                if (p == null) continue;
                if (p.state == State.Flying) p.Hide();
                if (p.TriggerX > viewMinX) p.state = State.Armed;
            }
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null || state == State.Done) return;
            if (state == State.Armed)
            {
                if (!route.Launched || route.View.xMax < TriggerX || !route.InBranch(branch)) return;
                state = State.Flying;
                age = 0f;
                screen = new Vector2(route.HalfWidth + 1f, Mathf.Clamp(screenY, -route.HalfHeight + 1.2f, route.HalfHeight - 1.2f));
                if (visual == null) Build();
                visual.gameObject.SetActive(true);
            }

            float dt = Time.deltaTime;
            age += dt;
            screen.x -= Drift * dt;
            float bob = Mathf.Sin(age * 2.4f) * 0.35f;
            Vector2 world = route.CameraPosition + new Vector2(screen.x, screen.y + bob);
            visual.position = world;
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 1.7f) * 10f);
            if (glow != null) glow.localScale = Vector3.one * (2.1f + Mathf.Sin(age * 6f) * 0.25f);

            PlayerController player = PlayerController.Current;
            if (player != null && player.IsFlying && player.Health.IsAlive)
            {
                Vector2 ship = player.Position + new Vector2(0f, 0.6f * player.Size);
                if ((ship - world).sqrMagnitude < PickRadius * PickRadius)
                {
                    Collect(player, world);
                    return;
                }
            }
            if (screen.x < -route.HalfWidth - 1.5f) Hide();
        }

        private void Hide()
        {
            state = State.Done;
            if (visual != null) visual.gameObject.SetActive(false);
        }

        private void Collect(PlayerController player, Vector2 at)
        {
            Hide();
            ShipPowerUps powers = ShipPowerUps.For(player);
            Sfx.EffectAt("Recoger_Capsula", at);
            switch (kind)
            {
                case FlightPowerKind.TripleShot:
                    powers.GiveTripleShot(ShipPowerUps.TripleSeconds);
                    GameplayEvents.RaiseMessage($"NODO: ¡TRIPLE DISPARO! Tres cápsulas en abanico por {ShipPowerUps.TripleSeconds:0} segundos.");
                    break;
                case FlightPowerKind.Missiles:
                    powers.GiveMissiles(3);
                    GameplayEvents.RaiseMessage("NODO: ¡+3 MISILES! {bomba} lanza uno: busca solo al enemigo más cercano.");
                    break;
                default:
                    if (powers.AddAlly())
                        GameplayEvents.RaiseMessage(powers.Allies == 1
                            ? "NODO: ¡Atrapaste un GLÓBULO BLANCO! Se volvió un LINFOCITO: te escolta y dispara contigo toda la carrera. " +
                              "(Los linfocitos son glóbulos blancos que reconocen a cada germen.)"
                            : $"NODO: ¡Otro LINFOCITO! Ya te escoltan {powers.Allies}.");
                    else
                    {
                        powers.GiveMissiles(2);
                        GameplayEvents.RaiseMessage("NODO: Ya tienes la escolta completa: el glóbulo blanco te da +2 MISILES.");
                    }
                    break;
            }
        }

        // ───────────────────────────── Dibujo ─────────────────────────────

        private void Build()
        {
            visual = new GameObject("PowerUp_" + kind).transform;
            Color outline = new Color(0.12f, 0.05f, 0.12f);
            Color aura;
            switch (kind)
            {
                case FlightPowerKind.TripleShot:
                    aura = new Color(1f, 0.85f, 0.3f, 0.7f);
                    glow = CodeSprites.Add(visual, CodeSprites.Glow, aura, 30, Vector2.zero, Vector2.one * 2.1f, "Aura").transform;
                    CodeSprites.Add(visual, CodeSprites.Circle, outline, 31, Vector2.zero, Vector2.one * 1.05f, "Contorno");
                    CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 0.92f, 0.55f), 32, Vector2.zero, Vector2.one * 0.92f, "Burbuja");
                    // Tres cápsulas en abanico.
                    foreach (float a in new[] { 28f, 0f, -28f })
                    {
                        Transform cap = new GameObject("Capsula").transform;
                        cap.SetParent(visual, false);
                        cap.localRotation = Quaternion.Euler(0f, 0f, a);
                        CodeSprites.Add(cap, CodeSprites.Circle, outline, 33, new Vector2(0.18f, 0f), new Vector2(0.5f, 0.2f), "Contorno");
                        CodeSprites.Add(cap, CodeSprites.Circle, new Color(1f, 0.45f, 0.3f), 34, new Vector2(0.18f, 0f), new Vector2(0.42f, 0.13f), "Cuerpo");
                    }
                    break;
                case FlightPowerKind.Missiles:
                    aura = new Color(1f, 0.5f, 0.3f, 0.7f);
                    glow = CodeSprites.Add(visual, CodeSprites.Glow, aura, 30, Vector2.zero, Vector2.one * 2.1f, "Aura").transform;
                    CodeSprites.Add(visual, CodeSprites.Circle, outline, 31, Vector2.zero, Vector2.one * 1.05f, "Contorno");
                    CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 0.75f, 0.65f), 32, Vector2.zero, Vector2.one * 0.92f, "Burbuja");
                    CodeSprites.Add(visual, CodeSprites.Square, outline, 33, new Vector2(-0.05f, 0f), new Vector2(0.5f, 0.26f), "Contorno");
                    CodeSprites.Add(visual, CodeSprites.Square, new Color(0.95f, 0.3f, 0.25f), 34, new Vector2(-0.07f, 0f), new Vector2(0.42f, 0.17f), "Misil");
                    CodeSprites.Add(visual, CodeSprites.Circle, Color.white, 34, new Vector2(0.18f, 0f), new Vector2(0.2f, 0.17f), "Punta");
                    CodeSprites.Add(visual, CodeSprites.Glow, new Color(1f, 0.85f, 0.4f), 33, new Vector2(-0.36f, 0f), new Vector2(0.3f, 0.2f), "Fuego");
                    CodeSprites.Add(visual, CodeSprites.Square, outline, 35, new Vector2(0.2f, 0.25f), new Vector2(0.24f, 0.07f), "MasH");
                    CodeSprites.Add(visual, CodeSprites.Square, outline, 35, new Vector2(0.2f, 0.25f), new Vector2(0.07f, 0.24f), "MasV");
                    break;
                default:
                    // Glóbulo blanco: redondo, pálido, con su núcleo grande (como un linfocito) y una orilla con pliegues.
                    aura = new Color(0.85f, 0.95f, 1f, 0.65f);
                    glow = CodeSprites.Add(visual, CodeSprites.Glow, aura, 30, Vector2.zero, Vector2.one * 2.1f, "Aura").transform;
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI * 2f / 7f;
                        CodeSprites.Add(visual, CodeSprites.Circle, outline, 31, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.4f, Vector2.one * 0.42f, "Pliegue");
                        CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.96f, 0.96f, 1f), 32, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.38f, Vector2.one * 0.34f, "PliegueClaro");
                    }
                    CodeSprites.Add(visual, CodeSprites.Circle, outline, 31, Vector2.zero, Vector2.one * 1f, "Contorno");
                    CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.96f, 0.96f, 1f), 32, Vector2.zero, Vector2.one * 0.88f, "Celula");
                    CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.45f, 0.3f, 0.75f), 33, new Vector2(-0.04f, -0.03f), Vector2.one * 0.56f, "Nucleo");
                    CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.6f), 34, new Vector2(-0.18f, 0.2f), new Vector2(0.2f, 0.1f), "Brillo");
                    break;
            }
        }
    }

    /// <summary>
    /// Los power-ups que trae la nave en este nivel: el TRIPLE DISPARO (con su tiempo) y los LINFOCITOS que la escoltan.
    /// Lo agrega FlightPowerUp al jugador la primera vez que agarras algo. Duran todo el nivel (también al reaparecer).
    /// </summary>
    public class ShipPowerUps : MonoBehaviour
    {
        public const float TripleSeconds = 15f;
        public const int MaxAllies = 3, MaxMissiles = 15;

        private PlayerController player;
        private float tripleLeft;
        private readonly List<FlightAlly> allies = new List<FlightAlly>();
        private SpriteRenderer tripleAura;

        public int Allies => allies.Count;
        public bool TripleShot => tripleLeft > 0f;

        public static ShipPowerUps For(PlayerController p)
        {
            ShipPowerUps s = p.GetComponent<ShipPowerUps>();
            if (s == null) { s = p.gameObject.AddComponent<ShipPowerUps>(); s.player = p; }
            return s;
        }

        private void Awake() => player = GetComponent<PlayerController>();

        public void GiveTripleShot(float seconds)
        {
            tripleLeft = Mathf.Min(tripleLeft + seconds, seconds * 2f);
            if (player.Shooter != null) player.Shooter.ExtraShots = 2;
            if (tripleAura == null)
                tripleAura = CodeSprites.Add(transform, CodeSprites.Ring, new Color(1f, 0.85f, 0.3f, 0.6f), 30, new Vector2(0.1f, 0.65f), Vector2.one * 2.3f, "AuraTriple");
            tripleAura.enabled = true;
        }

        public void GiveMissiles(int amount)
        {
            if (player.Bombs != null) player.Bombs.AddBonus(amount, MaxMissiles);
        }

        public bool AddAlly()
        {
            if (allies.Count >= MaxAllies) return false;
            var go = new GameObject("LinfocitoEscolta");
            go.transform.position = transform.position;
            FlightAlly ally = go.AddComponent<FlightAlly>();
            ally.Begin(player, allies.Count);
            allies.Add(ally);
            return true;
        }

        private void Update()
        {
            if (tripleLeft <= 0f) return;
            tripleLeft -= Time.deltaTime;
            if (tripleAura != null)
            {
                tripleAura.transform.localScale = Vector3.one * player.Size * (2.3f + Mathf.Sin(Time.time * 5f) * 0.1f);
                // Parpadea los últimos 3 segundos: ya se va a acabar.
                tripleAura.enabled = tripleLeft > 3f || Mathf.Repeat(tripleLeft * 4f, 1f) > 0.4f;
            }
            if (tripleLeft <= 0f)
            {
                if (player.Shooter != null) player.Shooter.ExtraShots = 0;
                if (tripleAura != null) tripleAura.enabled = false;
                GameplayEvents.RaiseMessage("NODO: Se acabó el TRIPLE DISPARO.");
            }
        }

        private void OnDestroy()
        {
            foreach (FlightAlly a in allies) if (a != null) Destroy(a.gameObject);
        }
    }

    /// <summary>
    /// LINFOCITO que escolta a la nave (sale de un glóbulo blanco que atrapaste): vuela pegado a ella en su lugar de la
    /// formación y le dispara al enemigo más cercano que tenga enfrente, con el medicamento que traes (más débil que
    /// tus cápsulas). No recibe daño.
    /// </summary>
    public class FlightAlly : MonoBehaviour
    {
        private const float FireEvery = 1.05f, Range = 12f, DamageScale = 0.6f;
        // Lugar de cada uno respecto a la nave (en la pantalla): arriba atrás, abajo atrás y más atrás.
        private static readonly Vector2[] Slots = { new Vector2(-1.2f, 1.7f), new Vector2(-1.2f, -0.9f), new Vector2(-2.5f, 0.4f) };

        private PlayerController player;
        private int slot;
        private float fireTimer, phase;
        private Transform visual;

        public void Begin(PlayerController owner, int index)
        {
            player = owner;
            slot = index;
            phase = index * 2.1f;
            fireTimer = 0.6f + index * 0.35f;
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Sprite art = CodeSprites.ResourceArt("Aliados/LinfocitoB");   // Art/Aliados/Resources/Aliados/LinfocitoB.png
            if (art != null) CodeSprites.AddArt(visual, art, 0.95f, 24, Vector2.zero, "Linfocito");
            else
            {
                CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.12f, 0.05f, 0.12f), 23, Vector2.zero, Vector2.one * 0.9f, "Contorno");
                CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.7f, 0.75f, 1f), 24, Vector2.zero, Vector2.one * 0.78f, "Celula");
                CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.35f, 0.2f, 0.6f), 25, new Vector2(-0.03f, -0.02f), Vector2.one * 0.56f, "Nucleo");
                CodeSprites.Add(visual, CodeSprites.Circle, Color.white, 26, new Vector2(0.14f, 0.1f), Vector2.one * 0.12f, "Ojo");
            }
            CodeSprites.Add(transform, CodeSprites.Glow, new Color(0.7f, 0.85f, 1f, 0.35f), 22, Vector2.zero, Vector2.one * 1.6f, "Luz");
            transform.position = Target();
        }

        private Vector2 Target()
        {
            Vector2 ship = player.Position + new Vector2(0f, 0.6f * player.Size);
            Vector2 offset = Slots[Mathf.Clamp(slot, 0, Slots.Length - 1)] + new Vector2(0f, Mathf.Sin(Time.time * 2.2f + phase) * 0.25f);
            return ship + offset;   // el mundo no gira (la cámara sí): +X siempre es "adelante"
        }

        private void Update()
        {
            if (player == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector2 target = Target();
            Vector2 p = transform.position;
            // Si la nave se fue lejos de golpe (reaparecer), llega de un salto.
            p = (target - p).sqrMagnitude > 64f ? target : Vector2.Lerp(p, target, 1f - Mathf.Exp(-7f * dt));
            transform.position = p;
            if (visual != null) visual.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + phase) * 8f);

            if (!player.IsFlying || !player.Health.IsAlive || player.Shooter == null) return;
            fireTimer -= dt;
            if (fireTimer > 0f) return;
            Transform enemy = Nearest(p, Vector2.right);
            if (enemy == null) { fireTimer = 0.2f; return; }
            fireTimer = FireEvery + Random.value * 0.2f;
            Hurtbox box = enemy.GetComponent<Hurtbox>();
            Vector2 aim = (box != null ? box.Center : (Vector2)enemy.position) - p;
            player.Shooter.FireFrom(p + aim.normalized * 0.5f, aim, DamageScale);
        }

        private static Transform Nearest(Vector2 from, Vector2 forward)
        {
            Transform best = null;
            float bestD = Range * Range;
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector2 to = (Vector2)e.transform.position - from;
                if (Vector2.Dot(to, forward) < -0.5f) continue;   // los de atrás no
                float d = to.sqrMagnitude;
                if (d < bestD) { bestD = d; best = e.transform; }
            }
            return best;
        }
    }
}
