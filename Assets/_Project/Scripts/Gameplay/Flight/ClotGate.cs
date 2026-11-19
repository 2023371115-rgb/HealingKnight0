using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// COÁGULO: plaquetas pegadas unas a otras y amarradas con hilos de fibrina. Solo lo deshace el ANTICOAGULANTE
    /// (las demás cápsulas rebotan; eso lo decide su EnemyData "Plaquetas").
    /// - Completo ('full'): tapa todo el vaso. La pantalla se detiene frente a él hasta que lo disuelvas.
    /// - Pegado a la pared: tapa una parte; si lo tocas te lastima y te empuja (pasa por el hueco o disuélvelo).
    /// - Formándose ('formTime' > 0): cuando aparece en la pantalla, las dos mitades (de arriba y de abajo) van
    ///   creciendo hasta cerrarse. Si lo disuelves antes, pasas; si se cierra, se vuelve completo y detiene la pantalla.
    /// Biología: las plaquetas tapan las heridas formando coágulos; dentro de un vaso sano un coágulo es peligroso
    /// (trombosis). Los anticoagulantes (como la heparina) evitan que se formen y crezcan.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ClotGate : MonoBehaviour, IFlightBlocker
    {
        [SerializeField] private bool full;
        [Tooltip("0 = cuenta siempre; 1/2 = solo si escogiste esa rama de la bifurcación (FlightRoute).")]
        [SerializeField] private int branch;
        [SerializeField] private Vector2 size = new Vector2(2f, 4f);
        [SerializeField] private float contactDamage = 8f;
        [Tooltip("Si es mayor que 0: se va formando (segundos hasta cerrarse) desde que entra a la pantalla.")]
        [SerializeField] private float formTime;
        [SerializeField, TextArea] private string message;

        private const float StartOpen = 0.12f;   // qué tanto mide cada mitad al empezar a formarse
        private EnemyHealth health;
        private Collider2D solid;
        private bool told, formed;
        private float hurtCooldown, progress;
        private Hurtbox playerBox;
        private Transform visual, upperHalf, lowerHalf;

        public bool Full => full;
        public bool Alive => health != null && health.IsAlive;
        public Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);
        private bool Forming => formTime > 0f && !formed;

        // IFlightBlocker
        public float BlockX => Area.xMin;
        public bool Blocking => full && Alive;
        public int Branch => branch;

        public void Configure(bool blocksAll, Vector2 area, string firstMessage, float secondsToClose = 0f)
        {
            full = blocksAll;
            size = area;
            message = firstMessage;
            formTime = secondsToClose;
        }

        /// <summary>Que solo cuente en una rama de la bifurcación (1 arriba, 2 abajo).</summary>
        public void SetBranch(int routeBranch) => branch = routeBranch;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            solid = GetComponent<Collider2D>();
            visual = transform.Find("Dibujo");
            if (visual != null)
            {
                upperHalf = visual.Find("MitadArriba");
                lowerHalf = visual.Find("MitadAbajo");
            }
            health.Died += OnDied;
            if (formTime > 0f)
            {
                full = false;
                if (solid != null) solid.enabled = false;
                SetHalves(StartOpen);
            }
        }

        private void OnEnable() => FlightBlockers.Add(this);
        private void OnDisable() => FlightBlockers.Remove(this);

        private void OnDied(EnemyHealth _)
        {
            // Se deshace: ya no estorba aunque todavía se esté desvaneciendo.
            if (solid != null) solid.enabled = false;
            Audio.Sfx.EffectAt("Coagulo_Disuelve", transform.position, 0.8f);
            if (full) GameplayEvents.RaiseMessage("¡Coágulo disuelto! La sangre vuelve a correr.");
            else if (Forming && progress > 0f) GameplayEvents.RaiseMessage("¡Lo disolviste antes de que se cerrara!");
        }

        private void SetHalves(float k)
        {
            if (upperHalf != null) upperHalf.localScale = new Vector3(1f, k, 1f);
            if (lowerHalf != null) lowerHalf.localScale = new Vector3(1f, k, 1f);
        }

        private void Update()
        {
            if (!Alive) return;
            float dt = Time.deltaTime;
            if (hurtCooldown > 0f) hurtCooldown -= dt;
            // Late con el corazón (la sangre lo golpea).
            if (visual != null) visual.localScale = Vector3.one * (1f + Heartbeat.SurgeNow * 0.04f);

            FlightRoute route = FlightRoute.Current;
            bool onScreen = route != null && route.InBranch(branch) && route.View.xMax > Area.xMin + 1f;
            if (!told && onScreen)
            {
                told = true;
                if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
            }
            PlayerController player = PlayerController.Current;

            // Se va cerrando: las dos mitades crecen desde las paredes hasta juntarse.
            if (Forming && onScreen)
            {
                progress = Mathf.Min(1f, progress + dt / formTime);
                SetHalves(Mathf.Lerp(StartOpen, 1f, progress));
                // No se cierra con la nave adentro (ni a medio pasar: se cuenta el ancho de la nave).
                Rect grown = new Rect(Area.xMin - 0.95f, Area.yMin, Area.width + 1.9f, Area.height);
                bool shipInside = player != null && grown.Contains(player.Position + new Vector2(0f, 0.6f));
                if (progress >= 1f && !shipInside)
                {
                    formed = true;
                    full = true;
                    if (solid != null) solid.enabled = true;
                    GameplayEvents.RaiseMessage("¡Se cerró el coágulo! Ahora hay que disolverlo todo.");
                }
            }

            if (full || player == null || hurtCooldown > 0f) return;
            if (playerBox == null) playerBox = player.GetComponent<Hurtbox>();
            if (playerBox == null || !playerBox.IsAlive || !playerBox.OverlapsRect(Area)) return;
            if (Forming)
            {
                // Mientras se forma solo lastiman las dos mitades: si cabes completo en el hueco de en medio, pasas.
                float gap = size.y * (1f - Mathf.Lerp(StartOpen, 1f, progress));
                Rect box = playerBox.WorldRect;
                float y = transform.position.y;
                if (box.yMin >= y - gap * 0.5f && box.yMax <= y + gap * 0.5f) return;
            }
            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, transform.position);
            playerBox.Damageable.ApplyDamage(info);
            hurtCooldown = 0.7f;
            ShipFlight ship = player.Flight;
            if (ship != null) ship.Kick(new Vector2(-6f, player.Position.y > transform.position.y ? 5f : -5f));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = full ? new Color(1f, 0.2f, 0.4f, 0.9f) : new Color(1f, 0.6f, 0.8f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        }
    }
}
