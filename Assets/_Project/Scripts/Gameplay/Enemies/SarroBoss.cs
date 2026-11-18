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
    /// JEFE de la Boca: EL SARRO. Biología: el sarro (cálculo dental) es placa bacteriana que se endureció como piedra;
    /// el cepillo ya no lo quita, solo raspándolo.
    ///  - Lo cubren PLACAS de sarro: mientras tenga alguna, las balas no le hacen nada.
    ///  - Engánchale el HILO DENTAL a una placa y mantente colgado: la arrancas (y le duele).
    ///  - Sin placas queda EXPUESTO unos segundos: ahí sí le pega el antibiótico. Luego le vuelven a crecer.
    ///  - Ataques: camina hacia ti, pisotón que manda ondas por el piso (sáltalas) y lanza pedazos de sarro en arco.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class SarroBoss : MonoBehaviour
    {
        private enum State { Intro, Walk, Stomp, Throw, Exposed, Dead }

        [SerializeField] private string bossName = "EL SARRO";
        [SerializeField, TextArea] private string introMessage = "NODO: ¡Sarro! Es placa hecha piedra: las balas no le hacen nada. Engánchale el HILO DENTAL a sus placas para arrancarlas.";
        [SerializeField, TextArea] private string exposedMessage = "¡Sin placas! Está expuesto: ANTIBIÓTICO ahora, antes de que le vuelvan a crecer.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Sarro eliminado. ¡Dientes limpios!";
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite idleSprite, attackSprite, exposedSprite, deadSprite;
        [SerializeField] private Sprite plateArt;
        [SerializeField] private float walkSpeed = 1.3f;
        [SerializeField] private float exposedTime = 7f;
        [SerializeField] private float ripDamage = 25f;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private Vector2[] plateSpots = { new Vector2(-1.1f, 3.4f), new Vector2(0.2f, 4.1f), new Vector2(1.3f, 3.2f) };

        private class Wave { public Transform root; public float x; public int dir; public bool hit, alive; }

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private readonly List<FlossHook> plates = new List<FlossHook>();
        private readonly List<Wave> waves = new List<Wave>();
        private State state = State.Intro;
        private float timer, floorY, contactCooldown;
        private int facing = -1;
        private bool explainedExposed;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            foreach (Wave w in waves) if (w.root != null) Destroy(w.root.gameObject);
        }

        private void Start()
        {
            floorY = transform.position.y;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 6, 20);
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
            GrowPlates();
            timer = 1.5f;
        }

        private void GrowPlates()
        {
            foreach (Vector2 spot in plateSpots)
            {
                var go = new GameObject("PlacaDeSarro");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = spot;
                var hook = go.AddComponent<FlossHook>();
                hook.Configure(0.9f, false, 4f);
                hook.Ripped += OnPlateRipped;
                if (plateArt != null) CodeSprites.Art(go.transform, plateArt, 1.3f, 12, Vector2.zero);
                else
                {
                    Sprite c = CodeSprites.Circle;
                    CodeSprites.Add(go.transform, c, new Color(0.3f, 0.22f, 0.1f), 11, Vector2.zero, new Vector2(1.25f, 1f));
                    CodeSprites.Add(go.transform, c, new Color(0.85f, 0.75f, 0.45f), 12, Vector2.zero, new Vector2(1.1f, 0.86f));
                    CodeSprites.Add(go.transform, c, new Color(0.7f, 0.58f, 0.3f), 13, new Vector2(0.2f, -0.15f), new Vector2(0.4f, 0.3f));
                    CodeSprites.Add(go.transform, c, new Color(1f, 0.95f, 0.75f), 13, new Vector2(-0.25f, 0.2f), new Vector2(0.3f, 0.14f));
                }
                plates.Add(hook);
            }
            health.Invulnerable = true;
        }

        private void OnPlateRipped(FlossHook hook)
        {
            plates.Remove(hook);
            Destroy(hook.gameObject);
            // Arrancarla le duele aunque esté blindado.
            health.Invulnerable = false;
            health.ApplyDamage(DamageInfo.Create(ripDamage, DamageKind.Environment, Team.Player, hook.transform.position));
            Sfx.EffectAt("Lombriz_Ruge", transform.position, 0.8f);
            if (!health.IsAlive) return;
            if (plates.Count > 0) { health.Invulnerable = true; return; }
            state = State.Exposed;
            timer = exposedTime;
            if (!explainedExposed) { explainedExposed = true; GameplayEvents.RaiseMessage(exposedMessage); }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateWaves(dt);
            UpdateArt();
            if (state == State.Dead) return;
            if (contactCooldown > 0f) contactCooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health.IsAlive;
            if (alive && state != State.Exposed) facing = player.Position.x < transform.position.x ? -1 : 1;
            timer -= dt;

            switch (state)
            {
                case State.Intro:
                    if (timer <= 0f) BeginWalk();
                    break;
                case State.Walk:
                    if (alive && Mathf.Abs(player.Position.x - transform.position.x) > 2.2f) MoveX(facing * walkSpeed * dt);
                    if (timer <= 0f)
                    {
                        if (Random.value < 0.5f) { state = State.Stomp; timer = 0.9f; }
                        else { state = State.Throw; timer = 0.7f; }
                    }
                    break;
                case State.Stomp:
                    if (timer <= 0f)
                    {
                        SpawnWave(-1); SpawnWave(1);
                        Sfx.EffectAt("Lombriz_Retumbo", transform.position, 1f);
                        Camera cam = Camera.main;
                        if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.25f, 0.2f);
                        BeginWalk();
                    }
                    break;
                case State.Throw:
                    if (timer <= 0f) { if (alive) Throw(player); BeginWalk(); }
                    break;
                case State.Exposed:
                    if (timer <= 0f) { GrowPlates(); GameplayEvents.RaiseMessage("¡Le volvió a crecer el sarro! Arráncalo otra vez."); BeginWalk(); }
                    break;
            }

            if (visual != null)
            {
                float squash = state == State.Stomp ? Mathf.Clamp01(1f - timer / 0.9f) * 0.15f : 0f;
                float shiver = state == State.Exposed ? Mathf.Sin(Time.time * 40f) * 0.03f : 0f;
                visual.localScale = new Vector3((1f + squash + shiver) * -facing, 1f - squash, 1f);
            }
            TryContact(player);
        }

        private void UpdateArt()
        {
            if (art == null) return;
            Sprite s = state == State.Dead ? deadSprite
                : state == State.Exposed ? exposedSprite
                : state == State.Stomp || state == State.Throw ? attackSprite
                : idleSprite;
            if (s == null) s = idleSprite;
            if (s != null && art.sprite != s) art.sprite = s;
        }

        private void BeginWalk() { state = State.Walk; timer = Random.Range(1.6f, 2.6f); }

        private void MoveX(float dx)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, WalkableArea.MinX + 2f, WalkableArea.MaxX - 2f);
            transform.position = p;
        }

        private void Throw(PlayerController player)
        {
            if (pool == null) return;
            Vector2 from = (Vector2)transform.position + new Vector2(facing * 1.4f, 3f);
            float dist = Mathf.Clamp(player.Position.x - from.x, -11f, 11f);
            Vector2 dir = new Vector2(dist >= 0f ? 1f : -1f, 1.1f).normalized;
            float speed = Mathf.Clamp(Mathf.Sqrt(Mathf.Abs(dist) * 16f), 5f, 14f);
            Projectile p = pool.Get();
            p.Launch(new ProjectileShot
            {
                origin = from, direction = dir, speed = speed, range = 30f, damage = 10f, knockback = 4f, radius = 0.4f,
                sourceTeam = Team.Enemy, targetTeam = Team.Player, color = new Color(0.85f, 0.75f, 0.45f),
                groundMask = groundMask, gravity = 16f
            }, done => pool.Release(done));
        }

        private void SpawnWave(int dir)
        {
            Wave w = waves.Find(x => !x.alive);
            if (w == null)
            {
                w = new Wave { root = new GameObject("OndaSarro").transform };
                Sprite c = CodeSprites.Circle;
                CodeSprites.Add(w.root, c, new Color(0.3f, 0.22f, 0.1f, 0.9f), 40, new Vector2(0f, 0.4f), new Vector2(0.9f, 0.9f));
                CodeSprites.Add(w.root, c, new Color(0.9f, 0.8f, 0.5f, 0.9f), 41, new Vector2(0f, 0.4f), new Vector2(0.7f, 0.72f));
                waves.Add(w);
            }
            w.alive = true; w.hit = false; w.dir = dir;
            w.x = transform.position.x + dir * 1.6f;
            w.root.position = new Vector3(w.x, floorY, 0f);
            w.root.gameObject.SetActive(true);
        }

        private void UpdateWaves(float dt)
        {
            PlayerController player = PlayerController.Current;
            foreach (Wave w in waves)
            {
                if (!w.alive) continue;
                w.x += w.dir * 8f * dt;
                w.root.position = new Vector3(w.x, floorY, 0f);
                if (w.x < WalkableArea.MinX - 1f || w.x > WalkableArea.MaxX + 1f) { w.alive = false; w.root.gameObject.SetActive(false); continue; }
                if (w.hit || player == null || !player.Health.IsAlive || player.Swinging) continue;
                if (Mathf.Abs(player.Position.x - w.x) < 0.55f && player.Position.y < floorY + 0.8f)
                {
                    w.hit = true;
                    DamageInfo info = DamageInfo.Create(12f, DamageKind.Contact, Team.Enemy, new Vector2(w.x, floorY));
                    info.knockback = 6f;
                    player.Health.ApplyDamage(info);
                }
            }
        }

        private void TryContact(PlayerController player)
        {
            if (player == null || contactCooldown > 0f || player.Swinging) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            Vector2 center = (Vector2)transform.position + Vector2.up * 1.6f;
            if (box == null || !box.IsAlive || !box.OverlapsCircle(center, 1.7f)) return;
            DamageInfo info = DamageInfo.Create(14f, DamageKind.Contact, Team.Enemy, center);
            info.knockback = 7f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            foreach (FlossHook h in plates) if (h != null) Destroy(h.gameObject);
            plates.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }
}
