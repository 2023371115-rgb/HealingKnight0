using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE del Ojo: TOXOPLASMA (Toxoplasma gondii). Biología: un parásito que se esconde en QUISTES dentro de la retina
    /// (forma lenta, bradizoíto) y puede REACTIVARSE en su forma rápida (taquizoíto) e inflamar el ojo.
    /// Pelea de escondidas con dos formas de sacarlo:
    ///  1) SIGILO: llega pegado a su quiste sin que te detecten → lo SORPRENDES (sale mareado un buen rato).
    ///  2) LUZ: dispárale al CRISTALINO (la lente de arriba): enfoca un rayo de luz sobre el quiste más cercano a ti.
    ///     Si ahí está, sale DESLUMBRADO. El cristalino tarda en recargarse.
    ///  Pistas: el quiste ocupado late y de vez en cuando se le asoman los ojos.
    ///  Si no lo sacas, sale solo (el quiste se infla: aviso) y ataca: abanico de taquizoítos (medias lunas) y
    ///  ráfagas directas. Si te ven (barra de inflamación alta) sale antes. El haz de luz que barre la arena también
    ///  lo deslumbra si lo agarra afuera.
    ///  Debajo de la mitad de vida se REACTIVA: se esconde menos y salta de quiste en quiste por el aire soltando esporas.
    /// Al derrotarlo ganas el premio: el ESCÁNER de NODO (ves la debilidad de los enemigos en todos los niveles).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ToxoplasmaBoss : MonoBehaviour
    {
        private enum State { Hidden, WindPop, Out, Stunned, Leap, Dead }

        [SerializeField] private string bossName = "TOXOPLASMA";
        [SerializeField, TextArea] private string introMessage = "NODO: Toxoplasma se esconde en los quistes. Sorpréndelo SIN que te vean... o dispárale al CRISTALINO (arriba): alumbra el quiste más cercano a ti y lo deslumbra.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Toxoplasma eliminado. PREMIO: aprendí a ESCANEAR. Verás la debilidad de cada enemigo en todos los niveles.";
        [SerializeField] private string rewardSecret = "premio_escaneo";
        [SerializeField] private Vector2[] cysts;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite idleSprite, stunnedSprite, attackSprite, deadSprite;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private float surpriseStun = 3.5f;
        [SerializeField] private float lightStun = 2.6f;
        [SerializeField] private float lensRecharge = 5f;
        [SerializeField] private float phase2At = 0.5f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private State state = State.Hidden;
        private int cyst, patternsLeft, pattern;
        private float timer, shootTimer, peekTimer, lensReady, flashTimer, hideTime, lensCharge = -1f;
        private bool reactivated, explainedLens, dazzledThisTrip;
        private Transform[] cystVisuals;
        private Vector2 leapFrom, leapTo;

        // Cristalino (la lente que se dispara para alumbrar).
        private Vector2 lensSpot;
        private Sprite lensSprite;
        private Transform lens, lensGlow, ray;
        private SpriteRenderer rayRenderer;

        private bool Angry => health != null && health.Health01 <= phase2At;

        public void Configure(Vector2[] spots) => cysts = spots;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            if (cystVisuals != null) foreach (Transform t in cystVisuals) if (t != null) Destroy(t.gameObject);
            if (lens != null) Destroy(lens.gameObject);
            if (ray != null) Destroy(ray.gameObject);
        }

        private void Start()
        {
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 8, 30);
            CystLayout layout = FindFirstObjectByType<CystLayout>();
            if ((cysts == null || cysts.Length == 0) && layout != null) cysts = layout.Spots;
            if (cysts == null || cysts.Length == 0) cysts = new[] { (Vector2)transform.position };
            lensSpot = layout != null && layout.Lens != Vector2.zero ? layout.Lens : (Vector2)transform.position + new Vector2(0f, 6f);
            lensSprite = layout != null ? layout.LensArt : null;

            cystVisuals = new Transform[cysts.Length];
            for (int i = 0; i < cysts.Length; i++)
            {
                Transform t = new GameObject("Quiste").transform;
                t.position = cysts[i];
                Sprite c = CodeSprites.Circle;
                CodeSprites.Add(t, c, new Color(0.25f, 0.15f, 0.3f, 0.9f), 3, Vector2.zero, new Vector2(2.3f, 2.1f));
                CodeSprites.Add(t, c, new Color(0.8f, 0.7f, 0.95f, 0.75f), 4, Vector2.zero, new Vector2(2.1f, 1.9f));
                for (int k = 0; k < 6; k++)
                    CodeSprites.Add(t, c, new Color(0.55f, 0.45f, 0.8f, 0.8f), 5, Random.insideUnitCircle * 0.6f, new Vector2(0.25f, 0.25f));
                CodeSprites.Add(t, c, new Color(1f, 1f, 1f, 0.6f), 6, new Vector2(-0.5f, 0.5f), new Vector2(0.4f, 0.25f));
                cystVisuals[i] = t;
            }
            BuildLens();
            cyst = 0;
            Hide();
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
        }

        /// <summary>El cristalino colgado arriba de la arena y el rayo de luz que enfoca.</summary>
        private void BuildLens()
        {
            lens = new GameObject("Cristalino_Jefe").transform;
            lens.position = lensSpot;
            lensGlow = CodeSprites.Add(lens, CodeSprites.Circle, new Color(1f, 0.97f, 0.7f, 0.5f), 40, Vector2.zero, new Vector2(2.6f, 2.2f), "Brillo").transform;
            if (lensSprite != null) CodeSprites.Art(lens, lensSprite, 2f, 41, Vector2.zero);
            else
            {
                CodeSprites.Add(lens, CodeSprites.Circle, new Color(0.3f, 0.45f, 0.75f, 0.9f), 41, Vector2.zero, new Vector2(2.1f, 1.7f), "Borde");
                CodeSprites.Add(lens, CodeSprites.Circle, new Color(0.75f, 0.9f, 1f, 0.85f), 42, Vector2.zero, new Vector2(1.9f, 1.5f), "Lente");
                CodeSprites.Add(lens, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.8f), 43, new Vector2(-0.45f, 0.35f), new Vector2(0.5f, 0.25f), "Reflejo");
            }
            ray = new GameObject("RayoDeLuz").transform;
            rayRenderer = CodeSprites.Add(ray, CodeSprites.Square, new Color(1f, 0.97f, 0.7f, 0f), 44, new Vector2(0f, -0.5f), new Vector2(1.2f, 1f), "Rayo");
        }

        private void Hide()
        {
            state = State.Hidden;
            health.Invulnerable = true;
            transform.position = cysts[cyst];
            if (visual != null) { visual.gameObject.SetActive(false); visual.localScale = Vector3.one; }
            timer = 0f;
            peekTimer = 1.2f;
            dazzledThisTrip = false;
            hideTime = Angry ? Random.Range(2f, 3f) : Random.Range(4f, 5.5f);
        }

        private void Update()
        {
            UpdateArt();
            UpdateLensVisual();
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            StealthMeter meter = StealthMeter.Current;
            bool alive = p != null && p.Health != null && p.Health.IsAlive;
            timer += dt;

            if (Angry && !reactivated)
            {
                reactivated = true;
                GameplayEvents.RaiseMessage("¡Toxoplasma se REACTIVÓ (taquizoíto)! Ahora salta de quiste en quiste: esquiva las esporas.");
            }

            // El quiste ocupado late (pista); al avisar se infla.
            for (int i = 0; i < cystVisuals.Length; i++)
            {
                float s = 1f;
                if (i == cyst && state == State.Hidden) s = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
                if (i == cyst && state == State.WindPop) s = 1f + 0.18f * Mathf.Clamp01(timer / 0.6f) + Mathf.Sin(Time.time * 40f) * 0.03f;
                cystVisuals[i].localScale = new Vector3(s, s, 1f);
            }

            // Mira hacia el jugador (el dibujo mira a la izquierda).
            if (visual != null && p != null && state != State.Hidden)
            {
                float sx = p.Position.x < transform.position.x ? 1f : -1f;
                Vector3 sc = visual.localScale;
                visual.localScale = new Vector3(sx * Mathf.Abs(sc.x), sc.y, 1f);
            }

            if (alive) CheckLensShot(p);
            if (lensCharge >= 0f)
            {
                // La lente junta luz un momento (te da tiempo de pararte junto al quiste) y luego dispara el rayo.
                lensCharge += dt;
                if (lensCharge >= 0.9f) { lensCharge = -1f; if (alive) FocusLight(p); }
            }

            switch (state)
            {
                case State.Hidden:
                {
                    // Se le asoman los ojos de vez en cuando.
                    peekTimer -= dt;
                    if (visual != null)
                    {
                        bool peek = peekTimer < 0.35f;
                        visual.gameObject.SetActive(peek);
                        if (peek) visual.localScale = new Vector3(0.55f * Mathf.Sign(visual.localScale.x == 0f ? 1f : visual.localScale.x), 0.55f, 1f);
                        if (peekTimer <= 0f) peekTimer = Random.Range(1.4f, 2.4f);
                    }
                    if (!alive) break;
                    float d = Vector2.Distance(p.Position + Vector2.up * 0.9f, cysts[cyst]);
                    bool noticed = meter != null && (meter.Level > 0.55f || meter.Alerted);
                    if (d < 2.6f && !noticed) { Stun(surpriseStun, "¡Lo SORPRENDISTE! Está mareado: ¡ANTIPARASITARIO!"); break; }
                    if ((noticed && d < 8f) || timer > hideTime) { state = State.WindPop; timer = 0f; Sfx.EffectAt("Parasito_Prepara", transform.position, 0.9f); }
                    break;
                }

                case State.WindPop:
                    if (timer >= 0.6f) GoOut();
                    break;

                case State.Out:
                    if (CheckSweepingLight()) break;
                    shootTimer -= dt;
                    if (shootTimer <= 0f && alive)
                    {
                        if (patternsLeft <= 0) { Leave(); break; }
                        Attack(p);
                        patternsLeft--;
                    }
                    break;

                case State.Stunned:
                    if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10f) * 15f);
                    if (timer > shootTimer)   // shootTimer guarda cuánto dura el mareo
                    {
                        if (visual != null) visual.localRotation = Quaternion.identity;
                        state = State.Out;
                        patternsLeft = 1;
                        shootTimer = 0.4f;
                        timer = 0f;
                    }
                    break;

                case State.Leap:
                {
                    // Salto en arco al otro quiste, soltando esporas que caen.
                    float k = Mathf.Clamp01(timer / 1.1f);
                    Vector2 pos = Vector2.Lerp(leapFrom, leapTo, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 3.2f;
                    transform.position = pos;
                    if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, k * 360f * Mathf.Sign(leapFrom.x - leapTo.x));
                    shootTimer -= dt;
                    if (shootTimer <= 0f) { shootTimer = 0.14f; Fire(pos, Vector2.down + new Vector2(Random.Range(-0.4f, 0.4f), 0f), 2.5f, 7f, 14f); }
                    if (k >= 1f)
                    {
                        if (visual != null) visual.localRotation = Quaternion.identity;
                        Sfx.EffectAt("Quiste_Entra", transform.position, 0.8f);
                        Hide();
                    }
                    break;
                }
            }
        }

        // ================================================================ salir, atacar, esconderse

        private void GoOut()
        {
            Pop();
            state = State.Out;
            health.Invulnerable = false;
            patternsLeft = Angry ? 3 : 2;
            shootTimer = 0.35f;
            timer = 0f;
        }

        private void Pop()
        {
            if (visual != null) { visual.gameObject.SetActive(true); visual.localPosition = Vector3.zero; visual.localScale = Vector3.one; }
            Sfx.EffectAt("Quiste_Sale", transform.position, 0.9f);
        }

        private void Stun(float duration, string message)
        {
            Pop();
            state = State.Stunned;
            timer = 0f;
            shootTimer = duration;
            health.Invulnerable = false;
            Sfx.Effect("Escudo_Parry", 0.8f);
            if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
        }

        private void Attack(PlayerController p)
        {
            pattern++;
            Vector2 from = transform.position;
            Vector2 aim = (p.Position + Vector2.up * 0.9f - from).normalized;
            if (pattern % 2 == 1)
            {
                // Abanico de taquizoítos (medias lunas): 5 en abanico, con hueco entre cada una.
                int n = Angry ? 7 : 5;
                float spread = Angry ? 70f : 56f;
                float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                for (int i = 0; i < n; i++)
                {
                    float a = (baseAngle - spread * 0.5f + spread * i / (n - 1)) * Mathf.Deg2Rad;
                    Fire(from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 6.5f, 8f, 0f);
                }
                shootTimer = 1.1f;
            }
            else
            {
                // Ráfaga directa: 3 rápidas a donde estás.
                for (int i = 0; i < 3; i++) Fire(from - aim * (i * 0.6f), aim, 9.5f, 7f, 0f);
                shootTimer = 0.9f;
            }
            Sfx.EffectAt("Parasito_Disparo", from, 1f);
        }

        private void Fire(Vector2 from, Vector2 dir, float speed, float damage, float gravity)
        {
            if (pool == null) return;
            Projectile shot = pool.Get();
            shot.Launch(new ProjectileShot
            {
                origin = from, direction = dir.normalized, speed = speed, range = 22f, damage = damage, knockback = 3f, radius = 0.28f,
                sourceTeam = Team.Enemy, targetTeam = Team.Player, color = new Color(0.7f, 1f, 0.6f), groundMask = groundMask, gravity = gravity
            }, done => pool.Release(done));
        }

        /// <summary>Se va a otro quiste: escondido (antes) o saltando por el aire (ya reactivado).</summary>
        private void Leave()
        {
            int next = cyst;
            if (cysts.Length > 1) while (next == cyst) next = Random.Range(0, cysts.Length);
            if (Angry)
            {
                leapFrom = transform.position;
                leapTo = cysts[next];
                cyst = next;
                state = State.Leap;
                timer = 0f;
                shootTimer = 0.2f;
                Sfx.EffectAt("Parasito_Ataca", transform.position, 0.9f);
                return;
            }
            cyst = next;
            Sfx.EffectAt("Quiste_Entra", transform.position, 0.8f);
            Hide();
        }

        // ================================================================ luz: el cristalino y el haz que barre

        /// <summary>¿Una bala del jugador le pegó al cristalino? Junta luz 0.9 s y la enfoca en el quiste más cercano al jugador.</summary>
        private void CheckLensShot(PlayerController p)
        {
            if (Time.time < lensReady || lensCharge >= 0f) return;
            foreach (Projectile s in Projectile.Flying)
            {
                if (s == null || s.SourceTeam != Team.Player || Vector2.Distance(s.Position, lensSpot) > 1.3f) continue;
                s.Consume();
                lensCharge = 0f;
                lensReady = Time.time + lensRecharge;
                Sfx.Effect("Neurona_Carga", 0.6f);
                break;
            }
        }

        private void FocusLight(PlayerController p)
        {
            int target = 0;
            float best = float.MaxValue;
            for (int i = 0; i < cysts.Length; i++)
            {
                float d = Mathf.Abs(cysts[i].x - p.Position.x) + Mathf.Abs(cysts[i].y - p.Position.y) * 0.3f;
                if (d < best) { best = d; target = i; }
            }
            // Dibujo del rayo: de la lente al quiste.
            Vector2 to = cysts[target];
            Vector2 delta = to - lensSpot;
            ray.position = lensSpot;
            ray.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f);
            ray.localScale = new Vector3(1f, delta.magnitude, 1f);
            flashTimer = 0.45f;
            Sfx.Effect("Campo_Neuroprotector", 0.7f);
            Shake(0.08f, 0.2f);

            bool there = target == cyst && (state == State.Hidden || state == State.WindPop || state == State.Out);
            if (there) Stun(lightStun, "¡La luz lo DESLUMBRÓ! ¡Ahora!");
            else if (!explainedLens)
            {
                explainedLens = true;
                GameplayEvents.RaiseMessage("Ahí no estaba. Dispárale al cristalino y PÁRATE junto al quiste que late: la luz cae en el más cercano a ti.");
            }
        }

        /// <summary>El haz de luz que barre la arena también lo deslumbra si lo agarra afuera.</summary>
        private bool CheckSweepingLight()
        {
            if (dazzledThisTrip || !LightBeam.Lights(transform.position)) return false;
            dazzledThisTrip = true;
            Stun(1.8f, "¡El haz de luz lo deslumbró!");
            return true;
        }

        private void UpdateLensVisual()
        {
            if (lensGlow != null)
            {
                bool ready = Time.time >= lensReady;
                float pulse = lensCharge >= 0f ? 1f + lensCharge * 0.6f : ready ? 1f + Mathf.Sin(Time.time * 4f) * 0.08f : 0.7f;
                ready |= lensCharge >= 0f;
                lensGlow.localScale = new Vector3(2.6f * pulse, 2.2f * pulse, 1f);
                SpriteRenderer r = lensGlow.GetComponent<SpriteRenderer>();
                if (r != null) r.color = new Color(1f, 0.97f, 0.7f, ready ? 0.5f : 0.12f);
            }
            if (rayRenderer != null)
            {
                flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
                rayRenderer.color = new Color(1f, 0.97f, 0.7f, flashTimer / 0.45f * 0.8f);
            }
        }

        private void UpdateArt()
        {
            if (art == null) return;
            Sprite s = state == State.Dead ? deadSprite : state == State.Stunned ? stunnedSprite
                : state == State.Out || state == State.Leap || state == State.WindPop ? attackSprite : idleSprite;
            if (s == null) s = idleSprite;
            if (s != null && art.sprite != s) art.sprite = s;
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            if (visual != null) { visual.gameObject.SetActive(true); visual.localScale = Vector3.one; visual.localRotation = Quaternion.identity; }
            GameSession.FindSecret(rewardSecret);
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }
    }

    /// <summary>Dónde están los quistes de la retina y el cristalino en esta escena (el jefe Toxoplasma los usa al aparecer).</summary>
    public class CystLayout : MonoBehaviour
    {
        [SerializeField] private Vector2[] spots;
        [SerializeField] private Vector2 lens;
        [SerializeField] private Sprite lensArt;
        public Vector2[] Spots => spots;
        public Vector2 Lens => lens;
        public Sprite LensArt => lensArt;
        public void Configure(Vector2[] positions) => spots = positions;
        public void Configure(Vector2[] positions, Vector2 lensPosition, Sprite lensDrawing)
        {
            spots = positions;
            lens = lensPosition;
            lensArt = lensDrawing;
        }
    }
}
