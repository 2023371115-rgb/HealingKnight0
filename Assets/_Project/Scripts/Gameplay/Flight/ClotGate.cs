using System.Collections.Generic;
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
    /// Biología: las plaquetas tapan las heridas formando coágulos; dentro de un vaso sano un coágulo es peligroso
    /// (trombosis). Los anticoagulantes (como la heparina) evitan que se formen y crezcan.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ClotGate : MonoBehaviour
    {
        [SerializeField] private bool full;
        [SerializeField] private Vector2 size = new Vector2(2f, 4f);
        [SerializeField] private float contactDamage = 8f;
        [SerializeField, TextArea] private string message;

        private static readonly List<ClotGate> all = new List<ClotGate>();
        private EnemyHealth health;
        private Collider2D solid;
        private bool told;
        private float hurtCooldown;
        private Hurtbox playerBox;
        private Transform visual;

        public bool Full => full;
        public bool Alive => health != null && health.IsAlive;
        public Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        public void Configure(bool blocksAll, Vector2 area, string firstMessage)
        {
            full = blocksAll;
            size = area;
            message = firstMessage;
        }

        /// <summary>¿Hay un coágulo completo (vivo) que empiece antes de esta X? La pantalla no pasa de ahí.</summary>
        public static bool BlocksAt(float x)
        {
            foreach (ClotGate g in all)
                if (g != null && g.full && g.Alive && g.Area.xMin <= x) return true;
            return false;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            solid = GetComponent<Collider2D>();
            visual = transform.Find("Dibujo");
            health.Died += OnDied;
        }

        private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        private void OnDisable() => all.Remove(this);

        private void OnDied(EnemyHealth _)
        {
            // Se deshace: ya no estorba aunque todavía se esté desvaneciendo.
            if (solid != null) solid.enabled = false;
            Audio.Sfx.EffectAt("Coagulo_Disuelve", transform.position, 0.8f);
            if (full) GameplayEvents.RaiseMessage("¡Coágulo disuelto! La sangre vuelve a correr.");
        }

        private void Update()
        {
            if (!Alive) return;
            float dt = Time.deltaTime;
            if (hurtCooldown > 0f) hurtCooldown -= dt;
            // Late con el corazón (la sangre lo golpea).
            if (visual != null) visual.localScale = Vector3.one * (1f + Heartbeat.SurgeNow * 0.04f);

            FlightRoute route = FlightRoute.Current;
            if (!told && route != null && route.View.xMax > Area.xMin + 1f)
            {
                told = true;
                if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
            }

            if (full) return;
            PlayerController player = PlayerController.Current;
            if (player == null || hurtCooldown > 0f) return;
            if (playerBox == null) playerBox = player.GetComponent<Hurtbox>();
            if (playerBox == null || !playerBox.IsAlive || !playerBox.OverlapsRect(Area)) return;
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
