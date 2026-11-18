using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE de las Amígdalas: EL ABSCESO (absceso periamigdalino). Biología: cuando una infección de garganta no se
    /// controla, se forma una bolsa cerrada llena de pus junto a la amígdala; adentro el medicamento casi no llega.
    ///  - Tus disparos no le hacen nada: solo le hacen daño los ANTICUERPOS de los linfocitos B.
    ///  - Llama estreptococos (sueltan antígenos: llévalos al centro germinal para fabricar más linfocitos).
    ///  - Escupe gotas en arco y late; con poca vida ataca más rápido.
    /// Al derrotarlo ganas el premio: un linfocito aliado que te acompaña en todos los niveles.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class AbscessBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "EL ABSCESO";
        [SerializeField, TextArea] private string introMessage = "NODO: ¡Un absceso! Es una bolsa cerrada: tus medicamentos no entran. Solo los ANTICUERPOS le hacen daño. Lleva antígenos al centro germinal.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Absceso drenado. PREMIO: un linfocito B te acompañará en todos los niveles.";
        [SerializeField] private string rewardSecret = "premio_linfocito";
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite idleSprite, attackSprite, hurtSprite, deadSprite;
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();
        private float spitTimer = 2f, summonTimer = 1.5f, hurtTimer, attackTimer, reminder = 14f, contactCooldown;
        private bool dead;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (!dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            foreach (EnemyHealth m in minions) if (m != null) Destroy(m.gameObject);
        }

        private void Start()
        {
            health.Invulnerable = true;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 8, 30);
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
        }

        /// <summary>Un anticuerpo lo alcanzó.</summary>
        public void AntibodyHit(float amount, Vector2 at)
        {
            if (dead) return;
            health.Invulnerable = false;
            health.ApplyDamage(DamageInfo.Create(amount, DamageKind.Projectile, Team.Player, at));
            health.Invulnerable = !dead;
            hurtTimer = 0.25f;
            reminder = 18f;
        }

        private void Update()
        {
            if (dead) { UpdateArt(); return; }
            float dt = Time.deltaTime;
            float rate = health.Health01 < 0.4f ? 1.5f : 1f;
            hurtTimer -= dt; attackTimer -= dt; reminder -= dt;
            if (contactCooldown > 0f) contactCooldown -= dt;
            PlayerController player = PlayerController.Current;

            spitTimer -= dt * rate;
            if (spitTimer <= 0f && player != null && player.Health.IsAlive)
            {
                spitTimer = 2.8f;
                attackTimer = 0.5f;
                for (int i = -1; i <= 1; i++) Spit(player, i);
            }

            summonTimer -= dt;
            if (summonTimer <= 0f)
            {
                summonTimer = 9f;
                minions.RemoveAll(m => m == null || !m.IsAlive);
                if (minions.Count < 3) Summon();
            }

            if (reminder <= 0f)
            {
                reminder = 18f;
                GameplayEvents.RaiseMessage("Recuerda: solo los ANTICUERPOS le hacen daño. Lleva antígenos al centro germinal.");
            }

            if (visual != null)
            {
                float beat = Mathf.Abs(Mathf.Sin(Time.time * 2.4f * rate));
                visual.localScale = new Vector3(1f + beat * 0.05f, 1f + beat * 0.08f, 1f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.9f) * 4f);
            }
            UpdateArt();
            TryContact(player);
        }

        private void UpdateArt()
        {
            if (art == null) return;
            Sprite s = dead ? deadSprite : hurtTimer > 0f ? hurtSprite : attackTimer > 0f ? attackSprite : idleSprite;
            if (s == null) s = idleSprite;
            if (s != null && art.sprite != s) art.sprite = s;
        }

        private void Spit(PlayerController player, int spread)
        {
            if (pool == null) return;
            Vector2 from = (Vector2)transform.position + new Vector2(0f, 3.2f);
            float dist = Mathf.Clamp(player.Position.x - from.x + spread * 2f, -12f, 12f);
            Vector2 dir = new Vector2(dist >= 0f ? 1f : -1f, 1.3f).normalized;
            float speed = Mathf.Clamp(Mathf.Sqrt(Mathf.Abs(dist) * 16f), 5f, 13f);
            Projectile p = pool.Get();
            p.Launch(new ProjectileShot
            {
                origin = from, direction = dir, speed = speed, range = 30f, damage = 8f, knockback = 3f, radius = 0.35f,
                sourceTeam = Team.Enemy, targetTeam = Team.Player, color = new Color(1f, 0.95f, 0.75f),
                groundMask = groundMask, gravity = 16f
            }, done => pool.Release(done));
        }

        private void Summon()
        {
            if (minionPrefab == null) return;
            Sfx.EffectAt("Colmena_Enjambre", transform.position, 0.8f);
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = transform.position + new Vector3(i == 0 ? -2.5f : 2.5f, 2f, 0f);
                EnemyHealth m = Instantiate(minionPrefab, pos, Quaternion.identity);
                if (minionData != null)
                {
                    m.Configure(minionData, null);
                    EnemyBrain brain = m.GetComponent<EnemyBrain>();
                    if (brain != null) brain.Configure(minionData);
                }
                minions.Add(m);
            }
        }

        private void TryContact(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            Vector2 center = (Vector2)transform.position + Vector2.up * 1.8f;
            if (box == null || !box.IsAlive || !box.OverlapsCircle(center, 1.9f)) return;
            DamageInfo info = DamageInfo.Create(12f, DamageKind.Contact, Team.Enemy, center);
            info.knockback = 8f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void OnDied(EnemyHealth _)
        {
            dead = true;
            foreach (EnemyHealth m in minions) if (m != null) m.Kill();
            minions.Clear();
            GameSession.FindSecret(rewardSecret);
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }
}
