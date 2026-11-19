using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Flight;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE del Corazón: el TRYPANOSOMA CRUZI (enfermedad de Chagas). Un parásito largo con un látigo (flagelo) y una
    /// membrana que ondula. Nada por la cámara del corazón.
    /// - Nada en ocho, se LANZA hacia ti (antes parpadea) y escupe parásitos chiquitos.
    /// - ARRITMIA (desde 65 % y luego cada rato): se mete al músculo del corazón (no le haces nada) y el latido se
    ///   vuelve DESORDENADO. En cada latido el piso da TOQUE: brilla justo antes, ¡salta! Hay que leer el ritmo nuevo.
    /// Se trata con antiparasitario. Biología: lo pasa la chinche besucona (triatomino); se esconde en el músculo del
    /// corazón y con los años causa arritmias y un corazón grande y débil.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class TrypanosomaBoss : MonoBehaviour
    {
        private enum State { Swim, Aim, Lunge, Burrowed, Dead }

        [SerializeField] private string bossName = "TRYPANOSOMA CRUZI";
        [SerializeField] private Transform visual;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float swimHeight = 4.2f;
        [SerializeField] private float lungeSpeed = 13f;
        [SerializeField] private float contactDamage = 13f;
        [SerializeField] private float shockDamage = 8f;
        [SerializeField] private float arrhythmiaEvery = 15f;
        [SerializeField] private float arrhythmiaTime = 8f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private State state = State.Swim;
        private float floorY, t, timer, contactCooldown, arrhythmiaTimer, shockCooldown;
        private Vector2 lungeDir, burrowSpot;
        private bool arrhythmia, toldArrhythmia, dead;
        private readonly List<Transform> segments = new List<Transform>();
        private readonly List<SpriteRenderer> floorGlow = new List<SpriteRenderer>();
        private int facing = -1;

        private BossPoses poses;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            poses = GetComponent<BossPoses>();
            if (poses != null && !poses.HasArt) poses = null;
        }
        private void OnEnable() { health.Died += OnDied; Heartbeat.Beat += OnBeat; }
        private void OnDisable() { health.Died -= OnDied; Heartbeat.Beat -= OnBeat; }

        private void OnDestroy()
        {
            if (!dead) GameplayEvents.RaiseBossEnded();
            if (Heartbeat.Current != null) Heartbeat.Current.Irregular = 0f;
            pool?.Clear();
        }

        private void Start()
        {
            floorY = transform.position.y;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 8, 30);
            if (visual != null)
                foreach (Transform c in visual) if (c.name.StartsWith("Segmento")) segments.Add(c);
            // Brillo del piso para el toque de la arritmia.
            for (float x = WalkableArea.MinX + 0.8f; x < WalkableArea.MaxX; x += 1.6f)
            {
                var g = new GameObject("ToquePiso");
                g.transform.position = new Vector3(x, floorY + 0.15f, 0f);
                SpriteRenderer r = CodeSprites.Add(g.transform, CodeSprites.Glow, new Color(1f, 0.95f, 0.4f, 0f), 45, Vector2.zero, new Vector2(2.2f, 1f), "Toque");
                floorGlow.Add(r);
            }
            GameplayEvents.RaiseBossStarted(bossName, health);
            GameplayEvents.RaiseMessage("¡El TRYPANOSOMA CRUZI (Chagas)! Se lanza contra ti y se esconde en el músculo del corazón. ANTIPARASITARIO.");
            timer = 1.5f;
            transform.position = new Vector3(transform.position.x, floorY + swimHeight, 0f);
        }

        private void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            t += dt;
            if (contactCooldown > 0f) contactCooldown -= dt;
            if (shockCooldown > 0f) shockCooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health.IsAlive;
            float minX = WalkableArea.MinX + 2f, maxX = WalkableArea.MaxX - 2f;

            // La arritmia llega a partir del 65 % y luego cada rato.
            if (state != State.Burrowed && health.Health01 <= 0.65f)
            {
                arrhythmiaTimer -= dt;
                if (!toldArrhythmia || arrhythmiaTimer <= 0f) StartArrhythmia();
            }

            timer -= dt;
            Vector2 pos = transform.position;
            switch (state)
            {
                case State.Swim:
                {
                    float x = Mathf.Lerp(minX, maxX, 0.5f + 0.5f * Mathf.Sin(t * 0.55f));
                    float y = floorY + swimHeight + Mathf.Sin(t * 1.1f) * 1.6f;
                    Vector2 next = Vector2.Lerp(pos, new Vector2(x, y), 1f - Mathf.Exp(-2f * dt));
                    facing = next.x < pos.x ? -1 : 1;
                    pos = next;
                    if (timer <= 0f && alive)
                    {
                        if (Random.value < 0.6f) { state = State.Aim; timer = 0.7f; if (poses != null) poses.Show(BossPoses.Pose.Attack, 1.4f); }
                        else { Spit(player); timer = 2f; }
                    }
                    break;
                }
                case State.Aim:
                    // Parpadea apuntando: luego se lanza en línea recta.
                    if (visual != null) visual.gameObject.SetActive(Mathf.Sin(Time.time * 30f) > -0.3f);
                    if (alive) lungeDir = (player.Position + Vector2.up * 0.8f - pos).normalized;
                    if (timer <= 0f)
                    {
                        if (visual != null) visual.gameObject.SetActive(true);
                        state = State.Lunge;
                        timer = 0.65f;
                        Sfx.EffectAt("Jugador_Dash", pos, 0.8f);
                    }
                    break;
                case State.Lunge:
                    pos += lungeDir * lungeSpeed * dt;
                    pos.x = Mathf.Clamp(pos.x, minX, maxX);
                    pos.y = Mathf.Clamp(pos.y, floorY + 0.8f, floorY + 8f);
                    facing = lungeDir.x < 0f ? -1 : 1;
                    if (timer <= 0f) { state = State.Swim; timer = Random.Range(1.6f, 2.6f); }
                    break;
                case State.Burrowed:
                    pos = Vector2.Lerp(pos, burrowSpot, 1f - Mathf.Exp(-3f * dt));
                    if (timer <= 0f) EndArrhythmia();
                    break;
            }
            transform.position = new Vector3(pos.x, pos.y, 0f);
            Animate();
            UpdateFloorGlow();
            if (state != State.Burrowed) TryContact(player);
        }

        /// <summary>El cuerpo ondula como una serpiente (la membrana ondulante) y mira hacia donde nada.</summary>
        private void Animate()
        {
            if (visual == null) return;
            // La figura nada con el flagelo por delante (como el de verdad); el dibujo mira hacia donde va.
            visual.localScale = new Vector3(poses != null ? facing : -facing, 1f, 1f);
            if (poses != null) poses.SetAngry(arrhythmia || health.Health01 <= 0.35f);
            for (int i = 0; i < segments.Count; i++)
            {
                Vector3 p = segments[i].localPosition;
                p.y = Mathf.Sin(t * 9f - i * 0.8f) * 0.28f;
                segments[i].localPosition = p;
            }
        }

        private void UpdateFloorGlow()
        {
            if (floorGlow.Count == 0) return;
            float a = 0f;
            if (arrhythmia && Heartbeat.Current != null)
            {
                float left = Heartbeat.Current.SecondsToNextBeat;
                a = left < 0.4f ? (1f - left / 0.4f) * 0.75f : 0f;
            }
            foreach (SpriteRenderer r in floorGlow)
            {
                if (r == null) continue;
                Color c = r.color;
                c.a = Mathf.MoveTowards(c.a, a, Time.deltaTime * 6f);
                r.color = c;
            }
        }

        private void OnBeat(int count)
        {
            if (!arrhythmia || dead) return;
            // ¡Toque! en el piso con cada latido desordenado.
            foreach (SpriteRenderer r in floorGlow) if (r != null) { Color c = r.color; c.a = 1f; r.color = c; }
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive || shockCooldown > 0f) return;
            if (player.Motor.IsGrounded && player.Position.y < floorY + 0.6f)
            {
                shockCooldown = 0.4f;
                DamageInfo info = DamageInfo.Create(shockDamage, DamageKind.Environment, Team.Neutral, player.Position);
                info.blockable = false;
                player.Health.ApplyDamage(info);
            }
        }

        private void StartArrhythmia()
        {
            arrhythmia = true;
            state = State.Burrowed;
            timer = arrhythmiaTime;
            arrhythmiaTimer = arrhythmiaEvery;
            health.Shelled = true;
            burrowSpot = new Vector2(Random.value < 0.5f ? WalkableArea.MinX + 1f : WalkableArea.MaxX - 1f, floorY + 7.5f);
            if (Heartbeat.Current != null) Heartbeat.Current.Irregular = 1f;
            SpeedBanner.Show("¡ARRITMIA!", new Color(1f, 0.9f, 0.4f), 1.4f);
            if (!toldArrhythmia)
            {
                toldArrhythmia = true;
                GameplayEvents.RaiseMessage("¡ARRITMIA! Se metió al músculo y el corazón late DESORDENADO. El piso da TOQUE en cada latido: " +
                    "cuando BRILLE, ¡SALTA! (o súbete a las cuerdas).");
            }
        }

        private void EndArrhythmia()
        {
            arrhythmia = false;
            state = State.Swim;
            timer = 1.2f;
            health.Shelled = false;
            if (Heartbeat.Current != null) Heartbeat.Current.Irregular = 0f;
        }

        private void Spit(PlayerController player)
        {
            if (pool == null || player == null) return;
            if (poses != null) poses.Show(BossPoses.Pose.Attack);
            Vector2 from = transform.position;
            Vector2 dir = (player.Position + Vector2.up * 0.8f - from).normalized;
            float baseA = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = -1; i <= 1; i++)
            {
                float a = (baseA + i * 18f) * Mathf.Deg2Rad;
                Projectile p = pool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = from, direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed = 7f, range = 22f, damage = 9f,
                    knockback = 2.5f, radius = 0.24f, sourceTeam = Team.Enemy, targetTeam = Team.Player,
                    color = new Color(0.55f, 0.85f, 0.95f), groundMask = 1 << PhysicsLayers.Ground
                }, done => pool.Release(done));
            }
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), from, CombatAudio.Enemies);
        }

        private void TryContact(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(transform.position, 1.1f)) return;
            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, transform.position);
            info.knockback = 7f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void OnDied(EnemyHealth _)
        {
            dead = true;
            state = State.Dead;
            if (poses != null) poses.Show(BossPoses.Pose.Defeated);
            if (Heartbeat.Current != null) Heartbeat.Current.Irregular = 0f;
            foreach (SpriteRenderer r in floorGlow) if (r != null) Destroy(r.gameObject);
            GameplayEvents.RaiseBossEnded();
            GameplayEvents.RaiseMessage("¡T. cruzi vencido y el corazón vuelve a su ritmo! El Chagas se previene sellando las grietas de la casa " +
                "(ahí se esconde la chinche) y usando pabellón. Se trata con medicinas que receta un médico.");
        }
    }
}
