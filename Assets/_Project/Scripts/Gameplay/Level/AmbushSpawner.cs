using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// EMBOSCADA: enemigos que aparecen cuando el jugador llega a cierto punto (fuera de las arenas).
    ///  - Modo "escondido": se ve una figurita pegada (una Giardia agarrada a la vellosidad) que tiembla y se te deja caer.
    ///  - Modo "adelante": aparecen adelante del jugador (por ejemplo, rotavirus que llegan volando mientras vas en la balsa).
    /// Solo sale una vez; si el jugador muere y regresa, se vuelve a armar.
    /// </summary>
    public class AmbushSpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth prefab;
        [SerializeField] private EnemyData data;
        [SerializeField] private int count = 1;
        [Tooltip("Se activa cuando el jugador pasa esta distancia en X (antes o después del objeto).")]
        [SerializeField] private float triggerRange = 1.6f;
        [Tooltip("Adelante: aparece a esta distancia frente al jugador (0 = en este objeto).")]
        [SerializeField] private float aheadOfPlayer = 0f;
        [SerializeField] private Vector2 launchVelocity = new Vector2(0f, -2f);
        [SerializeField] private float warning = 0.5f;
        [SerializeField, TextArea] private string message;
        [Tooltip("Figurita que se ve antes de que salga (opcional).")]
        [SerializeField] private Transform telltale;

        private bool triggered, spawned;
        private float timer;
        private Vector3 telltaleBase;
        private EnemyHealth[] spawnedEnemies;

        public void Configure(EnemyHealth enemyPrefab, EnemyData enemyData, int enemyCount, float range, float ahead, Vector2 velocity, Transform figure, string text)
        {
            prefab = enemyPrefab;
            data = enemyData;
            count = enemyCount;
            triggerRange = range;
            aheadOfPlayer = ahead;
            launchVelocity = velocity;
            telltale = figure;
            message = text;
        }

        private void OnEnable() => GameplayEvents.PlayerDefeated += Rearm;
        private void OnDisable() => GameplayEvents.PlayerDefeated -= Rearm;

        private void Start()
        {
            if (telltale != null) telltaleBase = telltale.localPosition;
        }

        /// <summary>Si moriste antes de matarlos, la emboscada se vuelve a armar (y los que quedaban se van).</summary>
        private void Rearm()
        {
            if (!spawned) { triggered = false; return; }
            bool anyAlive = false;
            if (spawnedEnemies != null)
                foreach (EnemyHealth e in spawnedEnemies)
                    if (e != null && e.IsAlive) { anyAlive = true; Destroy(e.gameObject); }
            if (!anyAlive) return;   // ya los habías matado: no regresan
            triggered = spawned = false;
            if (telltale != null) telltale.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (spawned) return;
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;

            if (!triggered)
            {
                if (telltale != null) telltale.localPosition = telltaleBase + new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.05f, 0f);
                if (Mathf.Abs(player.Position.x - transform.position.x) > triggerRange) return;
                triggered = true;
                timer = warning;
                Sfx.EffectAt("Lombriz_Sale", transform.position, 0.5f);
                return;
            }

            timer -= Time.deltaTime;
            if (telltale != null) telltale.localPosition = telltaleBase + new Vector3(Mathf.Sin(Time.time * 50f) * 0.06f, 0f, 0f);
            if (timer > 0f) return;
            Spawn(player);
        }

        private void Spawn(PlayerController player)
        {
            spawned = true;
            if (telltale != null) telltale.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
            if (prefab == null) return;

            spawnedEnemies = new EnemyHealth[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = transform.position;
                if (aheadOfPlayer != 0f) pos.x = player.Position.x + aheadOfPlayer;
                pos.x += (i - (count - 1) * 0.5f) * 1.2f;
                pos.y += i * 0.6f;
                EnemyHealth e = Instantiate(prefab, pos, Quaternion.identity);
                if (data != null) e.Configure(data, null);
                EnemyBrain brain = e.GetComponent<EnemyBrain>();
                if (brain != null && data != null) brain.Configure(data);
                CharacterMotor2D motor = e.GetComponent<CharacterMotor2D>();
                if (motor != null) motor.SetVelocity(launchVelocity);
                ArenaZone.Adopt(e);
                spawnedEnemies[i] = e;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(triggerRange * 2f, 0.4f, 0f));
        }
    }
}
