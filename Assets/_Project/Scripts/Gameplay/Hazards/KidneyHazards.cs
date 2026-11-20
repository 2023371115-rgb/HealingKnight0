using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// AGUA DEL RIÑÓN donde se NADA: flotas, {saltar} da una brazada hacia arriba (cerca de la superficie, un salto
    /// fuera del agua) y mantener ABAJO te hunde. Si 'salty', entre más hondo más salada está y MÁS FLOTAS (asa de Henle). El nivel del agua puede subir y
    /// bajar (túbulo colector con la hormona ADH).
    /// Adentro hay burbujas que suben, luz cerca de la superficie y el fondo más oscuro; mientras estás sumergido la
    /// música se oye "bajo el agua" (<see cref="PlayerSubmerged"/>). Con el DIURÉTICO nadas más rápido.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class BuoyancyZone : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, bottomY;
        [SerializeField] private float lowSurface = 1f, highSurface = 8f;
        [SerializeField] private bool salty;
        [SerializeField] private bool startHigh = true;
        [Tooltip("Color del agua (alfa 0 = el de siempre: amarillo, o ámbar si es salada).")]
        [SerializeField] private Color tint = new Color(0f, 0f, 0f, 0f);

        private float surface, target, leap;
        private bool stroke, holding;
        private Transform water, line, deepShade;
        private readonly System.Collections.Generic.List<Transform> bubbles = new System.Collections.Generic.List<Transform>();
        private static float lastSubmerged = -10f;

        public float Surface => surface;
        public float FromX => fromX;
        public float ToX => toX;
        public float BottomY => bottomY;

        /// <summary>¿El jugador está nadando ahorita en algún agua? (la música se oye apagada).</summary>
        public static bool PlayerSubmerged => Time.time - lastSubmerged < 0.15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => lastSubmerged = -10f;

        /// <summary>Cambia el color del agua (filtrado claro en el túbulo proximal, ámbar en el asa, amarillo en el colector).</summary>
        public void SetTint(Color color) => tint = color;

        public void Configure(float startX, float endX, float floorY, float low, float high, bool saltier, bool beginHigh)
        {
            fromX = startX; toX = endX; bottomY = floorY; lowSurface = low; highSurface = high; salty = saltier; startHigh = beginHigh;
        }

        /// <summary>Sube (true) o baja el agua.</summary>
        public void SetHigh(bool high) => target = high ? highSurface : lowSurface;

        private void Start()
        {
            surface = target = startHigh ? highSurface : lowSurface;
            float w = toX - fromX;
            water = new GameObject("Agua").transform;
            water.SetParent(transform, false);
            Color deep = tint.a > 0f ? tint : salty ? new Color(0.9f, 0.75f, 0.25f, 0.45f) : new Color(0.95f, 0.88f, 0.45f, 0.38f);
            CodeSprites.Add(water, CodeSprites.Square, deep, 30, new Vector2(w * 0.5f, 0.5f), new Vector2(w, 1f), "Agua");
            // Fondo más oscuro (lo hondo) y una franja de luz justo bajo la superficie.
            deepShade = CodeSprites.Add(transform, CodeSprites.Square, new Color(deep.r * 0.25f, deep.g * 0.2f, deep.b * 0.2f, 0.3f), 30,
                Vector2.zero, new Vector2(w, 1f), "Hondo").transform;
            Color light = new Color(Mathf.Min(1f, deep.r + 0.3f), Mathf.Min(1f, deep.g + 0.3f), Mathf.Min(1f, deep.b + 0.3f), 0.85f);
            line = CodeSprites.Add(transform, CodeSprites.Square, light, 31, Vector2.zero, new Vector2(w, 0.1f), "Superficie").transform;
            var rnd = new System.Random(Mathf.RoundToInt(fromX * 7f));
            for (int i = 0; i < Mathf.Clamp(Mathf.RoundToInt(w / 2.5f), 3, 40); i++)
            {
                float size = 0.12f + (float)rnd.NextDouble() * 0.2f;
                Transform b = CodeSprites.Add(transform, CodeSprites.Ring, new Color(1f, 1f, 1f, 0.55f), 32, Vector2.zero, Vector2.one * size, "Burbuja").transform;
                b.position = new Vector3(fromX + (float)rnd.NextDouble() * w, bottomY + (float)rnd.NextDouble() * Mathf.Max(1f, surface - bottomY), 0f);
                bubbles.Add(b);
            }
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            float h = Mathf.Max(0.01f, surface - bottomY);
            if (water != null)
            {
                water.position = new Vector3(fromX, bottomY, 0f);
                water.localScale = new Vector3(1f, h, 1f);
            }
            if (line != null) line.position = new Vector3((fromX + toX) * 0.5f, surface, 0f);
            if (deepShade != null)
            {
                deepShade.position = new Vector3((fromX + toX) * 0.5f, bottomY + h * 0.2f, 0f);
                deepShade.localScale = new Vector3(toX - fromX, h * 0.4f, 1f);
                deepShade.gameObject.SetActive(h > 2f);
            }
        }

        private void UpdateBubbles(float dt)
        {
            float h = surface - bottomY;
            for (int i = 0; i < bubbles.Count; i++)
            {
                Transform b = bubbles[i];
                Vector3 p = b.position;
                p.y += (0.6f + (i % 5) * 0.15f) * dt;
                p.x += Mathf.Sin(Time.time * 2f + i) * 0.15f * dt;
                if (p.y > surface - 0.1f) p.y = bottomY + 0.1f;
                b.position = p;
                b.gameObject.SetActive(h > 0.6f);
            }
        }

        private void Update()
        {
            surface = Mathf.MoveTowards(surface, target, Time.deltaTime * 1.6f);
            UpdateVisual();
            UpdateBubbles(Time.deltaTime);
            PlayerController player = PlayerController.Current;
            if (player != null && player.Input != null && player.Input.JumpPressed && Inside(player)) stroke = true;
        }

        private bool Inside(PlayerController p)
        {
            Vector2 c = p.Motor.Center;
            return c.x > fromX && c.x < toX && c.y < surface && c.y > bottomY - 1f;
        }

        private void OnDisable() => Release();

        /// <summary>Devuelve la gravedad normal al salir del agua.</summary>
        private void Release()
        {
            if (!holding) return;
            holding = false;
            PlayerController player = PlayerController.Current;
            if (player != null && player.Motor != null && !player.IsFlying) player.Motor.SuspendGravity = false;
        }

        private void FixedUpdate()
        {
            if (leap > 0f) leap -= Time.fixedDeltaTime;
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || player.IsFlying || !player.Health.IsAlive || leap > 0f || !Inside(player))
            {
                Release();
                stroke = false;
                return;
            }
            // Adentro del agua la gravedad la pone el agua (si no, te hundirías como piedra).
            if (!holding) { holding = true; player.Motor.SuspendGravity = true; }
            lastSubmerged = Time.time;
            bool boost = DiureticPickup.Active;
            Vector2 v = player.Motor.Velocity;
            float below = surface - player.Motor.Center.y;
            float depth01 = Mathf.Clamp01(below / Mathf.Max(1f, surface - bottomY));
            // Sin sal: flotas suave cerca de la superficie (subes con el agua). Con sal: entre más hondo, MÁS te empuja.
            float rise = salty ? Mathf.Lerp(-0.6f, 6f, depth01) : Mathf.Clamp(below * 2f - 0.6f, -0.6f, 3f);
            float down = player.Input != null ? player.Input.Move.y : 0f;
            if (down < -0.3f) rise = -4.5f;
            v.y = Mathf.Lerp(v.y, rise, 1f - Mathf.Exp(-6f * Time.fixedDeltaTime));
            if (stroke)
            {
                stroke = false;
                if (below < 1.4f)
                {
                    // Cerca de la superficie: ¡salto fuera del agua! (alcanza las orillas).
                    v.y = 16f;
                    leap = 0.35f;
                    Release();
                    Sfx.EffectAt("Jugador_Salto", player.Motor.Center, 0.6f);
                }
                else v.y = boost ? 9.5f : 7.5f; // brazada
            }
            v.x *= boost ? 0.98f : 0.92f;
            player.Motor.SetVelocity(v);
        }
    }

    /// <summary>
    /// BOTÓN que se activa a balazos (una arteriola, la hormona ADH...). Prendido brilla verde. Con 'holdTime' > 0 se
    /// apaga solo después de ese tiempo; con 0 cada balazo lo prende/apaga.
    /// </summary>
    public class ShootSwitch : MonoBehaviour
    {
        [SerializeField] private float holdTime = 6f;
        [SerializeField] private float radius = 1f;
        [SerializeField] private string label = "";

        private SpriteRenderer lamp, ring, halo;
        private float timer, cooldown;

        public bool On { get; private set; }
        public event System.Action<bool> Changed;

        public void Configure(float seconds, string text)
        {
            holdTime = seconds;
            label = text;
        }

        private void Start()
        {
            // Halo que late mientras está APAGADO, para que se vea que hay que dispararle.
            halo = CodeSprites.Add(transform, CodeSprites.Glow, new Color(1f, 0.85f, 0.4f, 0.7f), 19, Vector2.zero, Vector2.one * 3f, "Halo");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.25f, 0.1f, 0.08f), 20, Vector2.zero, Vector2.one * 1.5f, "Contorno");
            ring = CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.85f, 0.5f, 0.35f), 21, Vector2.zero, Vector2.one * 1.3f, "Valvula");
            lamp = CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.4f, 0.2f, 0.15f), 22, Vector2.zero, Vector2.one * 0.7f, "Luz");
            if (!string.IsNullOrEmpty(label))
                WorldText.Create(transform, label, new Vector2(0f, 1.2f), new Vector2(3f, 0.6f), Color.white, 23, null, 3f);
        }

        private void Set(bool on)
        {
            if (On == on) return;
            On = on;
            lamp.color = on ? new Color(0.5f, 1f, 0.55f) : new Color(0.4f, 0.2f, 0.15f);
            ring.color = on ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.85f, 0.5f, 0.35f);
            Sfx.EffectAt("Recoger_Vida", transform.position, 0.5f);
            Changed?.Invoke(on);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (cooldown > 0f) cooldown -= dt;
            if (halo != null)
            {
                halo.enabled = !On || holdTime <= 0f;
                halo.transform.localScale = Vector3.one * (2.6f + 0.5f * Mathf.Sin(Time.time * 5f));
            }
            if (On && holdTime > 0f)
            {
                timer -= dt;
                lamp.transform.localScale = Vector3.one * (0.7f + 0.15f * Mathf.Sin(Time.time * 10f));
                if (timer <= 0f) Set(false);
            }
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile p = Projectile.Flying[i];
                if (p == null || p.SourceTeam != Team.Player) continue;
                if (((Vector2)p.Position - (Vector2)transform.position).sqrMagnitude > radius * radius) continue;
                p.Consume();
                if (cooldown > 0f) continue;
                cooldown = 0.25f;
                if (holdTime > 0f) { timer = holdTime; Set(true); }
                else Set(!On);
            }
        }
    }

    /// <summary>
    /// Corriente lateral del asa de Henle (idea del equipo): empuja al jugador y pulsa, para crear ventanas de cruce.
    /// Para corrientes con dibujo y en cualquier dirección, ver <see cref="WaterCurrent"/>.
    /// </summary>
    public class HenleCurrent : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, minY, maxY;
        [SerializeField] private float direction = 1f, strength = 2f, pulseSpeed = 1.7f;
        private float phase;

        public void Configure(float startX, float endX, float bottom, float top, float push, float pulseSpeed)
        {
            fromX = startX; toX = endX; minY = bottom; maxY = top;
            direction = Mathf.Sign(push); strength = Mathf.Abs(push); this.pulseSpeed = pulseSpeed; phase = startX * 0.31f;
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || player.IsFlying || !player.Health.IsAlive) return;
            Vector2 center = player.Motor.Center;
            if (center.x < fromX || center.x > toX || center.y < minY || center.y > maxY) return;

            // La corriente alterna entre empujones y pausas para que el jugador pueda planear el cruce.
            float pulse = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phase));
            Vector2 velocity = player.Motor.Velocity;
            velocity.x = Mathf.Clamp(velocity.x + direction * strength * pulse * Time.fixedDeltaTime, -8f, 8f);
            player.Motor.SetVelocity(velocity);
        }
    }

    /// <summary>Une un botón con un agua: prendido sube, apagado baja (la hormona ADH en el túbulo colector).</summary>
    public class SwitchToWater : MonoBehaviour
    {
        [SerializeField] private ShootSwitch button;
        [SerializeField] private BuoyancyZone water;

        public void Configure(ShootSwitch sw, BuoyancyZone zone) { button = sw; water = zone; }

        private void Start()
        {
            if (button != null) button.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (button != null) button.Changed -= OnChanged;
        }

        private void OnChanged(bool on)
        {
            if (water != null) water.SetHigh(on);
            GameplayEvents.RaiseMessage(on ? "¡ADH! El cuerpo GUARDA agua: el nivel sube." : "Sin ADH el agua se va (por eso orinas más si tomas mucha agua).");
        }
    }

    /// <summary>
    /// GLOMÉRULO: un "colador" de vasitos que tapa el paso. Para filtrar hace falta PRESIÓN: con los dos botones
    /// (arteriolas) prendidos a la vez, se abre unos segundos y la presión te EMPUJA a través.
    /// </summary>
    public class GlomerulusFilter : MonoBehaviour
    {
        [SerializeField] private ShootSwitch afferent, efferent;
        [SerializeField] private float height = 10f;
        [SerializeField] private float openTime = 4f;

        private BoxCollider2D wall;
        private Transform art;
        private float open;
        private bool told;

        public void Configure(ShootSwitch a, ShootSwitch b, float wallHeight)
        {
            afferent = a; efferent = b; height = wallHeight;
        }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(1.4f, height);
            wall.offset = new Vector2(0f, height * 0.5f);
            art = new GameObject("Colador").transform;
            art.SetParent(transform, false);
            for (float y = 0.5f; y < height; y += 0.9f)
                for (int c = -1; c <= 1; c += 2)
                {
                    CodeSprites.Add(art, CodeSprites.Ring, new Color(0.9f, 0.3f, 0.35f), 10, new Vector2(c * 0.35f, y), new Vector2(0.8f, 0.8f), "Asa");
                }
            CodeSprites.Add(art, CodeSprites.Square, new Color(0.55f, 0.12f, 0.18f, 0.7f), 9, new Vector2(0f, height * 0.5f), new Vector2(1.4f, height), "Membrana");
        }

        private void Update()
        {
            if (wall == null) return;
            float dt = Time.deltaTime;
            bool pressure = afferent != null && efferent != null && afferent.On && efferent.On;
            if (pressure && open <= 0f)
            {
                open = openTime;
                Sfx.Effect("Nave_Despegue", 0.6f);
                GameplayEvents.RaiseMessage("¡PRESIÓN! El glomérulo FILTRA: ¡pasa ya!");
            }
            if (open > 0f)
            {
                open -= dt;
                // La presión te empuja hacia adelante a través del colador.
                PlayerController player = PlayerController.Current;
                if (player != null && Mathf.Abs(player.Position.x - transform.position.x) < 3f && player.Position.x < transform.position.x + 1f)
                    player.Motor.Body.position += new Vector2(6f * dt, 0f);
            }
            bool isOpen = open > 0f;
            wall.enabled = !isOpen;
            if (art != null) art.localScale = new Vector3(isOpen ? 0.35f : 1f, 1f, 1f);

            if (!told)
            {
                PlayerController p = PlayerController.Current;
                if (p != null && Mathf.Abs(p.Position.x - transform.position.x) < 12f)
                {
                    told = true;
                    GameplayEvents.RaiseMessage("GLOMÉRULO: un colador de vasitos. Para filtrar la sangre hace falta PRESIÓN: dispárale a las DOS " +
                        "arteriolas (los botones) y cuando las dos brillen, el colador se abre.");
                }
            }
        }
    }

    /// <summary>
    /// PIEDRA DEL RIÑÓN que rueda detrás de ti por el uréter: cuando pasas 'startX' aparece y te persigue. Si te
    /// alcanza duele y te avienta. Se rompe al llegar a 'endX'.
    /// </summary>
    public class RollingStoneChase : MonoBehaviour
    {
        [SerializeField] private float startX, endX, floorY;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private float damage = 16f;

        private Transform stone;
        private float x, spin, hurtCooldown;
        private bool running, done;

        public void Configure(float fromX, float toX, float groundY, float chaseSpeed)
        {
            startX = fromX; endX = toX; floorY = groundY; speed = chaseSpeed;
        }

        private void Start()
        {
            stone = new GameObject("Piedra").transform;
            stone.SetParent(transform, false);
            CodeSprites.Add(stone, CodeSprites.Circle, new Color(0.3f, 0.22f, 0.1f), 40, Vector2.zero, Vector2.one * radius * 2.1f, "Contorno");
            CodeSprites.Add(stone, CodeSprites.Circle, new Color(0.82f, 0.7f, 0.42f), 41, Vector2.zero, Vector2.one * radius * 2f, "Piedra");
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51f * Mathf.Deg2Rad;
                CodeSprites.Add(stone, CodeSprites.Circle, new Color(0.65f, 0.55f, 0.3f), 42, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * 0.6f, Vector2.one * 0.6f, "Cristal");
            }
            stone.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (done || stone == null) return;
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            if (!running)
            {
                if (player.Position.x < startX) return;
                running = true;
                x = startX - 10f;
                stone.gameObject.SetActive(true);
                GameplayEvents.RaiseMessage("¡Una PIEDRA del riñón (un cálculo) rueda por el URÉTER detrás de ti! ¡CORRE!");
            }
            if (player.Position.x < x - 4f) x = player.Position.x - 10f;
            x += speed * dt;
            spin -= speed * dt / radius * Mathf.Rad2Deg;
            stone.position = new Vector3(x, floorY + radius, 0f);
            stone.localRotation = Quaternion.Euler(0f, 0f, spin);
            if (x > endX)
            {
                done = true;
                stone.gameObject.SetActive(false);
                Sfx.EffectAt("Coagulo_Disuelve", new Vector2(x, floorY), 1f);
                GameplayEvents.RaiseMessage("¡La piedra se atoró y se hizo pedazos! (Por eso duelen tanto los cálculos: tapan el uréter.)");
                return;
            }
            if (hurtCooldown > 0f) { hurtCooldown -= dt; return; }
            if (!player.Health.IsAlive) return;
            if (Vector2.Distance(player.Motor.Center, stone.position) < radius + 0.4f)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, stone.position);
                info.knockback = 10f;
                player.Health.ApplyDamage(info);
                hurtCooldown = 1f;
            }
        }
    }
}
