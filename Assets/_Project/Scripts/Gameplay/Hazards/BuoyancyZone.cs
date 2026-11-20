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
}
