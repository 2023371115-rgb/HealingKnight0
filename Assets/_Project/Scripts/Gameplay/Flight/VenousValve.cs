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
}
