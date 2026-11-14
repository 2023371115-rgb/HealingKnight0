using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Burbujas de GAS: en el estómago se junta aire que tragamos al comer (por eso eructamos).
    /// Desde este punto suben burbujas grandes cada cierto tiempo; puedes pararte encima y te suben
    /// (sirven para llegar a lugares altos). Al llegar arriba revientan, así que hay que brincar a tiempo.
    /// Cada burbuja es una plataforma de un solo sentido que se mueve y te lleva (como la comida flotante).
    /// </summary>
    public class GasBubbleVent : MonoBehaviour
    {
        [SerializeField] private float interval = 2.6f;
        [SerializeField] private float riseSpeed = 1.7f;
        [SerializeField] private float riseHeight = 6f;
        [SerializeField] private float radius = 0.75f;
        [SerializeField] private int maxBubbles = 4;
        [SerializeField] private Color fill = new Color(0.9f, 1f, 0.75f, 0.45f);
        [SerializeField] private Color outline = new Color(0.12f, 0.26f, 0.08f, 1f);
        [SerializeField] private Color shine = new Color(1f, 1f, 1f, 0.9f);

        private class Bubble
        {
            public GameObject root;
            public Transform art;
            public float y, age;
            public bool active, carrying;
        }

        private readonly List<Bubble> bubbles = new List<Bubble>();
        private float timer;
        private Sprite circle, ring;

        public void Configure(float period, float height)
        {
            interval = period;
            riseHeight = height;
        }

        private void Start()
        {
            circle = MakeCircle(false);
            ring = MakeCircle(true);
            timer = 0.3f;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            timer -= dt;
            if (timer <= 0f)
            {
                timer = interval;
                Launch();
            }

            PlayerController player = PlayerController.Current;
            foreach (Bubble b in bubbles)
            {
                if (!b.active) continue;
                b.age += dt;
                float before = b.y;
                b.y += riseSpeed * dt;
                Vector3 p = b.root.transform.position;
                b.root.transform.position = new Vector3(transform.position.x + Mathf.Sin(b.age * 2.2f) * 0.15f, b.y, 0f);

                // Lleva al jugador si está parado encima.
                b.carrying = false;
                if (player != null && player.Motor != null)
                {
                    Vector2 feet = player.Position;
                    float top = before + radius * 2f;
                    if (Mathf.Abs(feet.x - p.x) <= radius + 0.1f && Mathf.Abs(feet.y - top) < 0.18f && player.Motor.Velocity.y <= 0.5f)
                    {
                        b.carrying = true;
                        player.Motor.Body.position += new Vector2(b.root.transform.position.x - p.x, b.y - before);
                    }
                }

                if (b.y - transform.position.y >= riseHeight) Pop(b);
            }
        }

        private void Update()
        {
            foreach (Bubble b in bubbles)
            {
                if (!b.active) continue;
                // Se bambolea y, al final del viaje, tiembla un poco: aviso de que va a reventar.
                float left = riseHeight - (b.y - transform.position.y);
                float shake = left < 1f ? Mathf.Sin(Time.time * 40f) * 0.05f * (1f - left) : 0f;
                float squash = 1f + Mathf.Sin(b.age * 5f) * 0.05f;
                b.art.localScale = new Vector3(radius * 2f * squash, radius * 2f / squash, 1f);
                b.art.localPosition = new Vector3(shake, radius, 0f);
            }
        }

        private void Launch()
        {
            Bubble b = bubbles.Find(x => !x.active);
            if (b == null)
            {
                if (bubbles.Count >= maxBubbles) return;
                b = Create();
                bubbles.Add(b);
            }
            b.y = transform.position.y - radius;
            b.age = 0f;
            b.active = true;
            b.root.transform.position = new Vector3(transform.position.x, b.y, 0f);
            b.root.SetActive(true);
            Sfx.EffectAt("Acido_Burbujeo", transform.position, 0.5f);
        }

        private void Pop(Bubble b)
        {
            b.active = false;
            b.root.SetActive(false);
            Sfx.EffectAt("Alveolo_Desinfla", b.root.transform.position, 0.6f);
        }

        private Bubble Create()
        {
            var root = new GameObject("BurbujaGas");
            root.transform.SetParent(transform, true);
            root.layer = PhysicsLayers.Ground;

            // Piso de un solo sentido arriba de la burbuja (el pivote del objeto es la parte de abajo de la burbuja).
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(radius * 1.4f, 0.25f);
            box.offset = new Vector2(0f, radius * 2f - 0.125f);
            box.usedByEffector = true;
            var effector = root.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            var art = new GameObject("Dibujo");
            art.transform.SetParent(root.transform, false);
            AddSprite(art.transform, ring, outline, 30, 1.06f, Vector2.zero);
            AddSprite(art.transform, circle, fill, 31, 1f, Vector2.zero);
            AddSprite(art.transform, circle, shine, 32, 0.22f, new Vector2(-0.22f, 0.22f));
            return new Bubble { root = root, art = art.transform };
        }

        private static void AddSprite(Transform parent, Sprite sprite, Color color, int order, float scale, Vector2 offset)
        {
            var go = new GameObject("Capa");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
        }

        /// <summary>Círculo de 1 unidad de diámetro; 'onlyRing' deja solo la orilla (el contorno).</summary>
        private static Sprite MakeCircle(bool onlyRing)
        {
            const int n = 64;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) * 30f);
                    if (onlyRing) a *= Mathf.Clamp01((d - 0.86f) * 30f);
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 1f, 0.6f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * riseHeight);
        }
    }
}
