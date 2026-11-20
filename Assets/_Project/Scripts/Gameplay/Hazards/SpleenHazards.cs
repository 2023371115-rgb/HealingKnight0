using System.Collections.Generic;
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
    /// BANDA DE GLÓBULOS (pulpa roja del bazo): el piso es un río lento de glóbulos rojos que te lleva hacia un lado
    /// (como banda transportadora). Ir en contra cuesta; a favor vas rapidísimo.
    /// Biología: en la pulpa roja la sangre pasa despacito para que el bazo revise cada glóbulo.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BloodConveyor : MonoBehaviour
    {
        [SerializeField] private float minX, maxX = 20f, floorY;
        [SerializeField] private float pushSpeed = 3f;
        [SerializeField] private int direction = 1;

        private readonly List<Transform> discs = new List<Transform>();
        private float offset;

        public void Configure(float from, float to, float groundY, float push, int dir)
        {
            minX = from; maxX = to; floorY = groundY; pushSpeed = push; direction = dir >= 0 ? 1 : -1;
        }

        private void Start()
        {
            // Glóbulos acostados que ruedan sobre el piso.
            for (float x = minX + 0.4f; x < maxX; x += 1.1f)
            {
                Transform d = new GameObject("Globulo").transform;
                d.SetParent(transform, false);
                CodeSprites.Add(d, CodeSprites.Circle, new Color(0.35f, 0.02f, 0.06f), -58, Vector2.zero, new Vector2(1f, 0.42f), "Contorno");
                CodeSprites.Add(d, CodeSprites.Circle, new Color(0.86f, 0.18f, 0.22f), -57, Vector2.zero, new Vector2(0.9f, 0.34f), "Disco");
                CodeSprites.Add(d, CodeSprites.Circle, new Color(0.65f, 0.08f, 0.12f), -56, Vector2.zero, new Vector2(0.4f, 0.12f), "Centro");
                discs.Add(d);
            }
            // Flechitas en el piso que dicen hacia dónde va.
            for (float x = minX + 1.5f; x < maxX - 1f; x += 4f)
                for (int s = -1; s <= 1; s += 2)
                {
                    SpriteRenderer a = CodeSprites.Add(transform, CodeSprites.Square, new Color(1f, 0.8f, 0.8f, 0.5f), -55,
                        new Vector2(x - transform.position.x, floorY - transform.position.y - 0.25f + s * 0.07f), new Vector2(0.32f, 0.06f), "Flecha");
                    a.transform.localRotation = Quaternion.Euler(0f, 0f, s * 35f * direction);
                }
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || !player.Health.IsAlive) return;
            Vector2 f = player.Position;
            if (f.x >= minX && f.x <= maxX && f.y < floorY + 0.3f && player.Motor.IsGrounded)
                player.Motor.Body.position += new Vector2(direction * pushSpeed * Time.fixedDeltaTime, 0f);
        }

        private void Update()
        {
            offset = Mathf.Repeat(offset + pushSpeed * Time.deltaTime, 1.1f);
            for (int i = 0; i < discs.Count; i++)
            {
                float x = minX + 0.4f + i * 1.1f + direction * offset;
                if (x > maxX) x -= maxX - minX;
                if (x < minX) x += maxX - minX;
                discs[i].position = new Vector3(x, floorY + 0.12f, 0f);
                discs[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + i) * 6f);
            }
        }
    }

    /// <summary>
    /// CORDÓN DEL BAZO (ranura): una pared de células muy juntas que solo deja pasar a las células flexibles. Se abre
    /// un momentito cada 'period' segundos (antes brilla): cruza con DASH justo cuando se abra. Si se cierra contigo
    /// adentro, te escupe para atrás.
    /// Biología: los glóbulos rojos viejos y tiesos no caben por las ranuras y ahí los atrapan los macrófagos.
    /// </summary>
    public class SplenicSlit : MonoBehaviour
    {
        [SerializeField] private float height = 9f;
        [SerializeField] private float thickness = 2.2f;
        [SerializeField] private float period = 2.4f;
        [SerializeField] private float openTime = 0.5f;
        [SerializeField] private float phase;
        [SerializeField] private float damage = 8f;

        private BoxCollider2D wall;
        private Transform left, right, glow;
        private SpriteRenderer glowRenderer;
        private float t;
        private bool wasOpen, told;

        public void Configure(float wallHeight, float wallThickness, float cycle, float window, float startPhase)
        {
            height = wallHeight; thickness = wallThickness; period = cycle; openTime = window; phase = startPhase;
        }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(thickness, height);
            wall.offset = new Vector2(0f, height * 0.5f);
            left = Half("Izquierda", -1f);
            right = Half("Derecha", 1f);
            glow = CodeSprites.Add(transform, CodeSprites.Glow, new Color(1f, 0.9f, 0.6f, 0f), 2, new Vector2(0f, height * 0.5f), new Vector2(1.4f, height), "Brillo").transform;
            glowRenderer = glow.GetComponent<SpriteRenderer>();
            t = phase;
        }

        /// <summary>Media pared: una columna de células (círculos rojo oscuro) apiladas.</summary>
        private Transform Half(string name, float side)
        {
            var h = new GameObject(name).transform;
            h.SetParent(transform, false);
            float w = thickness * 0.5f;
            CodeSprites.Add(h, CodeSprites.Square, new Color(0.38f, 0.06f, 0.12f), 0, new Vector2(side * w * 0.5f, height * 0.5f), new Vector2(w, height), "Relleno");
            for (float y = 0.4f; y < height; y += 0.8f)
            {
                CodeSprites.Add(h, CodeSprites.Circle, new Color(0.25f, 0.03f, 0.08f), 1, new Vector2(side * w * 0.5f, y), new Vector2(w * 1.05f, 0.85f), "Contorno");
                CodeSprites.Add(h, CodeSprites.Circle, new Color(0.7f, 0.2f, 0.3f), 1, new Vector2(side * w * 0.5f, y), new Vector2(w * 0.9f, 0.7f), "Celula");
            }
            return h;
        }

        private void Update()
        {
            if (wall == null) return;
            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Repeat(t, period);
            bool open = k < openTime;
            // Brilla medio segundo antes de abrirse (aviso del ritmo).
            float warn = Mathf.Clamp01(1f - (period - k) / 0.6f);
            if (glowRenderer != null) glowRenderer.color = new Color(1f, 0.9f, 0.6f, open ? 0.8f : warn * 0.5f);
            float gap = open ? 0.45f : 0f;
            if (left != null) left.localPosition = Vector3.Lerp(left.localPosition, new Vector3(-gap, 0f, 0f), 1f - Mathf.Exp(-25f * dt));
            if (right != null) right.localPosition = Vector3.Lerp(right.localPosition, new Vector3(gap, 0f, 0f), 1f - Mathf.Exp(-25f * dt));
            wall.enabled = !open;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            float dx = player.Position.x - transform.position.x;
            if (!told && Mathf.Abs(dx) < 7f)
            {
                told = true;
                GameplayEvents.RaiseMessage("CORDONES del bazo: ranuras por donde solo pasan las células flexibles. Cuando BRILLE y se abra, ¡cruza con DASH ({dash})!");
            }
            // Se cerró contigo adentro: te escupe hacia atrás.
            if (wasOpen && !open && Mathf.Abs(dx) < thickness * 0.5f + 0.3f && player.Position.y < transform.position.y + height)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, transform.position);
                info.knockback = 0f;
                player.Health.ApplyDamage(info);
                Vector2 back = new Vector2(transform.position.x - thickness * 0.5f - 0.8f, player.Position.y);
                player.Motor.Teleport(back);
            }
            wasOpen = open;
        }
    }

    /// <summary>
    /// MACRÓFAGO DEL BAZO que te persigue: cuando pasas 'startX' aparece atrás de ti y avanza (más lento que corriendo).
    /// Si te alcanza te muerde y te avienta hacia adelante. Se va al llegar a 'endX'.
    /// Biología: los macrófagos del bazo se comen a los glóbulos rojos viejos y a lo que se queda atorado.
    /// </summary>
    public class MacrophageChase : MonoBehaviour
    {
        [SerializeField] private float startX, endX;
        [SerializeField] private float speed = 4.2f;
        [SerializeField] private float damage = 14f;
        [SerializeField] private float floorY;

        private bool running, done;
        private float x, hurtCooldown;
        private Transform art, mouth;

        public void Configure(float fromX, float toX, float groundY, float chaseSpeed)
        {
            startX = fromX; endX = toX; floorY = groundY; speed = chaseSpeed;
        }

        private void Start()
        {
            art = new GameObject("Macrofago").transform;
            art.SetParent(transform, false);
            Color edge = new Color(0.4f, 0.3f, 0.2f), body = new Color(0.95f, 0.85f, 0.7f);
            CodeSprites.Add(art, CodeSprites.Circle, edge, 40, new Vector2(0f, 2.2f), new Vector2(4.6f, 4.4f), "Contorno");
            CodeSprites.Add(art, CodeSprites.Circle, body, 41, new Vector2(0f, 2.2f), new Vector2(4.3f, 4.1f), "Cuerpo");
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 72f + 30f) * Mathf.Deg2Rad;
                Vector2 p = new Vector2(Mathf.Cos(a) * 2f, 2.2f + Mathf.Sin(a) * 1.9f);
                CodeSprites.Add(art, CodeSprites.Circle, edge, 40, p, Vector2.one * 1.5f, "PataContorno");
                CodeSprites.Add(art, CodeSprites.Circle, body, 41, p, Vector2.one * 1.3f, "Pata");
            }
            CodeSprites.Add(art, CodeSprites.Circle, new Color(0.6f, 0.4f, 0.7f), 42, new Vector2(-0.8f, 1.6f), new Vector2(1.3f, 1f), "Nucleo");
            // Glóbulos viejos que ya se comió (adentro).
            for (int i = 0; i < 3; i++)
                CodeSprites.Add(art, CodeSprites.Circle, new Color(0.6f, 0.25f, 0.2f, 0.8f), 42, new Vector2(-0.4f + i * 0.6f, 2.8f - i * 0.4f), new Vector2(0.7f, 0.3f), "GloboViejo");
            foreach (float ex in new[] { 0.6f, 1.4f })
            {
                CodeSprites.Add(art, CodeSprites.Circle, Color.white, 43, new Vector2(ex, 3.1f), new Vector2(0.55f, 0.65f), "Ojo");
                CodeSprites.Add(art, CodeSprites.Circle, Color.black, 44, new Vector2(ex + 0.1f, 3.05f), Vector2.one * 0.25f, "Pupila");
            }
            mouth = CodeSprites.Add(art, CodeSprites.Circle, new Color(0.35f, 0.05f, 0.1f), 43, new Vector2(1.3f, 2f), new Vector2(1.1f, 0.5f), "Boca").transform;
            art.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (done || art == null) return;
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            if (!running)
            {
                if (player.Position.x < startX) return;
                running = true;
                x = startX - 9f;
                art.gameObject.SetActive(true);
                GameplayEvents.RaiseMessage("¡Un MACRÓFAGO del bazo! Se come a los glóbulos viejos... y a lo que se atora. ¡CORRE!");
                Sfx.Effect("Colmena_Enjambre", 0.6f);
            }
            // Si te quedas muy atrás (reapareciste), espera.
            if (player.Position.x < x - 3f) x = player.Position.x - 9f;
            x += speed * dt;
            art.position = new Vector3(x, floorY, 0f);
            art.localScale = new Vector3(1f + Mathf.Sin(Time.time * 5f) * 0.04f, 1f + Mathf.Cos(Time.time * 5f) * 0.04f, 1f);
            if (mouth != null) mouth.localScale = new Vector3(1.1f, 0.5f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.6f, 1f);
            if (x > endX)
            {
                done = true;
                art.gameObject.SetActive(false);
                GameplayEvents.RaiseMessage("Se quedó atrás, comiéndose glóbulos viejos. ¡Uf!");
                return;
            }
            if (hurtCooldown > 0f) { hurtCooldown -= dt; return; }
            if (!player.Health.IsAlive) return;
            if (player.Position.x < x + 2.2f && player.Position.x > x - 2.4f && player.Position.y < floorY + 4.4f)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, new Vector2(x, floorY + 2f));
                info.knockback = 9f;
                player.Health.ApplyDamage(info);
                hurtCooldown = 1f;
            }
        }
    }
}
