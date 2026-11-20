using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Líquido que SUBE y te persigue (colon ascendente: el contenido del intestino sube empujado desde el ciego).
    /// Empieza a subir cuando entras a la zona y empiezas a trepar; si te alcanza, te quema y te frena.
    /// Si sales de la zona o mueres, se vacía para que al regresar empiece de nuevo.
    /// Se dibuja por código (líquido turbio con olas y burbujas), enfrente de los personajes.
    /// </summary>
    public class RisingFlood : MonoBehaviour
    {
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 14f;
        [SerializeField] private float topY = 15f;
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float startHeight = 2f;      // empieza cuando el jugador sube de aquí
        [SerializeField] private float damage = 8f;
        [SerializeField] private float damageInterval = 0.6f;
        [SerializeField, TextArea] private string message = "¡El contenido del colon sube! Trepa rápido.";
        [SerializeField] private Color deep = new Color(0.42f, 0.36f, 0.12f, 0.92f);
        [SerializeField] private Color surface = new Color(0.66f, 0.58f, 0.24f, 0.9f);
        [SerializeField] private Color outline = new Color(0.2f, 0.15f, 0.04f, 1f);

        private float level, hitTimer;
        private bool rising, warned;
        private Transform body, top;
        private Transform[] waves, bubbles;

        public void Configure(float fromX, float toX, float maxHeight, float riseSpeed)
        {
            minX = fromX;
            maxX = toX;
            topY = maxHeight;
            speed = riseSpeed;
        }

        private float BaseY => transform.position.y;

        private void Start()
        {
            level = BaseY;
            Build();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health != null && player.Health.IsAlive;
            bool inside = alive && player.Position.x >= minX && player.Position.x <= maxX;

            if (!rising && inside && player.Position.y > BaseY + startHeight)
            {
                rising = true;
                if (!warned) { warned = true; GameplayEvents.RaiseMessage(message); }
                Sfx.Effect("Lombriz_Retumbo", 0.7f);
            }
            if (rising && (!inside || !alive)) rising = false;

            level = rising ? Mathf.Min(topY, level + speed * dt) : Mathf.Max(BaseY, level - speed * 4f * dt);

            if (inside && player.Position.y < level - 0.1f)
            {
                player.ApplySlow(0.6f, 0.2f);
                hitTimer -= dt;
                if (hitTimer <= 0f)
                {
                    hitTimer = damageInterval;
                    DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, player.Position);
                    info.blockable = false;
                    player.Health.ApplyDamage(info);
                    Sfx.Effect("Acido_Quema", 0.6f);
                }
            }
            else hitTimer = 0f;

            Draw();
        }

        private void Draw()
        {
            float w = maxX - minX;
            float h = Mathf.Max(0.01f, level - BaseY + 1f);
            float cx = (minX + maxX) * 0.5f;
            body.position = new Vector3(cx, BaseY - 1f + h * 0.5f, 0f);
            body.localScale = new Vector3(w, h, 1f);
            top.position = new Vector3(cx, level, 0f);
            float t = Time.time;
            for (int i = 0; i < waves.Length; i++)
            {
                float x = minX + (i + 0.5f) * w / waves.Length;
                waves[i].position = new Vector3(x, level + Mathf.Sin(t * 2.5f + i * 1.3f) * 0.08f, 0f);
            }
            for (int i = 0; i < bubbles.Length; i++)
            {
                float k = Mathf.Repeat(t * 0.4f + i * 0.31f, 1f);
                float x = minX + Mathf.Repeat(i * 0.37f, 1f) * w;
                bubbles[i].position = new Vector3(x, Mathf.Lerp(BaseY - 0.5f, level - 0.2f, k), 0f);
                bubbles[i].gameObject.SetActive(level > BaseY + 0.4f);
            }
        }

        private void Build()
        {
            Sprite sq = CodeSprites.Square, c = CodeSprites.Circle;
            body = CodeSprites.Add(transform, sq, deep, 30, Vector2.zero, Vector2.one, "Liquido").transform;
            top = new GameObject("Superficie").transform;
            top.SetParent(transform, false);
            float w = maxX - minX;
            CodeSprites.Add(top, sq, outline, 31, new Vector2(0f, 0.05f), new Vector2(w, 0.14f));
            CodeSprites.Add(top, sq, surface, 32, new Vector2(0f, -0.12f), new Vector2(w, 0.22f));
            int n = Mathf.Max(3, Mathf.CeilToInt(w / 1.2f));
            waves = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var wv = new GameObject("Ola").transform;
                wv.SetParent(transform, false);
                CodeSprites.Add(wv, c, outline, 31, Vector2.zero, new Vector2(1.36f, 0.5f));
                CodeSprites.Add(wv, c, surface, 32, new Vector2(0f, -0.04f), new Vector2(1.2f, 0.38f));
                waves[i] = wv;
            }
            bubbles = new Transform[8];
            for (int i = 0; i < bubbles.Length; i++)
            {
                var b = new GameObject("Burbuja").transform;
                b.SetParent(transform, false);
                CodeSprites.Add(b, c, new Color(1f, 0.95f, 0.7f, 0.7f), 33, Vector2.zero, Vector2.one * (0.15f + 0.07f * (i % 3)));
                bubbles[i] = b;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.7f, 0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(new Vector3((minX + maxX) * 0.5f, (transform.position.y + topY) * 0.5f, 0f), new Vector3(maxX - minX, topY - transform.position.y, 0f));
        }
    }
}
