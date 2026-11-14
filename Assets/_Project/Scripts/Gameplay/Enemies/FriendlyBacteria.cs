using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// FLORA INTESTINAL (bacterias buenas, como Lactobacillus y Bifidobacterium): viven en el colon, ayudan a digerir
    /// y no dejan espacio a las bacterias malas. NO ATACAN: caminan de un lado a otro.
    /// Si les disparas ANTIBIÓTICO se mueren... y en su lugar sale C. difficile (así pasa de verdad: los antibióticos
    /// matan la flora buena y C. difficile aprovecha el hueco). Los otros medicamentos no les hacen nada.
    /// Con el PROBIÓTICO (ProbioticPickup) se ponen bravas unos segundos y atacan a los enemigos cercanos.
    /// Tiene EnemyHealth solo para que las balas le peguen; no cuenta para las arenas.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class FriendlyBacteria : MonoBehaviour
    {
        public static readonly List<FriendlyBacteria> All = new List<FriendlyBacteria>();
        private static float rallyUntil;
        private static bool warned;

        [SerializeField] private MedicationData killer;          // el antibiótico
        [SerializeField] private EnemyHealth replacementPrefab;   // C. difficile
        [SerializeField] private EnemyData replacementData;
        [SerializeField] private float wander = 2.5f;
        [SerializeField] private float speed = 0.8f;
        [SerializeField] private float rallySpeed = 3.6f;
        [SerializeField] private float rallyRange = 8f;
        [SerializeField] private float rallyDamage = 6f;
        [SerializeField] private Transform visual;
        [Tooltip("Dibujos de distintas bacterias buenas; cada una escoge uno según dónde está (así no se ven todas iguales).")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite[] variants;
        [SerializeField, TextArea] private string killMessage = "¡Mataste flora buena con el antibiótico! En su lugar creció C. difficile. Apunta con cuidado.";

        private EnemyHealth health;
        private Vector3 home;
        private float dir = 1f, hitTimer, seed;
        private bool killedByAntibiotic;

        public static bool Rallying => Time.time < rallyUntil;

        /// <summary>El probiótico: toda la flora ataca a los enemigos durante unos segundos.</summary>
        public static void Rally(float seconds) => rallyUntil = Mathf.Max(rallyUntil, Time.time + seconds);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            All.Clear();
            rallyUntil = 0f;
            warned = false;
        }

        public void Configure(MedicationData antibiotic, EnemyHealth cdiffPrefab, EnemyData cdiffData)
        {
            killer = antibiotic;
            replacementPrefab = cdiffPrefab;
            replacementData = cdiffData;
        }

        private void Awake() => health = GetComponent<EnemyHealth>();

        private void OnEnable()
        {
            All.Add(this);
            health.Hit += OnHit;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            All.Remove(this);
            health.Hit -= OnHit;
            health.Died -= OnDied;
        }

        private void Start()
        {
            home = transform.position;
            seed = home.x * 1.37f;
            dir = Mathf.Sin(seed) >= 0f ? 1f : -1f;
            if (body != null && variants != null && variants.Length > 1)
            {
                int i = Mathf.Abs(Mathf.RoundToInt(home.x * 7.3f)) % variants.Length;
                if (variants[i] != null) body.sprite = variants[i];
            }
        }

        private void OnHit(DamageInfo info, HitResult result)
        {
            if (killer != null && info.medication == killer) killedByAntibiotic = true;
        }

        private void OnDied(EnemyHealth _)
        {
            All.Remove(this);
            if (!killedByAntibiotic || replacementPrefab == null) return;
            EnemyHealth bad = Instantiate(replacementPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
            if (replacementData != null) bad.Configure(replacementData, null);
            EnemyBrain brain = bad.GetComponent<EnemyBrain>();
            if (brain != null && replacementData != null) brain.Configure(replacementData);
            ArenaZone.Adopt(bad);
            Sfx.EffectAt("Colmena_Enjambre", transform.position, 0.6f);
            if (!warned) { warned = true; GameplayEvents.RaiseMessage(killMessage); }
        }

        private void Update()
        {
            if (!health.IsAlive) return;
            float dt = Time.deltaTime;
            Vector3 p = transform.position;
            EnemyHealth target = Rallying ? NearestEnemy(p) : null;

            float vx;
            if (target != null)
            {
                float dx = target.transform.position.x - p.x;
                vx = Mathf.Clamp(dx * 3f, -rallySpeed, rallySpeed);
                hitTimer -= dt;
                if (Mathf.Abs(dx) < 1f && Mathf.Abs(target.transform.position.y - p.y) < 1.5f && hitTimer <= 0f)
                {
                    hitTimer = 0.5f;
                    DamageInfo info = DamageInfo.Create(rallyDamage, DamageKind.Contact, Team.Player, p);
                    info.knockback = 3f;
                    target.ApplyDamage(info);
                }
            }
            else
            {
                // Pasea despacio alrededor de su lugar (o regresa si se alejó persiguiendo).
                if (Mathf.Abs(p.x - home.x) > wander + 0.5f) dir = home.x > p.x ? 1f : -1f;
                else if (p.x > home.x + wander) dir = -1f;
                else if (p.x < home.x - wander) dir = 1f;
                vx = dir * speed;
            }
            float nx = Mathf.Clamp(p.x + vx * dt, WalkableArea.MinX + 0.5f, WalkableArea.MaxX - 0.5f);
            transform.position = new Vector3(nx, home.y, 0f);

            if (visual != null)
            {
                float t = Time.time * (Rallying ? 9f : 4f) + seed;
                visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t)) * 0.12f, 0f);
                float face = Mathf.Abs(vx) > 0.05f ? Mathf.Sign(vx) : Mathf.Sign(visual.localScale.x == 0f ? 1f : visual.localScale.x);
                visual.localScale = new Vector3(Mathf.Abs(visual.localScale.x) * face, visual.localScale.y, 1f);
            }
        }

        private EnemyHealth NearestEnemy(Vector3 p)
        {
            EnemyHealth best = null;
            float bestD = rallyRange;
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive || e == health || e.GetComponent<FriendlyBacteria>() != null) continue;
                float d = Vector2.Distance(e.transform.position, p);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }
    }
}
