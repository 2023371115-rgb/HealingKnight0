using HealingKnight.Gameplay.Audio;
using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Jefe volador de los Pulmones: la colmena de tuberculosis (estilo nave de Metal Slug).
    /// Flota arriba de la arena siguiendo al jugador y alterna ataques:
    ///  - Ráfaga: abre sus celdas (brillan como aviso) y dispara un abanico apuntado al jugador.
    ///  - Bombardeo: cruza la arena soltando esporas que caen en línea recta. Se esquivan moviéndote o con la burbuja.
    ///  - Enjambre (fase 2+): suelta bacterias que caen al piso y te persiguen.
    /// Fases por vida: más rápida y con más disparos. Se le pega desde el piso apuntando arriba/diagonal,
    /// o desde las plataformas de la arena.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class HiveBoss : MonoBehaviour
    {
        private enum State { Intro, Patrol, Telegraph, Spray, Bombing, Swarm, Dead }
        private enum Attack { Spray, Bombing, Swarm }

        [Header("Identidad")]
        [SerializeField] private string bossName = "COLMENA DE TUBERCULOSIS";
        [SerializeField, TextArea] private string introMessage = "NODO: ¡El foco! Una colmena de M. tuberculosis. Antibiótico, y no dejes de moverte.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Foco eliminado. Despeja el último bloqueo.";

        [Header("Partes")]
        [SerializeField] private Transform visual;
        [Tooltip("Celdas que brillan antes de disparar (el aviso).")]
        [SerializeField] private SpriteRenderer[] cellRenderers;
        [SerializeField] private Color cellIdleColor = new Color(0.25f, 0.1f, 0.15f);
        [SerializeField] private Color cellWarnColor = new Color(1f, 0.85f, 0.3f);

        [Header("Vuelo")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float bobAmplitude = 0.35f;
        [SerializeField] private float patrolTime = 1.8f;

        [Header("Disparos")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private float projectileDamage = 10f;
        [SerializeField] private Color projectileColor = new Color(0.95f, 0.8f, 0.35f);

        [Header("Ráfaga")]
        [SerializeField] private float telegraphTime = 0.7f;
        [SerializeField] private int sprayShots = 5;
        [SerializeField] private float spraySpreadDegrees = 50f;
        [SerializeField] private float spraySpeed = 7f;

        [Header("Bombardeo")]
        [SerializeField] private float bombingSpeed = 6f;
        [SerializeField] private float sporeInterval = 0.35f;
        [SerializeField] private float sporeSpeed = 5.5f;

        [Header("Enjambre (fase 2+)")]
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;
        [SerializeField] private int minionsPerSwarm = 2;
        [SerializeField] private int maxMinionsAlive = 4;

        [Header("Contacto")]
        [SerializeField] private float contactRadius = 1.4f;
        [SerializeField] private float contactDamage = 15f;

        [Header("Fases (por % de vida)")]
        [SerializeField] private float phase2At = 0.6f;
        [SerializeField] private float phase3At = 0.3f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();

        private State state = State.Intro;
        private Attack lastAttack = Attack.Swarm;
        private float timer;
        private float hoverY;
        private float bobPhase;
        private float bombingTargetX;
        private float sporeTimer;
        private int spraysLeft;
        private float contactCooldown;

        private int Phase => health.Health01 <= phase3At ? 3 : health.Health01 <= phase2At ? 2 : 1;
        private float Speed => Phase == 3 ? 1.4f : Phase == 2 ? 1.2f : 1f;
        private Vector2 Position => transform.position;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
        }

        private void Start()
        {
            hoverY = transform.position.y;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 12, 40);
            SetCells(false);

            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);

            // Entra volando desde arriba.
            transform.position = new Vector3(transform.position.x, hoverY + 4f, 0f);
            state = State.Intro;
            timer = 1.5f;
        }

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            bobPhase += dt * 2f;
            if (contactCooldown > 0f) contactCooldown -= dt;

            PlayerController player = PlayerController.Current;
            bool playerAlive = player != null && player.Health != null && player.Health.IsAlive;

            switch (state)
            {
                case State.Intro:
                    timer -= dt;
                    MoveTowards(new Vector2(transform.position.x, hoverY), 4f, dt);
                    if (timer <= 0f) BeginPatrol();
                    break;

                case State.Patrol:
                    timer -= dt;
                    if (playerAlive) MoveTowards(new Vector2(player.Position.x, hoverY), patrolSpeed * Speed, dt);
                    if (timer <= 0f && playerAlive) ChooseAttack();
                    break;

                case State.Telegraph:
                    timer -= dt;
                    Hover(dt);
                    if (timer <= 0f) { state = State.Spray; timer = 0f; }
                    break;

                case State.Spray:
                    timer -= dt;
                    Hover(dt);
                    if (timer <= 0f)
                    {
                        if (playerAlive) FireSpray(player);
                        spraysLeft--;
                        timer = 0.45f;
                        if (spraysLeft <= 0) { SetCells(false); BeginPatrol(); }
                    }
                    break;

                case State.Bombing:
                    UpdateBombing(dt);
                    break;

                case State.Swarm:
                    timer -= dt;
                    Hover(dt);
                    if (timer <= 0f) { SpawnMinions(); BeginPatrol(); }
                    break;
            }

            TryContactDamage(player);
            Tilt();
        }

        // ---------------------------------------------------------------- movimiento

        private void MoveTowards(Vector2 target, float speed, float dt)
        {
            Vector2 pos = Vector2.MoveTowards(Position, target, speed * dt);
            pos.x = Mathf.Clamp(pos.x, WalkableArea.MinX + 1.5f, WalkableArea.MaxX - 1.5f);
            pos.y += Mathf.Sin(bobPhase) * bobAmplitude * dt * 2f;
            transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        private void Hover(float dt)
        {
            Vector2 p = Position;
            p.y = Mathf.MoveTowards(p.y, hoverY + Mathf.Sin(bobPhase) * bobAmplitude, 2f * dt);
            transform.position = new Vector3(p.x, p.y, 0f);
        }

        /// <summary>Se inclina un poco hacia donde avanza (se siente como nave).</summary>
        private void Tilt()
        {
            if (visual == null) return;
            float target = state == State.Bombing ? Mathf.Sign(bombingTargetX - Position.x) * -10f : 0f;
            float z = Mathf.LerpAngle(visual.localEulerAngles.z, target, 5f * Time.deltaTime);
            visual.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        // ---------------------------------------------------------------- ataques

        private void BeginPatrol()
        {
            state = State.Patrol;
            timer = patrolTime / Speed;
        }

        private void ChooseAttack()
        {
            minions.RemoveAll(m => m == null || !m.IsAlive);
            bool canSwarm = Phase >= 2 && minionPrefab != null && minions.Count < maxMinionsAlive;

            Attack next;
            float r = Random.value;
            if (canSwarm && lastAttack != Attack.Swarm && r < 0.3f) next = Attack.Swarm;
            else next = lastAttack == Attack.Spray ? Attack.Bombing : (r < 0.55f ? Attack.Spray : Attack.Bombing);
            lastAttack = next;

            switch (next)
            {
                case Attack.Spray:
                    state = State.Telegraph;
                    timer = telegraphTime / Mathf.Lerp(1f, Speed, 0.5f);
                    spraysLeft = Phase == 3 ? 3 : Phase == 2 ? 2 : 1;
                    SetCells(true);
                    break;

                case Attack.Bombing:
                    state = State.Bombing;
                    // Cruza hacia el lado contrario de donde está.
                    float mid = (WalkableArea.MinX + WalkableArea.MaxX) * 0.5f;
                    bombingTargetX = Position.x < mid ? WalkableArea.MaxX - 1.5f : WalkableArea.MinX + 1.5f;
                    sporeTimer = 0.4f; // margen antes de la primera espora
                    SetCells(true);
                    break;

                case Attack.Swarm:
                    state = State.Swarm;
                    timer = 0.8f;
                    SetCells(true);
                    break;
            }
        }

        private void FireSpray(PlayerController player)
        {
            if (pool == null) return;
            Vector2 origin = Position + Vector2.down * 0.8f;
            Vector2 toPlayer = (player.Position + Vector2.up * 0.9f) - origin;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            int shots = Mathf.Max(1, sprayShots + (Phase - 1));
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), origin, CombatAudio.Effects);
            float spread = shots > 1 ? spraySpreadDegrees / (shots - 1) : 0f;
            for (int i = 0; i < shots; i++)
            {
                float a = (baseAngle - spraySpreadDegrees * 0.5f + spread * i) * Mathf.Deg2Rad;
                Shoot(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), spraySpeed * Mathf.Lerp(1f, Speed, 0.5f), 0.22f);
            }
        }

        private void UpdateBombing(float dt)
        {
            Vector2 target = new Vector2(bombingTargetX, hoverY + 0.6f);
            MoveTowards(target, bombingSpeed * Speed, dt);

            sporeTimer -= dt;
            if (sporeTimer <= 0f)
            {
                sporeTimer = sporeInterval / Speed;
                Shoot(Position + Vector2.down * 1f, Vector2.down, sporeSpeed, 0.3f);
            }

            if (Mathf.Abs(Position.x - bombingTargetX) < 0.2f)
            {
                SetCells(false);
                BeginPatrol();
            }
        }

        private void Shoot(Vector2 origin, Vector2 direction, float speed, float radius)
        {
            if (pool == null) return;
            Projectile p = pool.Get();
            p.Launch(new ProjectileShot
            {
                origin = origin,
                direction = direction,
                speed = speed,
                range = 18f,
                damage = projectileDamage,
                knockback = 4f,
                radius = radius,
                sourceTeam = Team.Enemy,
                targetTeam = Team.Player,
                medication = null,
                color = projectileColor,
                groundMask = groundMask
            }, finished => pool.Release(finished));
        }

        private void SpawnMinions()
        {
            SetCells(false);
            if (minionPrefab == null) return;
            Sfx.EffectAt("Colmena_Enjambre", Position);
            minions.RemoveAll(m => m == null || !m.IsAlive);
            int toSpawn = Mathf.Min(minionsPerSwarm, maxMinionsAlive - minions.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 pos = new Vector3(Position.x + (i - (toSpawn - 1) * 0.5f) * 1.2f, Position.y - 1.2f, 0f);
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

        private void TryContactDamage(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(Position, contactRadius)) return;

            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, Position);
            info.knockback = 7f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void SetCells(bool warning)
        {
            if (cellRenderers == null) return;
            foreach (SpriteRenderer r in cellRenderers)
                if (r != null) r.color = warning ? cellWarnColor : cellIdleColor;
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            SetCells(false);
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
            minions.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }
}
