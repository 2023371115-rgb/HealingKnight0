using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CONTRACCIÓN (peristalsis): las paredes del estómago se aprietan en ondas que empujan la comida hacia el píloro.
    /// En el juego es una pared gigante de músculo que te PERSIGUE: arranca cuando cruzas 'startX' y avanza hasta 'endX'.
    /// Si te alcanza, te daña y te avienta hacia adelante. No puedes regresar: hay que correr.
    /// Es un poco más lenta que el jugador corriendo, así que si no te detienes de más, la libras.
    /// Se dibuja por código (más adelante se puede cambiar por un sprite en 'visual').
    /// </summary>
    public class PeristalsisWave : MonoBehaviour
    {
        [SerializeField] private float startX = 100f;
        [SerializeField] private float endX = 125f;
        [SerializeField] private float speed = 4.1f;
        [SerializeField] private float startGap = 7f;      // a qué distancia detrás del jugador aparece
        [SerializeField] private float damage = 14f;
        [SerializeField] private float height = 12f;
        [SerializeField, TextArea] private string startMessage = "¡CONTRACCIÓN! El estómago empuja todo hacia el píloro: ¡CORRE!";
        [SerializeField] private Color tissue = new Color(0.83f, 0.33f, 0.47f, 1f);
        [SerializeField] private Color shadow = new Color(0.62f, 0.18f, 0.32f, 1f);
        [SerializeField] private Color outline = new Color(0.24f, 0.04f, 0.15f, 1f);
        [SerializeField] private Transform visual;

        private enum Phase { Waiting, Chasing, Done }
        private Phase phase = Phase.Waiting;
        private float front;            // x del frente de la pared
        private float hitCooldown, rumbleTimer;
        private Transform[] bumps;

        public void Configure(float fromX, float toX, float waveSpeed)
        {
            startX = fromX;
            endX = toX;
            speed = waveSpeed;
        }

        private void Start()
        {
            if (visual == null) visual = BuildVisual();
            visual.gameObject.SetActive(false);
        }

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            float dt = Time.deltaTime;
            switch (phase)
            {
                case Phase.Waiting:
                    if (player != null && player.Position.x > startX)
                    {
                        phase = Phase.Chasing;
                        front = player.Position.x - startGap;
                        visual.gameObject.SetActive(true);
                        GameplayEvents.RaiseMessage(startMessage);
                        Sfx.Effect("Lombriz_Ruge", 0.9f);
                        Shake(0.25f, 0.6f);
                    }
                    break;

                case Phase.Chasing:
                    front += speed * dt;
                    if (front >= endX)
                    {
                        front = endX;
                        phase = Phase.Done;
                        Sfx.Effect("Lombriz_Retumbo", 0.8f);
                    }
                    rumbleTimer -= dt;
                    if (rumbleTimer <= 0f)
                    {
                        rumbleTimer = 1.1f;
                        Sfx.EffectAt("Lombriz_Retumbo", new Vector2(front, transform.position.y), 0.5f);
                        Shake(0.08f, 0.4f);
                    }
                    PushPlayer(player, dt);
                    break;

                case Phase.Done:
                    // Se retira despacio hacia atrás (la contracción se relaja).
                    front -= dt * 1.5f;
                    if (front < endX - 12f) visual.gameObject.SetActive(false);
                    break;
            }

            if (visual.gameObject.activeSelf)
            {
                visual.position = new Vector3(front, transform.position.y, 0f);
                Wobble();
            }
        }

        private void PushPlayer(PlayerController player, float dt)
        {
            if (hitCooldown > 0f) hitCooldown -= dt;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 feet = player.Position;
            if (feet.x > front + 0.2f) return;

            // Te alcanzó: te pone adelante de la pared y te avienta.
            Rigidbody2D body = player.Motor.Body;
            body.position = new Vector2(front + 0.25f, body.position.y);
            if (hitCooldown <= 0f)
            {
                hitCooldown = 0.8f;
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Neutral, new Vector2(front - 1f, feet.y + 0.9f));
                info.blockable = false;
                info.knockback = 9f;
                player.Health.ApplyDamage(info);
                player.Motor.SetVelocity(new Vector2(9f, 6f));
                Shake(0.2f, 0.3f);
            }
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }

        // ---------- dibujo por código: una pared de carne con pliegues redondos al frente ----------

        private Transform BuildVisual()
        {
            var root = new GameObject("Contraccion").transform;
            root.SetParent(transform, false);
            Sprite square = Square(), circle = Circle();

            // Cuerpo de la pared (se extiende hacia atrás) con contorno.
            Add(root, square, outline, 22, new Vector2(-6f, height * 0.5f - 1f), new Vector2(12.2f, height + 0.2f));
            Add(root, square, tissue, 23, new Vector2(-6f, height * 0.5f - 1f), new Vector2(12f, height));
            Add(root, square, shadow, 24, new Vector2(-9f, height * 0.5f - 1f), new Vector2(6f, height));

            // Pliegues redondos al frente (se bambolean como músculo apretando).
            int n = Mathf.CeilToInt(height / 1.4f);
            bumps = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var b = new GameObject("Pliegue" + i).transform;
                b.SetParent(root, false);
                b.localPosition = new Vector3(0f, -0.6f + i * 1.4f, 0f);
                Add(b, circle, outline, 25, Vector2.zero, new Vector2(1.9f, 1.75f));
                Add(b, circle, tissue, 26, Vector2.zero, new Vector2(1.75f, 1.6f));
                Add(b, circle, new Color(1f, 0.8f, 0.85f, 0.8f), 27, new Vector2(0.2f, 0.35f), new Vector2(0.55f, 0.22f));
                bumps[i] = b;
            }
            return root;
        }

        private void Wobble()
        {
            if (bumps == null) return;
            float t = Time.time;
            for (int i = 0; i < bumps.Length; i++)
            {
                Vector3 p = bumps[i].localPosition;
                bumps[i].localPosition = new Vector3(Mathf.Sin(t * 4f + i * 0.9f) * 0.25f, p.y, 0f);
            }
        }

        private static void Add(Transform parent, Sprite sprite, Color color, int order, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Capa");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
        }

        private static Sprite square, circle;

        private static Sprite Square()
        {
            if (square != null) return square;
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return square;
        }

        private static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 64;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 30f));
                }
            tex.SetPixels(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return circle;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.5f, 0.8f);
            Gizmos.DrawLine(new Vector3(startX, transform.position.y, 0f), new Vector3(startX, transform.position.y + 6f, 0f));
            Gizmos.DrawLine(new Vector3(endX, transform.position.y, 0f), new Vector3(endX, transform.position.y + 6f, 0f));
        }
    }
}
