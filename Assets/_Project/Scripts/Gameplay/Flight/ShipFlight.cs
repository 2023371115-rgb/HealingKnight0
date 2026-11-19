using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// NODO se transforma en una NANOCÁPSULA y el caballero la maneja sentado adentro (niveles de vuelo del sistema
    /// circulatorio, estilo avioncito de Cuphead):
    /// - Te mueves en 8 direcciones, sin gravedad. La pantalla avanza sola (FlightRoute) y la nave va con ella.
    /// - Disparas siempre hacia adelante con el medicamento que traes; la cápsula se pinta de su color.
    /// - DASH ({dash}): un empujón rápido hacia donde te mueves, invencible un instante (pasas entre balas).
    /// - ENCOGERSE (mantener {fijar}): la nave se hace chiquita y más lenta; cabes por capilares y entre balas.
    /// - Al latir el corazón, la sangre te empuja: la nave se queda un poco atrás y luego alcanza a la pantalla.
    /// La agrega FlightRoute al jugador al empezar el nivel; PlayerController la llama cada cuadro (Tick).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class ShipFlight : MonoBehaviour
    {
        [SerializeField] private float speed = 7.5f;
        [SerializeField] private float shrunkSpeed = 4.6f;
        [Tooltip("Qué tan rápido responde (más alto = menos inercia).")]
        [SerializeField] private float response = 13f;
        [SerializeField] private float dashSpeed = 17f;
        [SerializeField] private float dashTime = 0.17f;
        [SerializeField] private float dashCooldown = 0.55f;
        [SerializeField] private float dashInvulnerability = 0.28f;
        [SerializeField] private float shrinkFactor = 0.55f;

        private PlayerController controller;
        private CharacterMotor2D motor;
        private Hurtbox hurtbox;
        private Transform body, ship, flame;
        private SpriteRenderer[] tintParts = new SpriteRenderer[0];
        private SpriteRenderer flameRenderer;
        private Vector2 velocity, push, dashDir = Vector2.right;
        private float dashTimer, dashReadyAt, tilt, hurtPush;
        private Vector2 baseBoxSize, baseBoxOffset;

        public bool Active { get; private set; }
        public bool Shrunk { get; private set; }
        public bool IsDashing => dashTimer > 0f;
        /// <summary>Con adrenalina: invencible y embiste a los enemigos que toca.</summary>
        public bool Boosting => boostTimer > 0f;
        public float ShrinkFactor => shrinkFactor;
        public Vector2 Velocity => velocity;

        /// <summary>Corrientes, glóbulos y ramas de las arterias empujan la nave (fuerza de este cuadro; se llama cada cuadro).</summary>
        public void AddPush(Vector2 v) => push += v;

        /// <summary>Empujón de un golpe (choque con un coágulo, el despegue): se suma de una vez a la velocidad.</summary>
        public void Kick(Vector2 v) => velocity += v;

        /// <summary>ADRENALINA: unos segundos invencible, embistiendo enemigos, con el fuego de la turbina al máximo.</summary>
        public void StartBoost(float seconds)
        {
            boostTimer = Mathf.Max(boostTimer, seconds);
            controller.Health.GrantInvulnerability(seconds + 0.3f);
            velocity += new Vector2(6f, 0f);
        }

        /// <summary>SANGRE ESPESA: la nave se mueve con trabajo y sin dash (se llama cada cuadro mientras estás adentro).</summary>
        public void Slow(float factor)
        {
            slowFactor = Mathf.Min(slowFactor, factor);
            slowUntil = Time.time + 0.15f;
        }

        private float boostTimer, slowFactor = 1f, slowUntil;
        private readonly System.Collections.Generic.List<Hurtbox> rammed = new System.Collections.Generic.List<Hurtbox>();
        private float ramCooldown;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            motor = GetComponent<CharacterMotor2D>();
            hurtbox = GetComponent<Hurtbox>();
        }

        /// <summary>Convierte al caballero en nave (dibuja la cápsula y ajusta su cuerpo). 'art': dibujo de la cápsula (o null).</summary>
        public void Activate(Sprite art = null)
        {
            if (Active) return;
            Active = true;
            controller.Flight = this;
            motor.SuspendGravity = true;
            motor.SetFacing(1);
            body = motor.Visual != null ? motor.Visual.Find("Body") : null;
            if (body == null) body = motor.Visual != null ? motor.Visual : transform;
            BuildShip(art);
            if (motor.Collider != null)
            {
                // Casco + cabina (para que la cabeza no se meta en las paredes del vaso).
                baseBoxSize = new Vector2(1.7f, 1.75f);
                baseBoxOffset = new Vector2(0.05f, 0.9f);
                motor.Collider.size = baseBoxSize;
                motor.Collider.offset = baseBoxOffset;
            }
            if (controller.Shooter != null)
            {
                controller.Shooter.RangeMultiplier = 1.7f;
                controller.Shooter.MedicationChanged += OnMedication;
                OnMedication(controller.Shooter.Current);
            }
        }

        private void OnDestroy()
        {
            if (controller != null && controller.Shooter != null) controller.Shooter.MedicationChanged -= OnMedication;
        }

        // ───────────────────────────── La nave ─────────────────────────────

        private void BuildShip(Sprite art)
        {
            // El casco va ENTRE las piernas (atrás) y el torso (adelante): se suben las capas del torso, el brazo y el
            // escudo para dejarle lugar (el jugador tiene su propio grupo de capas, no afecta al resto del nivel).
            Transform visual = motor.Visual != null ? motor.Visual : transform;
            foreach (SpriteRenderer r in visual.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.sortingOrder >= 7) r.sortingOrder += 10;

            ship = new GameObject("Nanocapsula").transform;
            ship.SetParent(body, false);
            Sprite c = CodeSprites.Circle;
            Color outline = new Color(0.12f, 0.05f, 0.12f);
            Color white = new Color(0.97f, 0.97f, 1f);
            const float w = 1.25f, h = 0.86f, y = 0.5f;   // cuerpo de la píldora (sin las puntas redondas)
            const float line = 0.09f;

            var tinted = new System.Collections.Generic.List<SpriteRenderer>();
            if (art != null) BuildShipArt(art, y, tinted);
            else BuildShipFigure(w, h, y, line, outline, white, tinted);

            // Cabina de vidrio sobre el caballero (por delante, transparente).
            CodeSprites.Add(ship, c, new Color(0.7f, 0.95f, 1f, 0.16f), 25, new Vector2(0.03f, 1.3f), new Vector2(1.35f, 1.35f), "Cabina");
            CodeSprites.Add(ship, CodeSprites.Ring, new Color(0.85f, 1f, 1f, 0.55f), 25, new Vector2(0.03f, 1.3f), new Vector2(1.42f, 1.42f), "CabinaOrilla");
            CodeSprites.Add(ship, c, new Color(1f, 1f, 1f, 0.45f), 26, new Vector2(0.32f, 1.68f), new Vector2(0.26f, 0.13f), "CabinaBrillo");

            // Turbina de atrás: chorro que crece cuando aceleras.
            flame = new GameObject("Turbina").transform;
            flame.SetParent(ship, false);
            flame.localPosition = new Vector3(-w * 0.5f - h * 0.45f, y, 0f);
            flameRenderer = CodeSprites.Add(flame, CodeSprites.Glow, new Color(1f, 0.85f, 0.4f, 0.9f), 5, new Vector2(-0.25f, 0f), new Vector2(0.9f, 0.5f), "Fuego");
            CodeSprites.Add(flame, CodeSprites.Glow, new Color(1f, 1f, 0.9f, 1f), 5, new Vector2(-0.1f, 0f), new Vector2(0.4f, 0.28f), "FuegoCentro");
            tintParts = tinted.ToArray();
        }

        /// <summary>
        /// Con dibujo (Art/Nave/Nave_Capsula.png, la cápsula blanca vista de lado, con la nariz a la derecha): el dibujo, y
        /// encima su mitad de atrás (la mitad izquierda del mismo dibujo) teñida del medicamento, como una cápsula de verdad.
        /// </summary>
        private void BuildShipArt(Sprite art, float y, System.Collections.Generic.List<SpriteRenderer> tinted)
        {
            const float length = 2.3f;
            SpriteRenderer hull = CodeSprites.AddArt(ship, art, length, 8, new Vector2(0f, y), "Casco");
            Rect r = art.rect;
            // La mitad izquierda, con el pivote en su orilla derecha (= el centro del dibujo completo).
            float pivotY = art.pivot.y / Mathf.Max(1f, r.height);
            Sprite half = Sprite.Create(art.texture, new Rect(r.x, r.y, r.width * 0.5f, r.height), new Vector2(1f, pivotY), art.pixelsPerUnit);
            float centerX = (0.5f - art.pivot.x / Mathf.Max(1f, r.width)) * art.bounds.size.x * hull.transform.localScale.x;
            SpriteRenderer back = CodeSprites.Add(ship, half, Color.white, 9, new Vector2(centerX, y), hull.transform.localScale, "CascoColor");
            tinted.Add(back);
            tinted.Add(CodeSprites.Add(ship, CodeSprites.Circle, Color.white, 11, new Vector2(length * 0.42f, y + 0.05f), Vector2.one * 0.16f, "Faro"));
        }

        /// <summary>La cápsula hecha con figuras (mientras no hay dibujo).</summary>
        private void BuildShipFigure(float w, float h, float y, float line, Color outline, Color white, System.Collections.Generic.List<SpriteRenderer> tinted)
        {
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            // Contorno grueso (estilo caricatura).
            CodeSprites.Add(ship, sq, outline, 7, new Vector2(0f, y), new Vector2(w, h + line * 2f), "Contorno");
            CodeSprites.Add(ship, c, outline, 7, new Vector2(-w * 0.5f, y), Vector2.one * (h + line * 2f), "ContornoAtras");
            CodeSprites.Add(ship, c, outline, 7, new Vector2(w * 0.5f, y), Vector2.one * (h + line * 2f), "ContornoNariz");

            // Mitad de atrás: color del medicamento. Mitad de enfrente: blanca (como una cápsula de verdad).
            tinted.Add(CodeSprites.Add(ship, c, white, 8, new Vector2(-w * 0.5f, y), Vector2.one * h, "Atras"));
            tinted.Add(CodeSprites.Add(ship, sq, white, 8, new Vector2(-w * 0.25f, y), new Vector2(w * 0.5f, h), "AtrasCuerpo"));
            CodeSprites.Add(ship, sq, white, 8, new Vector2(w * 0.25f, y), new Vector2(w * 0.5f, h), "Enfrente");
            CodeSprites.Add(ship, c, white, 8, new Vector2(w * 0.5f, y), Vector2.one * h, "Nariz");
            CodeSprites.Add(ship, sq, outline, 9, new Vector2(0f, y), new Vector2(0.06f, h), "Union");
            // Brillo de arriba y sombra de abajo.
            CodeSprites.Add(ship, c, new Color(1f, 1f, 1f, 0.55f), 10, new Vector2(0.15f, y + h * 0.27f), new Vector2(w * 0.9f, 0.13f), "Brillo");
            CodeSprites.Add(ship, c, new Color(0f, 0f, 0f, 0.18f), 10, new Vector2(0f, y - h * 0.3f), new Vector2(w * 1.2f, 0.18f), "Sombra");
            // Luz del frente (se pinta del medicamento).
            tinted.Add(CodeSprites.Add(ship, c, white, 11, new Vector2(w * 0.62f, y + 0.05f), Vector2.one * 0.16f, "Faro"));
        }

        private void OnMedication(MedicationData med)
        {
            if (med == null) return;
            foreach (SpriteRenderer r in tintParts)
                if (r != null) r.color = r.name == "Faro" ? Color.Lerp(med.color, Color.white, 0.4f) : med.color;
        }

        // ───────────────────────────── Cada cuadro ─────────────────────────────

        /// <summary>Hacia atrás un momento al recibir un golpe (sin aturdir).</summary>
        public void OnHurt(float direction)
        {
            hurtPush = 0.18f;
            velocity += new Vector2(-3.5f, 0f);
        }

        public void Tick(float dt)
        {
            if (!Active || dt <= 0f) return;
            PlayerInputReader input = controller.Input;
            FlightRoute route = FlightRoute.Current;
            Vector2 scroll = route != null ? route.ScrollVelocity : Vector2.zero;
            Vector2 extra = push;
            push = Vector2.zero;

            if (!controller.Health.IsAlive)
            {
                // Sin vida: la nave se apaga y se hunde despacio.
                velocity = Vector2.Lerp(velocity, scroll + new Vector2(-1f, -1.5f), 1f - Mathf.Exp(-3f * dt));
                motor.SetVelocity(velocity);
                Visuals(dt, scroll);
                return;
            }

            bool blocked = input.Blocked;
            Shrunk = !blocked && input.LockHeld;
            Vector2 move = blocked ? Vector2.zero : input.Move;
            if (move.sqrMagnitude > 1f) move.Normalize();
            // Con la cámara girada (vuelo hacia arriba), "arriba" en el control es "adelante" en el mundo.
            if (route != null) move = route.ScreenToWorld(move);

            if (Time.time > slowUntil) slowFactor = 1f;
            bool thick = slowFactor < 0.99f;
            if (boostTimer > 0f) { boostTimer -= dt; Ram(dt); }

            if (dashTimer > 0f) dashTimer -= dt;
            else if (!blocked && !thick && input.DashPressed && Time.time >= dashReadyAt)
            {
                dashDir = move.sqrMagnitude > 0.04f ? move.normalized : Vector2.right;
                dashTimer = dashTime;
                dashReadyAt = Time.time + dashCooldown;
                controller.Health.GrantInvulnerability(dashInvulnerability);
                Sfx.Effect("Jugador_Dash", 0.6f);
            }

            float top = (Shrunk ? shrunkSpeed : speed) * slowFactor;
            Vector2 target = dashTimer > 0f ? scroll + dashDir * dashSpeed : scroll + move * top + extra;
            if (hurtPush > 0f) hurtPush -= dt;
            float k = dashTimer > 0f ? 1f : 1f - Mathf.Exp(-response * dt);
            velocity = Vector2.Lerp(velocity, target, k);

            // No salirse de la pantalla: la orilla izquierda te empuja (como en Cuphead) y las demás te detienen.
            if (route != null) velocity = ClampToView(route.View, velocity, dt);
            motor.SetVelocity(velocity);

            float s = controller.Size;
            if (motor.Collider != null)
            {
                motor.Collider.size = baseBoxSize * s;
                motor.Collider.offset = baseBoxOffset * s;
            }
            // Área de daño chica y centrada (perdona los roces, como en los juegos de naves).
            if (hurtbox != null) hurtbox.SetBox(new Vector2(0.05f, 0.62f) * s, new Vector2(1.05f, 0.62f) * s);
            Visuals(dt, scroll);
        }

        private Vector2 ClampToView(Rect view, Vector2 v, float dt)
        {
            float s = controller.Size;
            float minX = view.xMin + 0.9f * s, maxX = view.xMax - 1.3f * s;
            float minY = view.yMin + 0.15f, maxY = view.yMax - 1.75f * s;
            Vector2 p = motor.Position;
            Vector2 next = p + v * dt;
            if (next.x < minX) v.x = (minX - p.x) / dt;
            if (next.x > maxX) v.x = (maxX - p.x) / dt;
            if (next.y < minY) v.y = (minY - p.y) / dt;
            if (next.y > maxY) v.y = (maxY - p.y) / dt;
            // Si ya quedó afuera (reaparecer, empujón fuerte, o algo sólido no la deja avanzar y la pantalla la
            // alcanzó), se acomoda de golpe en un lugar LIBRE. Si fue porque quedó APLASTADA contra la orilla, duele.
            if (p.x < minX - 0.5f || p.x > maxX + 0.5f || p.y < minY - 0.5f || p.y > maxY + 0.5f)
            {
                bool squashed = p.x < minX - 0.5f && controller.Health.IsAlive;
                Vector2 fixedPos = FreeSpot(new Vector2(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY)), minX, maxX, minY, maxY);
                motor.Body.position = fixedPos;
                transform.position = fixedPos;
                velocity = route != null ? route.ScrollVelocity : Vector2.zero;
                v = velocity;
                if (squashed && Time.time >= squashedAt + 1f)
                {
                    squashedAt = Time.time;
                    DamageInfo info = DamageInfo.Create(15f, DamageKind.Environment, Team.Neutral, fixedPos);
                    info.blockable = false;
                    controller.Health.ApplyDamage(info);
                    GameplayEvents.RaiseMessage("¡Te aplastó la orilla! Encógete ({fijar}) o rompe lo que te tapa el paso.");
                }
            }
            return v;
        }

        private float squashedAt = -10f;
        private FlightRoute route => FlightRoute.Current;

        /// <summary>
        /// El lugar libre más cercano para la nave (sin paredes, placas ni coágulos encima): busca hacia arriba y hacia
        /// abajo, y si no hay, un poco más adelante.
        /// </summary>
        private Vector2 FreeSpot(Vector2 wanted, float minX, float maxX, float minY, float maxY)
        {
            if (motor.Collider == null) return wanted;
            Vector2 size = motor.Collider.size * 0.95f, offset = motor.Collider.offset;
            int mask = 1 << PhysicsLayers.Ground;
            for (int step = 0; step < 6; step++)
            {
                float x = Mathf.Min(maxX, wanted.x + step * 0.8f);
                for (int k = 0; k < 24; k++)
                {
                    float dy = (k / 2 + (k % 2)) * 0.4f * (k % 2 == 0 ? 1f : -1f);
                    Vector2 c = new Vector2(x, Mathf.Clamp(wanted.y + dy, minY, maxY));
                    if (Physics2D.OverlapBox(c + offset, size, 0f, mask) == null) return c;
                }
            }
            return wanted;
        }

        /// <summary>Con adrenalina, los enemigos que tocas reciben un golpazo (no los coágulos ni las placas).</summary>
        private void Ram(float dt)
        {
            if (ramCooldown > 0f) { ramCooldown -= dt; return; }
            if (hurtbox == null) return;
            Hurtbox.QueryCircle(Team.Enemy, hurtbox.Center, 1f * controller.Size, rammed);
            bool hit = false;
            foreach (Hurtbox h in rammed)
            {
                if (h == null || h.GetComponent<FlightEnemy>() == null) continue;
                DamageInfo info = DamageInfo.Create(40f, DamageKind.Contact, Team.Player, hurtbox.Center);
                h.Damageable.ApplyDamage(info);
                hit = true;
            }
            if (hit) ramCooldown = 0.2f;
        }

        private void Visuals(float dt, Vector2 scroll)
        {
            if (body == null) return;
            Vector2 rel = velocity - scroll;
            // Se inclina al subir o bajar y se mece un poquito.
            float targetTilt = Mathf.Clamp(rel.y * 1.8f, -14f, 14f) + Mathf.Sin(Time.time * 3f) * 1.5f;
            tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-8f * dt));
            body.localRotation = Quaternion.Euler(0f, 0f, tilt);
            if (flame != null)
            {
                float thrust = Mathf.Clamp01(0.35f + rel.x * 0.12f + Heartbeat.SurgeNow * 0.4f + (IsDashing ? 1f : 0f));
                if (Boosting) thrust = 1.8f;   // adrenalina: chorro enorme
                float flicker = 1f + Mathf.Sin(Time.time * 37f) * 0.12f;
                flame.localScale = new Vector3((0.6f + thrust * 1.1f) * flicker, 0.8f + thrust * 0.3f, 1f);
                if (flameRenderer != null)
                {
                    bool alive = controller.Health.IsAlive;
                    flameRenderer.color = !alive ? new Color(0.4f, 0.4f, 0.4f, 0.3f)
                        : Boosting ? new Color(1f, 0.55f, 0.15f, 1f) : new Color(1f, 0.8f, 0.35f, 0.9f);
                }
            }
        }
    }
}
