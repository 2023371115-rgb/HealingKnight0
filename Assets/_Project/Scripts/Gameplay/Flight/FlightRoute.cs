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
    /// - BIFURCACIÓN (opcional): el vaso se parte en dos ramas. Tú escoges por cuál lado de la "punta" pasa la nave;
    ///   la pantalla sigue esa rama (Branch = 1 arriba, 2 abajo). Si reapareces antes de la punta, puedes escoger otra vez.
    /// Los puntos pueden subir o bajar; el camino siempre avanza a la derecha (cada X tiene un solo punto).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class FlightRoute : MonoBehaviour
    {
        [Tooltip("Camino principal (hasta la bifurcación, si hay).")]
        [SerializeField] private Vector2[] path = { new Vector2(0f, 2.5f), new Vector2(100f, 2.5f) };
        [SerializeField] private float baseSpeed = 3.2f;
        [SerializeField] private float surgeBoost = 2.2f;
        [SerializeField] private float launchDelay = 2.4f;
        [SerializeField] private float launchBoost = 9f;
        [SerializeField, TextArea] private string launchMessage = "¡El ventrículo izquierdo se contrae y te DISPARA a la AORTA!";

        [Header("Bifurcación (vacío = no hay)")]
        [Tooltip("Siguen al final del camino principal.")]
        [SerializeField] private Vector2[] upperPath = new Vector2[0];
        [SerializeField] private Vector2[] lowerPath = new Vector2[0];
        [Tooltip("La punta que separa las dos ramas: la nave escoge rama al pasarla (por arriba o por abajo).")]
        [SerializeField] private Vector2 forkTip;
        [Tooltip("La rama de arriba es más rápida (multiplica la velocidad).")]
        [SerializeField] private float upperSpeed = 1.25f;
        [SerializeField, TextArea] private string upperMessage;
        [SerializeField, TextArea] private string lowerMessage;

        [Header("Después de la bifurcación (vacío = no hay)")]
        [Tooltip("Camino que sigue después de CUALQUIERA de las dos ramas (más a la derecha). Se llega con un fundido " +
                 "a negro: por ejemplo, las dos ilíacas llevan a 'una pierna' (la arteria femoral).")]
        [SerializeField] private Vector2[] continuation = new Vector2[0];
        [SerializeField, TextArea] private string continuationMessage;

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
        /// <summary>0 = todavía no escoges (o no hay bifurcación), 1 = rama de arriba, 2 = rama de abajo.</summary>
        public int Branch => branch;
        public bool HasFork => upperPath != null && lowerPath != null && upperPath.Length > 0 && lowerPath.Length > 0;

        private Vector2[] active;
        private float[] cumulative;
        private float total, mainTotal, distance, clock, launchKick;
        private bool launched;
        private int branch;
        // Salto (fundido) del final de la rama al principio de la continuación.
        private float jumpFrom = -1f, jumpTo = -1f, fade;
        private int fadeState;          // 0 nada, 1 oscureciendo, 2 aclarando
        private SpriteRenderer fadeRenderer;
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

        /// <summary>Camino que sigue después de las dos ramas (se llega con un fundido a negro).</summary>
        public void ConfigureContinuation(Vector2[] points, string message)
        {
            continuation = points;
            continuationMessage = message;
        }

        /// <summary>Agrega una bifurcación al final del camino principal.</summary>
        public void ConfigureFork(Vector2[] upper, Vector2[] lower, Vector2 tip, float upperSpeedMultiplier, string upperText, string lowerText)
        {
            upperPath = upper;
            lowerPath = lower;
            forkTip = tip;
            upperSpeed = upperSpeedMultiplier;
            upperMessage = upperText;
            lowerMessage = lowerText;
        }

        private void Awake()
        {
            Current = this;
            Build(0);
            CameraPosition = active.Length > 0 ? active[0] : Vector2.zero;
            GameCamera.Override = () => CameraPosition;
            UpdateView();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            GameCamera.Override = null;
            if (player != null) player.Respawned -= OnRespawned;
        }

        /// <summary>Arma el camino activo: el principal y, si ya escogiste, la rama.</summary>
        private void Build(int chosen)
        {
            branch = chosen;
            if (path == null || path.Length < 2) path = new[] { Vector2.zero, new Vector2(100f, 0f) };
            var list = new List<Vector2>(path);
            Vector2[] extra = chosen == 1 ? upperPath : chosen == 2 ? lowerPath : null;
            if (extra != null)
                foreach (Vector2 p in extra)
                    if (p.x > list[list.Count - 1].x + 0.01f) list.Add(p);
            int branchEnd = list.Count - 1;
            bool withContinuation = extra != null && continuation != null && continuation.Length > 1;
            if (withContinuation)
                foreach (Vector2 p in continuation)
                    if (p.x > list[list.Count - 1].x + 0.01f) list.Add(p);
            active = list.ToArray();
            cumulative = new float[active.Length];
            total = 0f;
            mainTotal = 0f;
            for (int i = 1; i < active.Length; i++)
            {
                total += Vector2.Distance(active[i - 1], active[i]);
                cumulative[i] = total;
                if (i < path.Length) mainTotal = total;
            }
            jumpFrom = withContinuation ? cumulative[branchEnd] : -1f;
            jumpTo = withContinuation && branchEnd + 1 < active.Length ? cumulative[branchEnd + 1] : -1f;
        }

        /// <summary>Punto del camino a 'd' unidades del principio.</summary>
        public Vector2 PointAt(float d)
        {
            d = Mathf.Clamp(d, 0f, total);
            for (int i = 1; i < active.Length; i++)
            {
                if (d > cumulative[i]) continue;
                float seg = cumulative[i] - cumulative[i - 1];
                float k = seg > 0f ? (d - cumulative[i - 1]) / seg : 0f;
                return Vector2.Lerp(active[i - 1], active[i], k);
            }
            return active[active.Length - 1];
        }

        /// <summary>Distancia del camino donde la X es 'x' (el camino avanza siempre a la derecha).</summary>
        public float DistanceAtX(float x)
        {
            if (x <= active[0].x) return 0f;
            for (int i = 1; i < active.Length; i++)
            {
                if (x > active[i].x) continue;
                float span = active[i].x - active[i - 1].x;
                float k = span > 0f ? (x - active[i - 1].x) / span : 0f;
                return Mathf.Lerp(cumulative[i - 1], cumulative[i], k);
            }
            return total;
        }

        /// <summary>Altura del centro del camino en esa X (las paredes del vaso la siguen).</summary>
        public float CenterYAt(float x) => PointAt(DistanceAtX(x)).y;

        /// <summary>¿Esto (de la rama 'b') cuenta ahora? 0 = de todas. Lo de una rama no cuenta hasta escogerla.</summary>
        public bool InBranch(int b) => b == 0 || b == branch;

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

        /// <summary>
        /// Escoge la rama: cuando la nave pasa la punta (por arriba o por abajo). Si la pantalla llega al final del
        /// camino principal antes, se escoge por la altura de la nave en ese momento.
        /// </summary>
        private void CheckFork()
        {
            if (!HasFork || branch != 0 || player == null) return;
            float shipY = player.Position.y + 0.8f;
            bool passedTip = player.Position.x >= forkTip.x;
            if (!passedTip && distance < mainTotal - 0.01f) return;
            int chosen = shipY >= forkTip.y ? 1 : 2;
            Build(chosen);
            string text = chosen == 1 ? upperMessage : lowerMessage;
            if (!string.IsNullOrEmpty(text)) Level.GameplayEvents.RaiseMessage(text);
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
            if (alive) CheckFork();

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
            else if (fadeState == 1 || UpdateJump())
            {
                // Oscureciendo para el salto: la pantalla espera.
            }
            else if (alive && !AtEnd && !FlightBlockers.Blocks(CameraPosition.x + HalfWidth * 0.45f, player.Position.x, branch))
            {
                launchKick = Mathf.MoveTowards(launchKick, 0f, dt * 0.7f);
                float branchSpeed = branch == 1 && distance > mainTotal ? upperSpeed : 1f;
                speed = (baseSpeed + Heartbeat.SurgeNow * surgeBoost) * SpeedMultiplier * branchSpeed + launchKick * launchBoost;
            }

            Vector2 before = CameraPosition;
            distance = Mathf.Min(total, distance + speed * dt);
            if (jumpFrom >= 0f && distance > jumpFrom && distance < jumpTo) distance = jumpFrom;   // espera el salto
            CameraPosition = PointAt(distance);
            ScrollVelocity = (CameraPosition - before) / dt;
            UpdateView();
            UpdateFade(dt);
        }

        /// <summary>Al llegar al final de la rama: se oscurece la pantalla. Devuelve true si está esperando el salto.</summary>
        private bool UpdateJump()
        {
            if (jumpFrom < 0f || fadeState != 0 || distance < jumpFrom - 0.01f || distance >= jumpTo) return false;
            fadeState = 1;
            return true;
        }

        /// <summary>Fundido a negro y salto a la continuación (la nave conserva su lugar en la pantalla).</summary>
        private void UpdateFade(float dt)
        {
            if (fadeState == 0) return;
            if (fadeRenderer == null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                fadeRenderer = CodeSprites.Add(cam.transform, CodeSprites.Square, Color.black, 900, Vector2.zero, new Vector2(60f, 40f), "Fundido");
                fadeRenderer.transform.localPosition = new Vector3(0f, 0f, 2f);
            }
            if (fadeState == 1)
            {
                fade = Mathf.MoveTowards(fade, 1f, dt / 0.35f);
                if (fade >= 1f)
                {
                    Vector2 from = PointAt(jumpFrom), to = PointAt(jumpTo);
                    distance = jumpTo;
                    CameraPosition = to;
                    ScrollVelocity = Vector2.zero;
                    UpdateView();
                    if (player != null) player.Motor.Teleport(player.Position + (to - from));
                    ClearScreen();
                    fadeState = 2;
                    if (!string.IsNullOrEmpty(continuationMessage)) Level.GameplayEvents.RaiseMessage(continuationMessage);
                }
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 0f, dt / 0.45f);
                if (fade <= 0f) fadeState = 0;
            }
            fadeRenderer.color = new Color(0f, 0f, 0f, fade);
            fadeRenderer.enabled = fade > 0.001f;
        }

        /// <summary>Borra a los enemigos de vuelo y las balas enemigas que hay en la pantalla.</summary>
        private static void ClearScreen()
        {
            foreach (EnemyHealth e in new List<EnemyHealth>(EnemyHealth.Active))
                if (e != null && e.GetComponent<FlightEnemy>() != null) Destroy(e.gameObject);
            foreach (Projectile p in new List<Projectile>(Projectile.Flying))
                if (p != null && p.SourceTeam == Team.Enemy) p.Consume();
        }

        /// <summary>
        /// Reaparecer: la pantalla regresa a donde quedó el jugador (el punto de guardado), se borran los enemigos y
        /// balas que había y se rearman las oleadas de adelante. Antes de la bifurcación se puede volver a escoger.
        /// </summary>
        private void OnRespawned(Vector2 at)
        {
            if (HasFork && branch != 0 && at.x < forkTip.x - 4f) Build(0);
            // La pantalla empieza justo en el punto de guardado (la orilla izquierda un poco antes).
            distance = DistanceAtX(at.x + HalfWidth * 0.8f);
            CameraPosition = PointAt(distance);
            ScrollVelocity = Vector2.zero;
            launchKick = 0f;
            UpdateView();
            Vector2 safe = new Vector2(CameraPosition.x - HalfWidth * 0.6f, CameraPosition.y - 0.5f);
            player.Motor.Teleport(safe);
            fadeState = 0;
            fade = 0f;
            if (fadeRenderer != null) fadeRenderer.enabled = false;

            ClearScreen();
            FlightWave.RearmFrom(View.xMin, View.xMax);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.35f, 0.9f);
            DrawLine(path);
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.9f);
            DrawLine(upperPath);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            DrawLine(lowerPath);
            Gizmos.color = new Color(0.6f, 1f, 0.5f, 0.9f);
            DrawLine(continuation);
        }

        private static void DrawLine(Vector2[] points)
        {
            if (points == null) return;
            for (int i = 1; i < points.Length; i++) Gizmos.DrawLine(points[i - 1], points[i]);
        }
    }
}
