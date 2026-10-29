using HealingKnight.Gameplay.Audio;
using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Jefe del Hipocampo: una cadena de viriones de rabia que avanza como el ciempiés de Centipede.
    /// (La rabia real viaja por dentro de los nervios hasta el cerebro.)
    ///  - Corre por un riel de axón; al llegar a la orilla de la arena baja al siguiente riel y da la vuelta.
    ///    Del riel más bajo regresa al de arriba.
    ///  - Cada virión es una parte con su propia vida. Si destruyes uno del MEDIO, la cadena se PARTE en dos
    ///    y la mitad de atrás sigue por su cuenta con cabeza nueva (como en Centipede).
    ///  - Cada vez que una cabeza rebota en la orilla invoca un RAYO sobre ti.
    ///  - Fase 2 (60 %): las cabezas disparan y, al pasar junto a una neurona sana, la INFECTAN.
    ///  - Fase 3 (30 %): FURIA. Más rápida y aparece un riel a ras de piso: hay que saltar la cadena.
    /// Se trata con antiviral. La barra del jefe es la vida sumada de todos los viriones.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class RabiesBoss : MonoBehaviour
    {
        private class Chain
        {
            public readonly List<RabiesSegment> segments = new List<RabiesSegment>();
            public int dir = -1;
            public int rail;
            public bool changingRail;
            public float shootCooldown = 2f;
            public float infectCooldown;
            public RabiesSegment Head => segments.Count > 0 ? segments[0] : null;
        }

        [Header("Identidad")]
        [SerializeField] private string bossName = "VIRUS DE LA RABIA";
        [SerializeField, TextArea] private string introMessage =
            "NODO: ¡Rabia! Una cadena de viriones viaja por los nervios. Antiviral. Si rompes el medio, se parte en dos. (En la vida real se previene con la vacuna.)";
        [SerializeField, TextArea] private string phase2Message = "NODO: ¡Las cabezas disparan e infectan neuronas! Cálmalas con Anticonvulsivo.";
        [SerializeField, TextArea] private string phase3Message = "NODO: ¡Furia! Ahora también corre a ras de piso: sáltala.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Virus de la rabia eliminado. El hipocampo está a salvo.";

        [Header("Cadena")]
        [SerializeField] private RabiesSegment segmentPrefab;
        [SerializeField] private EnemyData segmentData;
        [SerializeField] private int segmentCount = 12;
        [SerializeField] private float spacing = 0.85f;
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float railChangeSpeed = 6f;

        [Header("Rieles de axones (altura sobre el piso)")]
        [SerializeField] private float floorY = 0f;
        [SerializeField] private float[] railHeights = { 6.2f, 4.4f, 2.6f };
        [Tooltip("Riel extra que aparece en la fase de furia (hay que saltar la cadena).")]
        [SerializeField] private float furyRailHeight = 0.7f;

        [Header("Disparos (fase 2+)")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private float shotSpeed = 8f;
        [SerializeField] private float shotDamage = 10f;
        [SerializeField] private Color shotColor = new Color(1f, 0.4f, 0.7f);

        [Header("Rayos")]
        [SerializeField] private LightningStorm stormPrefab;

        [Header("Contacto")]
        [SerializeField] private float contactDamage = 15f;

        [Header("Fases (por % de vida total)")]
        [SerializeField] private float phase2At = 0.6f;
        [SerializeField] private float phase3At = 0.3f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private LightningStorm storm;
        private readonly List<Chain> chains = new List<Chain>();
        private readonly List<RabiesSegment> allSegments = new List<RabiesSegment>();
        private int lastPhase = 1;
        private int reversals;
        private float contactCooldown;
        private bool finished;

        private float Health01
        {
            get
            {
                if (segmentCount <= 0) return 0f;
                float sum = 0f;
                foreach (RabiesSegment s in allSegments)
                    if (s != null && s.Health != null && s.Health.IsAlive) sum += s.Health.Health01;
                return sum / segmentCount;
            }
        }

        private int Phase { get; set; } = 1;
        private float SpeedMultiplier => Phase == 3 ? 1.5f : Phase == 2 ? 1.2f : 1f;
        private int RailCount => railHeights.Length + (Phase == 3 ? 1 : 0);

        private float RailY(int rail)
        {
            if (rail < railHeights.Length) return floorY + railHeights[rail];
            return floorY + furyRailHeight;
        }

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (!finished) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            if (storm != null) Destroy(storm.gameObject);
            foreach (RabiesSegment s in allSegments)
                if (s != null) Destroy(s.gameObject);
        }

        private void Start()
        {
            health.Invulnerable = true; // el daño se lo hacen a los viriones, no a la raíz
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 10, 40);
            if (stormPrefab != null)
            {
                storm = Instantiate(stormPrefab);
                storm.Configure(WalkableArea.MinX, WalkableArea.MaxX, floorY);
                storm.AutoStrikes = false;
            }

            // La cadena entra por la derecha, en el riel de arriba.
            var chain = new Chain { dir = -1, rail = 0 };
            float startX = WalkableArea.MaxX + 1.5f;
            for (int i = 0; i < segmentCount; i++)
            {
                RabiesSegment seg = Instantiate(segmentPrefab, new Vector3(startX + i * spacing, RailY(0), 0f), Quaternion.identity);
                if (segmentData != null) seg.Health.Configure(segmentData, null);
                seg.Init(this);
                chain.segments.Add(seg);
                allSegments.Add(seg);
            }
            chains.Add(chain);
            RefreshHeads(chain);

            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
        }

        private void Update()
        {
            if (finished) return;
            float dt = Time.deltaTime;
            if (contactCooldown > 0f) contactCooldown -= dt;

            float h = Health01;
            health.SetHealth01(h);
            Phase = h <= phase3At ? 3 : h <= phase2At ? 2 : 1;
            if (Phase != lastPhase)
            {
                lastPhase = Phase;
                if (Phase == 2 && !string.IsNullOrEmpty(phase2Message)) GameplayEvents.RaiseMessage(phase2Message);
                if (Phase == 3 && !string.IsNullOrEmpty(phase3Message)) GameplayEvents.RaiseMessage(phase3Message);
            }

            PlayerController player = PlayerController.Current;
            for (int i = chains.Count - 1; i >= 0; i--)
            {
                Chain c = chains[i];
                c.segments.RemoveAll(s => s == null || !s.Health.IsAlive);
                if (c.segments.Count == 0) { chains.RemoveAt(i); continue; }
                UpdateChain(c, player, dt);
            }

            TryContactDamage(player);
        }

        // ---------------------------------------------------------------- movimiento tipo Centipede

        private void UpdateChain(Chain c, PlayerController player, float dt)
        {
            RabiesSegment head = c.Head;
            Vector2 p = head.transform.position;
            float v = speed * SpeedMultiplier;

            if (c.changingRail)
            {
                float y = Mathf.MoveTowards(p.y, RailY(c.rail), railChangeSpeed * SpeedMultiplier * dt);
                head.transform.position = new Vector3(p.x, y, 0f);
                if (Mathf.Abs(y - RailY(c.rail)) < 0.02f)
                {
                    c.changingRail = false;
                    c.dir = -c.dir;
                }
            }
            else
            {
                float x = p.x + c.dir * v * dt;
                float minX = WalkableArea.MinX + 0.8f;
                float maxX = WalkableArea.MaxX - 0.8f;
                bool atEdge = (c.dir < 0 && x <= minX) || (c.dir > 0 && x >= maxX && p.x <= maxX + 0.01f);
                // Mientras entra desde fuera de la arena no cuenta como orilla.
                if (c.dir > 0 && p.x > maxX + 0.01f) atEdge = false;
                head.transform.position = new Vector3(x, Mathf.MoveTowards(p.y, RailY(c.rail), railChangeSpeed * dt), 0f);

                if (atEdge)
                {
                    // Baja al siguiente riel (del más bajo regresa al de arriba) y da la vuelta.
                    c.rail = (c.rail + 1) % RailCount;
                    c.changingRail = true;
                    OnReversal(player);
                }
            }

            head.SetFacing(c.dir, c.changingRail);

            // El cuerpo sigue a la cabeza como una cadena.
            for (int i = 1; i < c.segments.Count; i++)
            {
                Transform prev = c.segments[i - 1].transform;
                Transform cur = c.segments[i].transform;
                Vector2 delta = (Vector2)prev.position - (Vector2)cur.position;
                float d = delta.magnitude;
                if (d > spacing)
                {
                    Vector2 np = (Vector2)prev.position - delta / d * spacing;
                    cur.position = new Vector3(np.x, np.y, 0f);
                }
                c.segments[i].SetFacing(delta.x >= 0f ? 1 : -1, false);
            }

            if (Phase >= 2 && player != null && player.Health.IsAlive)
            {
                c.shootCooldown -= dt;
                if (c.shootCooldown <= 0f && !c.changingRail)
                {
                    Shoot(head.transform.position, player);
                    c.shootCooldown = (Phase == 3 ? 2f : 3f) + Random.value;
                }

                c.infectCooldown -= dt;
                if (c.infectCooldown <= 0f) TryInfectNear(c, head.transform.position);
            }
        }

        private void OnReversal(PlayerController player)
        {
            reversals++;
            if (player != null) Sfx.Effect("Rabia_Chillido", 0.6f);
            if (storm == null || player == null || !player.Health.IsAlive) return;
            bool strike = Phase >= 2 || reversals % 2 == 0;
            if (strike) storm.ForceStrike(player.Position.x + Random.Range(-1f, 1f));
        }

        // ---------------------------------------------------------------- partes

        /// <summary>Lo llama cada virión al morir: si estaba en medio, la cadena se parte en dos.</summary>
        public void OnSegmentDied(RabiesSegment seg)
        {
            foreach (Chain c in chains)
            {
                int k = c.segments.IndexOf(seg);
                if (k < 0) continue;

                var back = new List<RabiesSegment>();
                for (int i = k + 1; i < c.segments.Count; i++) back.Add(c.segments[i]);
                c.segments.RemoveRange(k, c.segments.Count - k);

                if (back.Count > 0)
                {
                    // La mitad de atrás sigue sola con cabeza nueva: como en Centipede, baja de riel y da la vuelta.
                    var tail = new Chain { dir = c.dir, rail = (c.rail + 1) % RailCount, changingRail = true, shootCooldown = 2.5f };
                    tail.segments.AddRange(back);
                    chains.Add(tail);
                    RefreshHeads(tail);
                }
                RefreshHeads(c);
                break;
            }

            bool anyAlive = false;
            foreach (RabiesSegment s in allSegments)
                if (s != null && s != seg && s.Health.IsAlive) { anyAlive = true; break; }
            if (!anyAlive)
            {
                health.Invulnerable = false;
                health.Kill();
            }
        }

        private static void RefreshHeads(Chain c)
        {
            for (int i = 0; i < c.segments.Count; i++)
                if (c.segments[i] != null) c.segments[i].SetHead(i == 0);
        }

        private void TryInfectNear(Chain c, Vector2 headPos)
        {
#pragma warning disable CS0618
            HealthyNeuron[] neurons = FindObjectsByType<HealthyNeuron>(FindObjectsSortMode.None);
#pragma warning restore CS0618
            foreach (HealthyNeuron n in neurons)
            {
                if (Vector2.Distance(n.transform.position, headPos) > 1.3f) continue;
                if (NeuroprotectionField.Protects(n.transform.position)) continue;
                n.Infect();
                c.infectCooldown = 6f;
                return;
            }
        }

        private void Shoot(Vector2 origin, PlayerController player)
        {
            if (pool == null) return;
            Vector2 dir = ((player.Position + Vector2.up * 0.9f) - origin).normalized;
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), origin, CombatAudio.Enemies);
            Projectile p = pool.Get();
            p.Launch(new ProjectileShot
            {
                origin = origin,
                direction = dir,
                speed = shotSpeed,
                range = 16f,
                damage = shotDamage,
                knockback = 4f,
                radius = 0.22f,
                sourceTeam = Team.Enemy,
                targetTeam = Team.Player,
                color = shotColor,
                groundMask = groundMask
            }, finished => pool.Release(finished));
        }

        private void TryContactDamage(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive) return;

            foreach (Chain c in chains)
            {
                foreach (RabiesSegment s in c.segments)
                {
                    if (s == null || !box.OverlapsCircle(s.transform.position, s.ContactRadius)) continue;
                    DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, s.transform.position);
                    info.knockback = 7f;
                    box.Damageable.ApplyDamage(info);
                    contactCooldown = 1f;
                    return;
                }
            }
        }

        private void OnDied(EnemyHealth _)
        {
            finished = true;
            if (storm != null) Destroy(storm.gameObject);
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }
}
