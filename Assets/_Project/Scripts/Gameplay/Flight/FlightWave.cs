using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>Cómo se mueve un enemigo en vuelo (todo es relativo a la pantalla, que va avanzando).</summary>
    public enum FlightPattern
    {
        /// <summary>Cruza la pantalla de derecha a izquierda (con 'amplitude' hace ondas).</summary>
        Line,
        /// <summary>Entra, se acerca a tu altura y sigue derecho (picada).</summary>
        Swoop,
        /// <summary>Entra hasta 'hoverX', se queda 'hoverTime' disparando y se va.</summary>
        Hover,
        /// <summary>Te persigue despacio un rato y luego se va.</summary>
        Chase
    }

    /// <summary>Por dónde entra la oleada.</summary>
    public enum FlightEdge { Right, Top, Bottom }

    /// <summary>Todo lo que define una oleada de vuelo (lo llena SceneSetup).</summary>
    [System.Serializable]
    public class FlightWaveSpec
    {
        public EnemyHealth prefab;
        public int count = 3;
        [Tooltip("Segundos entre un enemigo y el siguiente.")]
        public float interval = 0.6f;
        public FlightPattern pattern = FlightPattern.Line;
        public FlightEdge edge = FlightEdge.Right;
        [Tooltip("Dónde entra el primero (respecto al centro de la pantalla): Y si entra por la derecha, X si entra por arriba/abajo.")]
        public float entry;
        [Tooltip("Cuánto se recorre la entrada de cada uno (formación).")]
        public float entryStep;
        [Tooltip("Si no es 0 y entra por arriba/abajo: sale de esta X del mundo (la boca de una rama), no de un lugar de la pantalla.")]
        public float worldX;
        [Tooltip("Velocidad respecto a la pantalla.")]
        public float speed = 3f;
        public float amplitude;
        public float frequency = 0.5f;
        [Tooltip("Velocidad vertical constante (las crías que salen disparadas).")]
        public float drift;
        public float hoverX = 4f;
        public float hoverTime = 4f;
        [Tooltip("Dispara cada tantos latidos (0 = no dispara).")]
        public int fireEveryBeats;
        public bool aimed = true;
        public int shots = 1;
        public float spreadDegrees = 20f;
        public float shotSpeed = 6f;
        public Projectile projectilePrefab;
        [Tooltip("Al morir se separa en estas crías (S. aureus: racimo → cocos).")]
        public EnemyHealth splitPrefab;
        public int splitCount;
        [TextArea] public string message;

        public FlightWaveSpec Copy() => (FlightWaveSpec)MemberwiseClone();
    }

    /// <summary>
    /// Oleada de enemigos de un nivel de vuelo: cuando la orilla derecha de la pantalla llega a su X, van saliendo uno
    /// por uno (desde la derecha, arriba o abajo) con su patrón de vuelo. Al reaparecer se vuelve a armar si quedó
    /// adelante del punto de guardado.
    /// Los enemigos son los mismos prefabs de siempre (su vida, debilidades y dibujo), pero los mueve FlightEnemy.
    /// </summary>
    public class FlightWave : MonoBehaviour
    {
        [SerializeField] private FlightWaveSpec spec = new FlightWaveSpec();

        private static readonly List<FlightWave> all = new List<FlightWave>();
        private bool triggered, told;
        private int spawned;
        private float timer, holdUntil;
        private ComponentPool<Projectile> pool;

        public FlightWaveSpec Spec => spec;
        public float TriggerX => transform.position.x;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        public void Configure(FlightWaveSpec waveSpec) => spec = waveSpec;

        private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        private void OnDisable() => all.Remove(this);
        private void OnDestroy() => pool?.Clear();

        /// <summary>Al reaparecer: se rearman las oleadas que quedaron adelante; las que ya se ven salen escalonadas.</summary>
        public static void RearmFrom(float viewMinX, float viewMaxX)
        {
            var ahead = new List<FlightWave>();
            foreach (FlightWave w in all)
                if (w != null && w.TriggerX > viewMinX) ahead.Add(w);
            ahead.Sort((a, b) => a.TriggerX.CompareTo(b.TriggerX));
            int visible = 0;
            foreach (FlightWave w in ahead)
            {
                w.triggered = false;
                w.spawned = 0;
                w.timer = 0f;
                w.holdUntil = Time.time + (w.TriggerX < viewMaxX ? 1.2f + 2.2f * visible++ : 0.8f);
            }
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null || spec.prefab == null || spawned >= spec.count) return;
            if (!triggered)
            {
                if (route.View.xMax < TriggerX || Time.time < holdUntil || !route.Launched) return;
                triggered = true;
                timer = 0f;
                if (!told && !string.IsNullOrEmpty(spec.message)) { told = true; GameplayEvents.RaiseMessage(spec.message); }
            }
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = spec.interval;
            Spawn(route, spawned);
            spawned++;
        }

        private void Spawn(FlightRoute route, int index)
        {
            float hw = route.HalfWidth, hh = route.HalfHeight;
            float along = spec.entry + spec.entryStep * index;
            if (spec.worldX != 0f && spec.edge != FlightEdge.Right) along = spec.worldX - route.CameraPosition.x + spec.entryStep * index;
            Vector2 screen;
            switch (spec.edge)
            {
                case FlightEdge.Top: screen = new Vector2(along, hh + 1.2f); break;
                case FlightEdge.Bottom: screen = new Vector2(along, -hh - 1.2f); break;
                default: screen = new Vector2(hw + 1.4f, along); break;
            }
            SpawnAt(spec, screen, Random.value * 10f);
        }

        /// <summary>Hace nacer un enemigo de esta oleada en una posición de la pantalla (también lo usan las crías).</summary>
        public FlightEnemy SpawnAt(FlightWaveSpec s, Vector2 screen, float seed)
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null || s.prefab == null) return null;
            Vector2 world = route.CameraPosition + screen;
            EnemyHealth e = Instantiate(s.prefab, new Vector3(world.x, world.y, 0f), Quaternion.identity);
            FlightEnemy fe = e.gameObject.AddComponent<FlightEnemy>();
            fe.Begin(this, s, screen, seed);
            return fe;
        }

        /// <summary>Bala enemiga (de la reserva de esta oleada).</summary>
        public void Fire(Vector2 origin, Vector2 direction, float speed, float damage, Color color)
        {
            if (spec.projectilePrefab == null) return;
            if (pool == null) pool = new ComponentPool<Projectile>(spec.projectilePrefab, 4, 24);
            Projectile p = pool.Get();
            p.Launch(new ProjectileShot
            {
                origin = origin,
                direction = direction.normalized,
                speed = speed,
                range = 30f,
                damage = damage,
                knockback = 2f,
                radius = 0.22f,
                sourceTeam = Team.Enemy,
                targetTeam = Team.Player,
                medication = null,
                color = color,
                groundMask = 1 << Movement.PhysicsLayers.Ground
            }, finished => pool.Release(finished));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.8f);
            Gizmos.DrawLine(transform.position + Vector3.up * 6f, transform.position + Vector3.down * 6f);
        }
    }
}
