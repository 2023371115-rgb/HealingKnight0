using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// El camino de la pantalla en un nivel de vuelo: la cámara avanza SOLA por una línea de puntos (el centro del vaso
    /// sanguíneo) y el jugador vuela dentro de lo que se ve.
    /// - Avanza a 'baseSpeed' y en cada latido la sangre la empuja más ('surgeBoost').
    /// - Al empezar espera 'launchDelay' (dentro del ventrículo) y luego el latido te DISPARA a la aorta.
    /// - Se detiene frente a un coágulo que tapa todo (ClotGate) hasta que lo disuelvas, y al final del camino.
    /// - Si mueres, regresa al último punto de guardado: borra a los enemigos de la pantalla y rearma las oleadas.
    /// Los puntos pueden subir o bajar (el arco de la aorta); por ahora el camino siempre avanza a la derecha.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class FlightRoute : MonoBehaviour
    {
        [SerializeField] private Vector2[] path = { new Vector2(0f, 2.5f), new Vector2(100f, 2.5f) };
        [SerializeField] private float baseSpeed = 3.2f;
        [SerializeField] private float surgeBoost = 2.2f;
        [SerializeField] private float launchDelay = 2.4f;
        [SerializeField] private float launchBoost = 9f;
        [SerializeField, TextArea] private string launchMessage = "¡El ventrículo izquierdo se contrae y te DISPARA a la AORTA!";

        public static FlightRoute Current { get; private set; }

        public Vector2 CameraPosition { get; private set; }
        public Vector2 ScrollVelocity { get; private set; }
        public Rect View { get; private set; }
        public float HalfWidth { get; private set; }
        public float HalfHeight { get; private set; }
        public bool Launched => launched;
        public bool AtEnd => distance >= total - 0.01f;
        /// <summary>Multiplica la velocidad (tramos lentos o rápidos). 1 = normal.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        private float[] cumulative;
        private float total, distance, clock, launchKick;
        private bool launched;
        private PlayerController player;
        private ShipFlight ship;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        public void Configure(Vector2[] points, float speed, float surge)
        {
            path = points;
            baseSpeed = speed;
            surgeBoost = surge;
        }

        private void Awake()
        {
            Current = this;
            Measure();
            CameraPosition = path.Length > 0 ? path[0] : Vector2.zero;
            GameCamera.Override = () => CameraPosition;
            UpdateView();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            GameCamera.Override = null;
            if (player != null) player.Respawned -= OnRespawned;
        }

        private void Measure()
        {
            if (path == null || path.Length < 2) path = new[] { Vector2.zero, new Vector2(100f, 0f) };
            cumulative = new float[path.Length];
            total = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                total += Vector2.Distance(path[i - 1], path[i]);
                cumulative[i] = total;
            }
        }

        /// <summary>Punto del camino a 'd' unidades del principio.</summary>
        public Vector2 PointAt(float d)
        {
            d = Mathf.Clamp(d, 0f, total);
            for (int i = 1; i < path.Length; i++)
            {
                if (d > cumulative[i]) continue;
                float seg = cumulative[i] - cumulative[i - 1];
                float k = seg > 0f ? (d - cumulative[i - 1]) / seg : 0f;
                return Vector2.Lerp(path[i - 1], path[i], k);
            }
            return path[path.Length - 1];
        }

        /// <summary>Distancia del camino donde la X es 'x' (el camino avanza siempre a la derecha).</summary>
        public float DistanceAtX(float x)
        {
            if (x <= path[0].x) return 0f;
            for (int i = 1; i < path.Length; i++)
            {
                if (x > path[i].x) continue;
                float span = path[i].x - path[i - 1].x;
                float k = span > 0f ? (x - path[i - 1].x) / span : 0f;
                return Mathf.Lerp(cumulative[i - 1], cumulative[i], k);
            }
            return total;
        }

        /// <summary>Altura del centro del camino en esa X (las paredes del vaso la siguen).</summary>
        public float CenterYAt(float x) => PointAt(DistanceAtX(x)).y;

        private void UpdateView()
        {
            Camera cam = Camera.main;
            HalfHeight = cam != null ? cam.orthographicSize : 5.4f;
            HalfWidth = HalfHeight * (cam != null ? cam.aspect : 16f / 9f);
            View = new Rect(CameraPosition.x - HalfWidth, CameraPosition.y - HalfHeight, HalfWidth * 2f, HalfHeight * 2f);
        }

        private void Hook()
        {
            PlayerController p = PlayerController.Current;
            if (p == null || p == player) return;
            if (player != null) player.Respawned -= OnRespawned;
            player = p;
            player.Respawned += OnRespawned;
            ship = player.GetComponent<ShipFlight>();
            if (ship == null) ship = player.gameObject.AddComponent<ShipFlight>();
            ship.Activate();
        }

        private void Update()
        {
            Hook();
            float dt = Time.deltaTime;
            UpdateView();
            if (dt <= 0f) return;
            clock += dt;

            bool alive = player != null && player.Health.IsAlive;
            if (Heartbeat.Current != null) Heartbeat.Current.SetRunning(alive || !launched);

            float speed = 0f;
            if (!launched)
            {
                if (clock >= launchDelay)
                {
                    launched = true;
                    launchKick = 1f;
                    if (!string.IsNullOrEmpty(launchMessage)) Level.GameplayEvents.RaiseMessage(launchMessage);
                    Audio.Sfx.Effect("Nave_Despegue", 0.9f);
                    if (ship != null) ship.Kick(new Vector2(9f, 0f));
                }
            }
            else if (alive && !AtEnd && !ClotGate.BlocksAt(CameraPosition.x + HalfWidth * 0.45f))
            {
                launchKick = Mathf.MoveTowards(launchKick, 0f, dt * 0.7f);
                speed = (baseSpeed + Heartbeat.SurgeNow * surgeBoost) * SpeedMultiplier + launchKick * launchBoost;
            }

            Vector2 before = CameraPosition;
            distance = Mathf.Min(total, distance + speed * dt);
            CameraPosition = PointAt(distance);
            ScrollVelocity = (CameraPosition - before) / dt;
            UpdateView();
        }

        /// <summary>
        /// Reaparecer: la pantalla regresa a donde quedó el jugador (el punto de guardado), se borran los enemigos y
        /// balas que había y se rearman las oleadas de adelante.
        /// </summary>
        private void OnRespawned(Vector2 at)
        {
            // La pantalla empieza justo en el punto de guardado (la orilla izquierda un poco antes).
            distance = DistanceAtX(at.x + HalfWidth * 0.8f);
            CameraPosition = PointAt(distance);
            ScrollVelocity = Vector2.zero;
            launchKick = 0f;
            UpdateView();
            Vector2 safe = new Vector2(CameraPosition.x - HalfWidth * 0.6f, CameraPosition.y - 0.5f);
            player.Motor.Teleport(safe);

            foreach (EnemyHealth e in new List<EnemyHealth>(EnemyHealth.Active))
                if (e != null && e.GetComponent<FlightEnemy>() != null) Destroy(e.gameObject);
            foreach (Projectile p in new List<Projectile>(Projectile.Flying))
                if (p != null && p.SourceTeam == Team.Enemy) p.Consume();
            FlightWave.RearmFrom(View.xMin, View.xMax);
        }

        private void OnDrawGizmos()
        {
            if (path == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.35f, 0.9f);
            for (int i = 1; i < path.Length; i++) Gizmos.DrawLine(path[i - 1], path[i]);
        }
    }
}
