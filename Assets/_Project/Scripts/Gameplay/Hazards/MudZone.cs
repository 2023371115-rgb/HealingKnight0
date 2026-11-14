using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// LODO (colon descendente): el colon absorbe el agua, así que el contenido se va haciendo espeso.
    /// Caminar en el lodo te FRENA; si te quedas parado, te vas hundiendo y te cuesta más salir.
    /// Se dibuja por código como una capa café espesa sobre el piso, con burbujas.
    /// </summary>
    public class MudZone : MonoBehaviour
    {
        [SerializeField] private float width = 14f;
        [SerializeField] private float depth = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float slow = 0.55f;
        [SerializeField, Range(0.05f, 1f)] private float stuckSlow = 0.25f;
        [SerializeField] private float stuckAfter = 1.2f;
        [SerializeField] private Color mud = new Color(0.45f, 0.3f, 0.16f, 0.95f);
        [SerializeField] private Color top = new Color(0.62f, 0.45f, 0.24f, 1f);
        [SerializeField] private Color outline = new Color(0.2f, 0.12f, 0.05f, 1f);

        private float stillTime, lastX;
        private Transform[] bubbles;

        public void Configure(float zoneWidth) => width = zoneWidth;

        private void Start() => Build();

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            if (player != null && player.Health.IsAlive)
            {
                Vector2 feet = player.Position;
                bool inside = Mathf.Abs(feet.x - transform.position.x) <= width * 0.5f && feet.y <= transform.position.y + depth + 0.05f
                              && feet.y >= transform.position.y - 0.5f;
                if (inside)
                {
                    stillTime = Mathf.Abs(feet.x - lastX) < 0.01f ? stillTime + Time.deltaTime : 0f;
                    player.ApplySlow(stillTime > stuckAfter ? stuckSlow : slow, 0.15f);
                }
                else stillTime = 0f;
                lastX = feet.x;
            }

            if (bubbles == null) return;
            float t = Time.time;
            for (int i = 0; i < bubbles.Length; i++)
            {
                float k = Mathf.Repeat(t * 0.5f + i * 0.29f, 1f);
                float s = Mathf.Sin(k * Mathf.PI) * (0.18f + 0.06f * (i % 3));
                bubbles[i].localScale = new Vector3(s, s * 0.8f, 1f);
            }
        }

        private void Build()
        {
            Sprite sq = CodeSprites.Square, c = CodeSprites.Circle;
            CodeSprites.Add(transform, sq, mud, 30, new Vector2(0f, depth * 0.5f - 0.05f), new Vector2(width, depth + 0.1f), "Lodo");
            CodeSprites.Add(transform, sq, outline, 31, new Vector2(0f, depth), new Vector2(width, 0.1f), "Orilla");
            int n = Mathf.Max(4, Mathf.CeilToInt(width / 1.1f));
            for (int i = 0; i < n; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * width / n;
                CodeSprites.Add(transform, c, outline, 31, new Vector2(x, depth), new Vector2(1.2f, 0.42f));
                CodeSprites.Add(transform, c, top, 32, new Vector2(x, depth - 0.03f), new Vector2(1.06f, 0.3f));
            }
            bubbles = new Transform[Mathf.Max(3, n / 2)];
            for (int i = 0; i < bubbles.Length; i++)
                bubbles[i] = CodeSprites.Add(transform, c, new Color(0.75f, 0.58f, 0.32f, 1f), 33,
                    new Vector2(-width * 0.5f + Mathf.Repeat(i * 0.41f + 0.1f, 1f) * width, depth + 0.05f), Vector2.one * 0.2f).transform;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.35f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * depth * 0.5f, new Vector3(width, depth, 0f));
        }
    }
}
