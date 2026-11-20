using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Mueve a un enemigo de siempre (su vida, debilidades y dibujo) en un nivel de vuelo: apaga su IA de piso y lo
    /// lleva por la pantalla con el patrón de su oleada. Dispara AL RITMO del latido (avisa medio latido antes
    /// poniéndose rojo) y daña si lo tocas. Al morir puede separarse en crías (racimo de S. aureus → cocos).
    /// Si sale por la izquierda se fue (no cuenta como eliminado).
    /// </summary>
    public class FlightEnemy : MonoBehaviour
    {
        private FlightWave wave;
        private FlightWaveSpec spec;
        private EnemyHealth health;
        private Hurtbox box;
        private Transform visual;
        private Vector3 visualBase = Vector3.one;
        private Vector2 screen, offset, hoverSpot;
        private float age, seed, baseY, hoverLeft, contactCooldown;
        private int beats;
        private bool shotPending, entered, leaving;
        private Hurtbox playerBox;

        public void Begin(FlightWave owner, FlightWaveSpec waveSpec, Vector2 startScreen, float randomSeed)
        {
            wave = owner;
            spec = waveSpec;
            screen = startScreen;
            seed = randomSeed;
            baseY = startScreen.y;
            hoverLeft = spec.hoverTime;
            // Dónde se queda flotando: los que entran por la derecha a su altura; los de arriba/abajo, adentro de la pantalla.
            hoverSpot = spec.edge == FlightEdge.Right
                ? new Vector2(spec.hoverX, startScreen.y)
                : new Vector2(spec.hoverX + (startScreen.x - spec.entry), spec.edge == FlightEdge.Top ? 2.4f : -2.4f);

            health = GetComponent<EnemyHealth>();
            box = GetComponent<Hurtbox>();
            // Su IA de piso no sirve aquí: la apagamos (y su física: aquí no hay gravedad ni choques).
            EnemyBrain brain = GetComponent<EnemyBrain>();
            if (brain != null) brain.enabled = false;
            CharacterMotor2D motor = GetComponent<CharacterMotor2D>();
            if (motor != null) motor.enabled = false;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
            visual = transform.Find("Visual");
            if (visual != null) visualBase = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);
            offset = box != null ? box.Center - (Vector2)transform.position : Vector2.zero;
            if (health != null) health.Died += OnDied;
        }

        private void OnEnable() => Heartbeat.Beat += OnBeat;

        private void OnDisable()
        {
            Heartbeat.Beat -= OnBeat;
            if (health != null) health.SetTelegraph(false);
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        private Vector2 Center => box != null ? box.Center : (Vector2)transform.position;

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null || spec == null || health == null || !health.IsAlive) return;
            float dt = Time.deltaTime;
            age += dt;
            if (contactCooldown > 0f) contactCooldown -= dt;
            float hw = route.HalfWidth, hh = route.HalfHeight;
            PlayerController player = PlayerController.Current;
            Vector2 playerScreen = player != null ? player.Position + new Vector2(0f, 0.6f) - route.CameraPosition : Vector2.zero;

            // Los que salen de una rama primero BAJAN por el tubo (sin atravesar sus paredes) y luego hacen lo suyo.
            if (spec.worldX != 0f && spec.edge != FlightEdge.Right && age < 0.75f)
            {
                screen.x = spec.worldX - route.CameraPosition.x;
                screen.y += (spec.edge == FlightEdge.Top ? -5f : 5f) * dt;
                baseY = screen.y;
            }
            else switch (spec.pattern)
            {
                case FlightPattern.Line:
                    screen.x -= spec.speed * dt;
                    baseY += spec.drift * dt;
                    screen.y = baseY + Mathf.Sin(age * spec.frequency * Mathf.PI * 2f + seed) * spec.amplitude;
                    break;

                case FlightPattern.Swoop:
                    // Baja (o sube) hacia tu altura mientras viene; ya cerca sigue derecho.
                    screen.x -= spec.speed * dt;
                    if (screen.x > -hw * 0.2f)
                        screen.y = Mathf.Lerp(screen.y, Mathf.Clamp(playerScreen.y, -hh + 1f, hh - 1f), 1f - Mathf.Exp(-1.8f * dt));
                    break;

                case FlightPattern.Hover:
                    if (!leaving)
                    {
                        // Entra frenando hasta su lugar y ahí se queda flotando.
                        Vector2 spot = hoverSpot;
                        screen = Vector2.Lerp(screen, spot + new Vector2(0f, Mathf.Sin(age * 1.6f + seed) * 0.4f), 1f - Mathf.Exp(-2.2f * dt));
                        if (Mathf.Abs(screen.x - spot.x) < 0.4f) hoverLeft -= dt;
                        if (hoverLeft <= 0f) leaving = true;
                    }
                    else screen.x -= (spec.speed + age * 0.2f) * dt;
                    break;

                case FlightPattern.Chase:
                    if (!leaving)
                    {
                        Vector2 to = playerScreen - screen;
                        if (to.sqrMagnitude > 0.01f) screen += to.normalized * spec.speed * dt;
                        hoverLeft -= dt;
                        if (hoverLeft <= 0f) leaving = true;
                    }
                    else screen.x -= spec.speed * 1.6f * dt;
                    break;
            }

            transform.position = (Vector3)(route.CameraPosition + screen - offset);
            if (!entered && Mathf.Abs(screen.x) < hw - 0.3f && Mathf.Abs(screen.y) < hh - 0.3f) entered = true;

            // Mira hacia el jugador (los dibujos miran a la derecha) y "nada" un poco.
            if (visual != null)
            {
                float face = playerScreen.x < screen.x ? -1f : 1f;
                float wob = Mathf.Sin(age * 6f + seed) * 0.05f;
                visual.localScale = new Vector3(visualBase.x * face * (1f - wob), visualBase.y * (1f + wob), visualBase.z);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 2.3f + seed) * 6f);
            }

            // Aviso medio latido antes de disparar.
            if (shotPending) health.SetTelegraph(Heartbeat.PhaseNow > 0.5f);

            Contact(player);

            bool gone = screen.x < -hw - 3f || (entered && Mathf.Abs(screen.y) > hh + 3f) || age > 40f;
            if (gone) Destroy(gameObject);
        }

        private void Contact(PlayerController player)
        {
            if (player == null || box == null || contactCooldown > 0f) return;
            if (playerBox == null) playerBox = player.GetComponent<Hurtbox>();
            if (playerBox == null || !playerBox.IsAlive || !playerBox.OverlapsRect(box.WorldRect)) return;
            float damage = health.Data != null ? health.Data.attackDamage : 10f;
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, Center);
            info.knockback = 2f;
            playerBox.Damageable.ApplyDamage(info);
            contactCooldown = 0.8f;
        }

        private void OnBeat(int count)
        {
            if (spec == null || spec.fireEveryBeats <= 0 || health == null || !health.IsAlive || !entered) return;
            if (leaving && spec.pattern == FlightPattern.Hover) return;
            FlightRoute route = FlightRoute.Current;
            if (route == null || screen.x > route.HalfWidth - 0.6f || screen.x < -route.HalfWidth + 1.5f) return;
            beats++;
            if (shotPending)
            {
                shotPending = false;
                health.SetTelegraph(false);
                Fire();
                beats = 0;
            }
            if (beats + 1 >= spec.fireEveryBeats) shotPending = true;
        }

        private void Fire()
        {
            if (wave == null) return;
            PlayerController player = PlayerController.Current;
            Vector2 from = Center;
            Vector2 dir = Vector2.left;
            if (spec.aimed && player != null) dir = (player.Position + new Vector2(0f, 0.6f) - from).normalized;
            EnemyData d = health.Data;
            Color color = d != null && d.projectileColor.a > 0f ? d.projectileColor : new Color(1f, 0.9f, 0.4f);
            float damage = d != null ? d.attackDamage : 10f;
            int n = Mathf.Max(1, spec.shots);
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                float a = (baseAngle + (n > 1 ? -spec.spreadDegrees * 0.5f + spec.spreadDegrees * i / (n - 1) : 0f)) * Mathf.Deg2Rad;
                wave.Fire(from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), spec.shotSpeed, damage, color);
            }
            Sfx.PlayAt(Sfx.EnemySound(d, "Disparo"), from, CombatAudio.Enemies);
        }

        private void OnDied(EnemyHealth _)
        {
            if (spec == null || spec.splitPrefab == null || spec.splitCount <= 0 || wave == null) return;
            // Las crías salen disparadas en abanico y siguen de frente.
            for (int i = 0; i < spec.splitCount; i++)
            {
                FlightWaveSpec kid = spec.Copy();
                kid.prefab = spec.splitPrefab;
                kid.splitPrefab = null;
                kid.splitCount = 0;
                kid.pattern = FlightPattern.Line;
                kid.amplitude = 0f;
                kid.speed = spec.speed + 1.5f;
                kid.drift = (spec.splitCount > 1 ? Mathf.Lerp(-2.2f, 2.2f, i / (spec.splitCount - 1f)) : 0f);
                kid.fireEveryBeats = 0;
                kid.message = null;
                wave.SpawnAt(kid, screen + new Vector2(0f, kid.drift * 0.15f), Random.value * 10f);
            }
        }
    }
}
