using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE de la tráquea: la REINA DE LA TOS FERINA (Bordetella pertussis gigante, con corona).
    /// Biología: su toxina paraliza los cilios y provoca ataques de tos en ráfaga ("tos de perro" y el "gallo" al
    /// tomar aire). Patrón:
    ///  - Camina hacia ti despacio.
    ///  - TOS EN RÁFAGA: se infla (aviso, 1 s) y tose 3 veces; cada tos manda una ONDA por el piso hacia los dos
    ///    lados (sáltala) y un soplido que te empuja si estás cerca y en el suelo.
    ///  - Fase 2 (60 %): además llama a 2 bacterias de tos ferina.
    ///  - Fase 3 (30 %): cada tos también escupe gotitas de moco en arco, y tose 4 veces.
    /// Se trata con antibiótico. La vida la maneja EnemyHealth.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class PertussisQueenBoss : MonoBehaviour
    {
        private enum State { Intro, Walk, Inhale, Cough, Summon, Dead }

        [SerializeField] private string bossName = "REINA DE LA TOS FERINA";
        [SerializeField, TextArea] private string introMessage = "NODO: ¡La reina de la tos ferina! Cuando se infle, prepárate: sus toses mandan ondas por el piso. SALTA.";
        [SerializeField, TextArea] private string phase2Message = "NODO: Está llamando a más bacterias. Antibiótico a todas.";
        [SerializeField, TextArea] private string phase3Message = "NODO: ¡Ataque de tos! Ahora también escupe moco.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Reina eliminada. Los cilios vuelven a moverse: el paso a los pulmones está libre.";

        [SerializeField] private Transform visual;
        [SerializeField] private Transform mouth;
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float waveSpeed = 9f;
        [SerializeField] private float waveDamage = 12f;
        [SerializeField] private float pushRange = 5f;
        [SerializeField] private float contactDamage = 14f;
        [SerializeField] private float contactRadius = 1.6f;
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private Color waveColor = new Color(0.75f, 0.92f, 1f, 0.85f);

        [Header("Dibujo (opcional): poses")]
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite idleSprite, inflateSprite, coughSprite, hurtSprite, deadSprite;
        private float hurtTimer;

        private class CoughWave { public Transform root; public float x; public int dir; public bool hit, alive; }

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private readonly List<CoughWave> waves = new List<CoughWave>();
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();
        private State state = State.Intro;
        private float timer, contactCooldown, floorY;
        private int coughsLeft, lastPhase = 1, facing = -1, cycles;

        private int Phase => health.Health01 <= 0.3f ? 3 : health.Health01 <= 0.6f ? 2 : 1;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() { health.Died += OnDied; health.Hit += OnHit; }
        private void OnDisable() { health.Died -= OnDied; health.Hit -= OnHit; }

        private void OnHit(DamageInfo info, HitResult result) => hurtTimer = 0.25f;

        private void UpdateArt()
        {
            if (art == null) return;
            hurtTimer -= Time.deltaTime;
            Sprite s = state == State.Dead ? deadSprite
                : hurtTimer > 0f ? hurtSprite
                : state == State.Inhale || state == State.Summon ? inflateSprite
                : state == State.Cough && timer > 0.35f ? coughSprite
                : idleSprite;
            if (s == null) s = idleSprite;
            if (s != null && art.sprite != s) art.sprite = s;
        }

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            foreach (EnemyHealth m in minions) if (m != null) Destroy(m.gameObject);
            foreach (CoughWave w in waves) if (w.root != null) Destroy(w.root.gameObject);
        }

        private void Start()
        {
            floorY = transform.position.y;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 8, 30);
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
            timer = 1.4f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateWaves(dt);
            UpdateArt();
            if (state == State.Dead) return;
            if (contactCooldown > 0f) contactCooldown -= dt;

            int phase = Phase;
            if (phase != lastPhase)
            {
                lastPhase = phase;
                string msg = phase == 2 ? phase2Message : phase == 3 ? phase3Message : null;
                if (!string.IsNullOrEmpty(msg)) GameplayEvents.RaiseMessage(msg);
            }

            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health.IsAlive;
            if (alive) facing = player.Position.x < transform.position.x ? -1 : 1;
            float inflate = 0f;

            timer -= dt;
            switch (state)
            {
                case State.Intro:
                    if (timer <= 0f) BeginWalk();
                    break;

                case State.Walk:
                    if (alive && Mathf.Abs(player.Position.x - transform.position.x) > 2.5f)
                        MoveX(facing * walkSpeed * (phase == 3 ? 1.4f : 1f) * dt);
                    if (timer <= 0f)
                    {
                        cycles++;
                        minions.RemoveAll(m => m == null || !m.IsAlive);
                        if (phase >= 2 && cycles % 3 == 0 && minions.Count < 3) { state = State.Summon; timer = 0.8f; }
                        else { state = State.Inhale; timer = 1f; Sfx.EffectAt("Tos_Inhala", transform.position); }
                    }
                    break;

                case State.Inhale:
                    inflate = 1f - Mathf.Clamp01(timer);
                    if (timer <= 0f) { state = State.Cough; coughsLeft = phase == 3 ? 4 : 3; timer = 0f; }
                    break;

                case State.Cough:
                    inflate = 0.6f;
                    if (timer <= 0f)
                    {
                        CoughOnce(player, phase);
                        coughsLeft--;
                        timer = 0.75f;
                        if (coughsLeft <= 0) BeginWalk();
                    }
                    break;

                case State.Summon:
                    inflate = Mathf.Sin(timer * 20f) * 0.2f;
                    if (timer <= 0f) { Summon(); BeginWalk(); }
                    break;
            }

            if (visual != null)
            {
                float bob = state == State.Walk ? Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.08f : 0f;
                float cough = state == State.Cough ? Mathf.Clamp01(timer / 0.75f) : 0f;
                float amount = art != null ? 0.3f : 1f;
                visual.localScale = new Vector3((1f + (inflate * 0.25f - cough * 0.12f) * amount) * -facing, 1f + (inflate * 0.3f - cough * 0.1f) * amount + bob, 1f);
            }
            if (mouth != null) mouth.localScale = new Vector3(1f, state == State.Cough ? 1f + Mathf.Clamp01(timer / 0.75f) * 1.5f : 0.4f + inflate * 0.4f, 1f);
            TryContact(player);
        }

        private void BeginWalk()
        {
            state = State.Walk;
            timer = Random.Range(1.6f, 2.4f);
        }

        private void MoveX(float dx)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, WalkableArea.MinX + 1.8f, WalkableArea.MaxX - 1.8f);
            transform.position = p;
        }

        private void CoughOnce(PlayerController player, int phase)
        {
            Sfx.EffectAt("Tos_Fuerte", transform.position, 1f);
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.2f, 0.2f);
            SpawnWave(-1);
            SpawnWave(1);

            // Soplido: si estás cerca y en el suelo, te avienta.
            if (player != null && player.Health.IsAlive && player.Motor.IsGrounded)
            {
                float dx = player.Position.x - transform.position.x;
                if (Mathf.Abs(dx) < pushRange) player.Motor.SetVelocity(new Vector2(Mathf.Sign(dx) * 11f, 3f));
            }

            if (phase == 3 && pool != null && player != null)
                for (int i = 0; i < 2; i++) Lob(player, i);
        }

        private void Lob(PlayerController player, int i)
        {
            Vector2 from = (Vector2)transform.position + new Vector2(facing * 1.2f, 1.8f);
            float dist = Mathf.Clamp(player.Position.x - from.x + (i == 0 ? -1.5f : 1.5f), -10f, 10f);
            Vector2 dir = new Vector2(dist >= 0f ? 1f : -1f, 1.2f).normalized;
            float speed = Mathf.Sqrt(Mathf.Abs(dist) * 16f / Mathf.Max(0.1f, Mathf.Sin(2f * Mathf.Atan2(dir.y, Mathf.Abs(dir.x)))));
            Projectile p = pool.Get();
            p.Launch(new ProjectileShot
            {
                origin = from, direction = dir, speed = Mathf.Clamp(speed, 5f, 14f), range = 30f, damage = 8f, knockback = 3f,
                radius = 0.35f, sourceTeam = Team.Enemy, targetTeam = Team.Player, color = new Color(0.75f, 0.9f, 0.45f),
                groundMask = groundMask, gravity = 16f, slowMultiplier = 0.6f, slowDuration = 1.2f
            }, done => pool.Release(done));
        }

        private void Summon()
        {
            if (minionPrefab == null) return;
            Sfx.EffectAt("Colmena_Enjambre", transform.position);
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = new Vector3(transform.position.x + (i == 0 ? -2f : 2f), floorY + 3f, 0f);
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

        // ---------- ondas de tos ----------

        private void SpawnWave(int dir)
        {
            CoughWave w = waves.Find(x => !x.alive);
            if (w == null)
            {
                w = new CoughWave { root = new GameObject("OndaTos").transform };
                Sprite c = CodeSprites.Circle;
                CodeSprites.Add(w.root, c, new Color(0.1f, 0.15f, 0.35f, 0.9f), 40, new Vector2(0f, 0.45f), new Vector2(0.9f, 1.05f));
                CodeSprites.Add(w.root, c, waveColor, 41, new Vector2(0f, 0.45f), new Vector2(0.7f, 0.85f));
                CodeSprites.Add(w.root, c, new Color(1f, 1f, 1f, 0.8f), 42, new Vector2(0f, 0.6f), new Vector2(0.25f, 0.35f));
                waves.Add(w);
            }
            w.alive = true;
            w.hit = false;
            w.dir = dir;
            w.x = transform.position.x + dir * 1.4f;
            w.root.position = new Vector3(w.x, floorY, 0f);
            w.root.gameObject.SetActive(true);
        }

        private void UpdateWaves(float dt)
        {
            PlayerController player = PlayerController.Current;
            foreach (CoughWave w in waves)
            {
                if (!w.alive) continue;
                w.x += w.dir * waveSpeed * dt;
                w.root.position = new Vector3(w.x, floorY, 0f);
                w.root.localScale = new Vector3(1f + Mathf.Sin(Time.time * 30f) * 0.08f, 1f, 1f);
                if (w.x < WalkableArea.MinX - 1f || w.x > WalkableArea.MaxX + 1f) { w.alive = false; w.root.gameObject.SetActive(false); continue; }
                if (w.hit || player == null || !player.Health.IsAlive) continue;
                if (Mathf.Abs(player.Position.x - w.x) < 0.55f && player.Position.y < floorY + 0.9f)
                {
                    w.hit = true;
                    DamageInfo info = DamageInfo.Create(waveDamage, DamageKind.Contact, Team.Enemy, new Vector2(w.x, floorY));
                    info.knockback = 6f;
                    player.Health.ApplyDamage(info);
                }
            }
        }

        private void TryContact(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            Vector2 center = (Vector2)transform.position + Vector2.up * 1.3f;
            if (box == null || !box.IsAlive || !box.OverlapsCircle(center, contactRadius)) return;
            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, center);
            info.knockback = 7f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            foreach (EnemyHealth m in minions) if (m != null) m.Kill();
            minions.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }
}
