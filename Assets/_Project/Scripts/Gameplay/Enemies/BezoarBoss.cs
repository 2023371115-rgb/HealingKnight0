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
    /// Jefe del Estómago: el BEZOAR. En la vida real es una bola de comida (o pelo, o chicle) que no se digiere,
    /// se queda atorada y puede tapar la salida del estómago (el píloro). Este está hecho de comida chatarra
    /// y lleno de H. pylori viviendo encima.
    ///
    /// Ataques (alterna):
    ///  - Rodar: se lanza rodando de un lado a otro de la arena; rebota en las orillas. Brinca por encima.
    ///  - Lanzar: se detiene y avienta pedazos de comida en arco hacia ti.
    ///  - Azotón (fase 2+): brinca alto y cae haciendo temblar todo; suelta H. pylori al caer.
    /// Fases por vida: cada vez más rápido. Al vencerlo se desmorona y el píloro se abre (la arena se limpia).
    /// El dibujo ('visual') gira al rodar; las caras (normal / enojado / derrotado) se cambian en 'faces'.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class BezoarBoss : MonoBehaviour
    {
        private enum State { Intro, Idle, WindRoll, Roll, Throw, JumpUp, Slam, Dead }

        [Header("Identidad")]
        [SerializeField] private string bossName = "BEZOAR";
        [SerializeField, TextArea] private string introMessage = "NODO: ¡Un bezoar! Una bola de comida que no se digiere tapa el píloro. Antibiótico contra las H. pylori que lo mantienen pegado.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: ¡El bezoar se deshizo! El píloro está libre.";

        [Header("Partes")]
        [SerializeField] private Transform visual;
        [Tooltip("Caras: 0 normal, 1 enojado, 2 derrotado (si hay sprites; si no, se ignoran).")]
        [SerializeField] private GameObject[] faces;
        [SerializeField] private float radius = 1.8f;

        [Header("Rodar")]
        [SerializeField] private float rollSpeed = 7f;
        [SerializeField] private int rollPasses = 2;

        [Header("Lanzar")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private float projectileDamage = 10f;
        [SerializeField] private Color projectileColor = new Color(0.95f, 0.75f, 0.35f);
        [SerializeField] private int chunks = 4;

        [Header("Azotón (fase 2+)")]
        [SerializeField] private float jumpHeight = 6f;
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;
        [SerializeField] private int maxMinionsAlive = 3;

        [Header("Contacto")]
        [SerializeField] private float contactDamage = 16f;

        [Header("Fases (por % de vida)")]
        [SerializeField] private float phase2At = 0.66f;
        [SerializeField] private float phase3At = 0.33f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();
        private State state = State.Intro;
        private float timer, groundY, rollDir, spin, contactCooldown, jumpVel, jumpTargetX;
        private int passesLeft, lastAttack = -1;

        private int Phase => health.Health01 <= phase3At ? 3 : health.Health01 <= phase2At ? 2 : 1;
        private float Speed => Phase == 3 ? 1.35f : Phase == 2 ? 1.15f : 1f;
        private Vector2 Center => (Vector2)transform.position + Vector2.up * radius;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
        }

        private void Start()
        {
            groundY = transform.position.y;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 8, 24);
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
            Sfx.Effect("Lombriz_Ruge", 1f);
            Shake(0.3f, 0.6f);
            state = State.Intro;
            timer = 1.6f;
            SetFace(0);
        }

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            if (contactCooldown > 0f) contactCooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health != null && player.Health.IsAlive;
            float minX = WalkableArea.MinX + radius, maxX = WalkableArea.MaxX - radius;

            switch (state)
            {
                case State.Intro:
                    timer -= dt;
                    Wobble(0.06f);
                    if (timer <= 0f) GoIdle();
                    break;

                case State.Idle:
                    timer -= dt;
                    Wobble(0.03f);
                    if (timer <= 0f && alive) ChooseAttack(player);
                    break;

                case State.WindRoll:
                    // Se echa para atrás y tiembla: aviso de que va a rodar.
                    timer -= dt;
                    spin -= rollDir * 120f * dt;
                    Wobble(0.08f);
                    if (timer <= 0f) { state = State.Roll; Sfx.EffectAt("Lombriz_Entra", Center, 0.8f); }
                    break;

                case State.Roll:
                {
                    float x = transform.position.x + rollDir * rollSpeed * Speed * dt;
                    spin -= rollDir * rollSpeed * Speed * dt / radius * Mathf.Rad2Deg;
                    if (x <= minX || x >= maxX)
                    {
                        x = Mathf.Clamp(x, minX, maxX);
                        rollDir = -rollDir;
                        passesLeft--;
                        Shake(0.15f, 0.25f);
                        Sfx.EffectAt("Lombriz_Retumbo", Center, 0.7f);
                        if (passesLeft <= 0) { SetPos(x, groundY); GoIdle(); break; }
                    }
                    SetPos(x, groundY);
                    break;
                }

                case State.Throw:
                    timer -= dt;
                    Wobble(0.04f);
                    if (timer <= 0f)
                    {
                        if (alive) ThrowChunks(player);
                        GoIdle();
                    }
                    break;

                case State.JumpUp:
                {
                    // Sube y se mueve hacia donde estabas; luego cae.
                    jumpVel -= 30f * dt;
                    float y = transform.position.y + jumpVel * dt;
                    float x = Mathf.MoveTowards(transform.position.x, jumpTargetX, 6f * dt);
                    spin += 200f * dt;
                    if (jumpVel < 0f && y <= groundY)
                    {
                        SetPos(x, groundY);
                        Landed();
                        break;
                    }
                    SetPos(x, y);
                    break;
                }
            }

            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, spin);
            TryContactDamage(player);
        }

        private void ChooseAttack(PlayerController player)
        {
            minions.RemoveAll(m => m == null || !m.IsAlive);
            int attack;
            float r = Random.value;
            if (Phase >= 2 && lastAttack != 2 && r < 0.35f) attack = 2;
            else attack = lastAttack == 0 ? 1 : (r < 0.6f ? 0 : 1);
            lastAttack = attack;

            switch (attack)
            {
                case 0: // rodar hacia el jugador
                    state = State.WindRoll;
                    rollDir = player.Position.x >= transform.position.x ? 1f : -1f;
                    passesLeft = rollPasses + (Phase - 1);
                    timer = 0.7f / Speed;
                    SetFace(1);
                    break;
                case 1:
                    state = State.Throw;
                    timer = 0.55f / Speed;
                    SetFace(1);
                    break;
                default:
                    state = State.JumpUp;
                    jumpVel = Mathf.Sqrt(2f * 30f * jumpHeight);
                    jumpTargetX = Mathf.Clamp(player.Position.x, WalkableArea.MinX + radius, WalkableArea.MaxX - radius);
                    SetFace(1);
                    break;
            }
        }

        private void GoIdle()
        {
            state = State.Idle;
            timer = 1.1f / Speed;
            SetFace(0);
        }

        private void Landed()
        {
            Shake(0.35f, 0.4f);
            Sfx.EffectAt("Lombriz_Retumbo", Center, 1f);
            // Suelta H. pylori (las que lo mantienen pegado).
            if (minionPrefab != null && minions.Count < maxMinionsAlive)
            {
                for (int i = 0; i < 2 && minions.Count < maxMinionsAlive; i++)
                {
                    Vector3 pos = (Vector3)Center + new Vector3(i == 0 ? -radius : radius, 0.3f, 0f);
                    EnemyHealth m = Instantiate(minionPrefab, pos, Quaternion.identity);
                    if (minionData != null) m.Configure(minionData, null);
                    EnemyBrain brain = m.GetComponent<EnemyBrain>();
                    if (brain != null) { if (minionData != null) brain.Configure(minionData); brain.MarkOffspring(); }
                    CharacterMotor2D motor = m.GetComponent<CharacterMotor2D>();
                    if (motor != null) motor.SetVelocity(new Vector2(i == 0 ? -5f : 5f, 6f));
                    ArenaZone.Adopt(m);
                    minions.Add(m);
                }
            }
            GoIdle();
        }

        private void ThrowChunks(PlayerController player)
        {
            if (pool == null) return;
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), Center, CombatAudio.Effects);
            int n = chunks + (Phase - 1);
            float dx = player.Position.x - Center.x;
            for (int i = 0; i < n; i++)
            {
                // Arcos de distinto largo alrededor del jugador: obliga a moverse.
                float spread = (i - (n - 1) * 0.5f) * 1.4f;
                float targetDx = dx + spread;
                float vx = Mathf.Clamp(targetDx / 1.1f, -11f, 11f);
                float vy = 9f + (i % 2) * 1.5f;
                Vector2 v = new Vector2(vx, vy);
                Projectile p = pool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = Center + Vector2.up * (radius * 0.6f),
                    direction = v.normalized,
                    speed = v.magnitude,
                    range = 30f,
                    damage = projectileDamage,
                    knockback = 4f,
                    radius = 0.3f,
                    sourceTeam = Team.Enemy,
                    targetTeam = Team.Player,
                    medication = null,
                    color = projectileColor,
                    groundMask = groundMask,
                    gravity = 18f
                }, finished => pool.Release(finished));
            }
        }

        private void TryContactDamage(PlayerController player)
        {
            if (player == null || contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(Center, radius * 0.9f)) return;
            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, Center);
            info.knockback = 9f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void SetPos(float x, float y) => transform.position = new Vector3(x, y, 0f);

        private void Wobble(float amount)
        {
            if (visual == null) return;
            float s = 1f + Mathf.Sin(Time.time * 9f) * amount;
            visual.localScale = new Vector3(s, 2f - s, 1f);
        }

        private void SetFace(int index)
        {
            if (faces == null) return;
            for (int i = 0; i < faces.Length; i++)
                if (faces[i] != null) faces[i].SetActive(i == index);
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            SetFace(2);
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
            minions.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
            Shake(0.4f, 0.8f);
        }
    }
}
