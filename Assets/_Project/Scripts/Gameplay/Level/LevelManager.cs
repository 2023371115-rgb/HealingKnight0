using System.Collections;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Dirige un nivel: límites del piso, aparición del jugador, arenas, muerte y reaparición, y la salida.
    /// Al completar el nivel lo marca en el guardado y regresa al mapa del cuerpo.
    /// Si la escena no está en el catálogo (escenas Sandbox), al terminar simplemente se reinicia.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Límites del nivel (X = largo; yMin = caída al vacío)")]
        [SerializeField] private Rect levelBounds = new Rect(-10f, -6f, 60f, 20f);

        [Header("Jugador")]
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private Vector2 spawnPoint = new Vector2(-7f, 0.1f);
        [SerializeField] private float respawnDelay = 1.5f;

        [Header("Salida")]
        [Tooltip("Al cruzar esta X (sin arenas pendientes) se completa el nivel.")]
        [SerializeField] private float exitX = 48f;
        [SerializeField] private float completeDelay = 2.5f;

        [Header("Medicamento del nivel")]
        [Tooltip("Un medicamento que solo se da en este nivel (ej. ANTIPARASITARIO en el Corazón, para el T. cruzi).")]
        [SerializeField] private MedicationData levelMedication;
        [Tooltip("El genérico que deja su lugar (si no lo traes, el del nivel se agrega al final).")]
        [SerializeField] private MedicationData replacesMedication;

        [Header("Mensajes")]
        [SerializeField, TextArea] private string introMessage;
        [SerializeField] private string completeMessage = "¡ZONA DESINFECTADA!";

        private readonly List<ArenaZone> arenas = new List<ArenaZone>();
        private PlayerController player;
        private float checkpointX;
        private bool completed;

        public static LevelManager Current { get; private set; }
        public Rect LevelBounds => levelBounds;
        public float ExitX => exitX;
        public MedicationData LevelMedication => levelMedication;
        public MedicationData ReplacesMedication => replacesMedication;

        /// <summary>
        /// Cosas que impiden terminar el nivel aunque llegues a la salida (la persecución de la lombriz hasta que huye).
        /// Se agregan con Block y se quitan con Unblock.
        /// </summary>
        public static readonly System.Collections.Generic.HashSet<Object> ExitBlockers = new System.Collections.Generic.HashSet<Object>();
        public static void Block(Object who) { if (who != null) ExitBlockers.Add(who); }
        public static void Unblock(Object who) => ExitBlockers.Remove(who);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBlockers() => ExitBlockers.Clear();

        private void Awake()
        {
            Current = this;
            WalkableArea.SetLevelBounds(levelBounds);
            GameSession.RegisterActiveLevel(SceneManager.GetActiveScene().name);
            #pragma warning disable CS0618 // La API cambia entre versiones de Unity 6; esta sigue funcionando.
            arenas.AddRange(FindObjectsByType<ArenaZone>(FindObjectsSortMode.None));
#pragma warning restore CS0618
            arenas.Sort((a, b) => a.triggerX.CompareTo(b.triggerX));
            checkpointX = spawnPoint.x;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (player != null && player.Health != null) player.Health.Died -= OnPlayerDied;
            foreach (ArenaZone a in arenas)
                if (a != null) a.Cleared -= OnArenaCleared;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null && playerPrefab != null)
                player = Instantiate(playerPrefab, new Vector3(spawnPoint.x, spawnPoint.y, 0f), Quaternion.identity);

            if (player == null)
            {
                Debug.LogError("[LevelManager] No hay jugador en la escena ni prefab asignado.", this);
                return;
            }

            player.Motor.Teleport(spawnPoint);
            if (levelMedication != null && player.Shooter != null) player.Shooter.BuildLoadout();
            player.Health.Died += OnPlayerDied;
            foreach (ArenaZone a in arenas) a.Cleared += OnArenaCleared;

            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
        }

        private float lastSafeY = float.NaN;

        private void Update()
        {
            // Recuerda a qué altura estabas parado por última vez: al reaparecer se busca el piso más parecido (y no,
            // por ejemplo, la parte de ARRIBA de la encía del techo en la Boca).
            if (player != null && player.Health.IsAlive && player.Motor.IsGrounded) lastSafeY = player.Position.y;
            if (completed || player == null || !player.Health.IsAlive) return;
            if (player.Position.x < exitX) return;
            if (ExitBlockers.Count > 0) return;   // algo del nivel todavía no termina (ej. la lombriz no ha huido)

            foreach (ArenaZone a in arenas)
            {
                if (a.triggerX < exitX && !a.IsCleared) return; // aún quedan arenas por limpiar
            }

            StartCoroutine(CompleteLevel());
        }

        /// <summary>Punto de reaparición nuevo (por ejemplo, la placa de Peyer). Solo avanza, nunca regresa.</summary>
        public void SetCheckpoint(float x) => checkpointX = Mathf.Max(checkpointX, x);

        private void OnArenaCleared(ArenaZone arena)
        {
            checkpointX = Mathf.Max(checkpointX, arena.rightBound - 2f);
        }

        private void OnPlayerDied()
        {
            GameplayEvents.RaisePlayerDefeated();
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            float respawnX = checkpointX;
            foreach (ArenaZone a in arenas)
            {
                if (a.IsRunning)
                {
                    respawnX = a.RespawnX;
                    a.ResetArena();
                }
            }

            // Reaparece cayendo desde arriba, un poco antes de la arena para que esta vuelva a activarse al avanzar.
            Vector2 at = RespawnPoint(respawnX - 1f);
            player.Respawn(at);
        }

        /// <summary>
        /// Dónde reaparecer cerca de esa X: un poco arriba de un piso sólido (no de una plataforma de un solo sentido)
        /// que tenga espacio libre encima, el MÁS PARECIDO a la altura donde estabas parado antes de morir. Así no
        /// apareces encima de un techo (la encía de la Boca) y funciona en partes altas (el colon transverso). Si ahí
        /// no hay piso razonable (un hueco), busca un poco más atrás.
        /// </summary>
        private Vector2 RespawnPoint(float x)
        {
            float reference = float.IsNaN(lastSafeY) ? spawnPoint.y : lastSafeY;
            for (int step = 0; step <= 12; step++)
            {
                float tryX = x - step * 0.75f;
                if (FloorNear(tryX, reference, out float y) && y < reference + 4f) return new Vector2(tryX, y + 2.5f);
            }
            return new Vector2(x, FloorNear(x, reference, out float any) ? any + 2.5f : spawnPoint.y + 3f);
        }

        private bool FloorNear(float x, float reference, out float floorY)
        {
            floorY = 0f;
            bool found = false;
            float best = float.MaxValue;
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, levelBounds.yMax + 30f), Vector2.down, levelBounds.height + 60f, 1 << PhysicsLayers.Ground);
            foreach (RaycastHit2D h in hits)
            {
                if (h.collider == null || h.collider.usedByEffector || h.point.y < levelBounds.yMin) continue;
                // Tiene que caber el jugador encima (si no, es la parte de abajo de algo).
                if (Physics2D.OverlapBox(h.point + new Vector2(0f, 1.3f), new Vector2(0.6f, 2.2f), 0f, 1 << PhysicsLayers.Ground) != null) continue;
                float d = Mathf.Abs(h.point.y - reference);
                if (d < best) { best = d; floorY = h.point.y; found = true; }
            }
            return found;
        }

        private IEnumerator CompleteLevel()
        {
            completed = true;
            player.Input.Blocked = true;
            player.Motor.SetHorizontalVelocity(0f);
            GameplayEvents.RaiseLevelCompleted();
            if (!string.IsNullOrEmpty(completeMessage)) GameplayEvents.RaiseMessage(completeMessage);

            yield return new WaitForSeconds(completeDelay);

            if (GameSession.CurrentLevel != null)
            {
                GameSession.CompleteCurrentLevel();
                GameSession.GoToBodyMap();
            }
            else
            {
                GameSession.ReloadCurrentScene();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
            Vector3 c = new Vector3(levelBounds.center.x, levelBounds.center.y, 0f);
            Gizmos.DrawWireCube(c, new Vector3(levelBounds.width, levelBounds.height, 0f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(spawnPoint.x, spawnPoint.y, 0f), 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(exitX, levelBounds.yMin, 0f), new Vector3(exitX, levelBounds.yMin + 10f, 0f));
        }
    }
}
