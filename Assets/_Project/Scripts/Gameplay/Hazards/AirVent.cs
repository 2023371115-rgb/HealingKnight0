using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CORRIENTE DE AIRE hacia arriba (un bronquio que sopla): cada cierto tiempo echa un chorro.
    /// Si estás dentro de la columna cuando sopla, te lanza muy alto (como un alvéolo inflado, pero más).
    /// Avisa medio segundo antes con burbujitas.
    /// </summary>
    public class AirVent : MonoBehaviour
    {
        [SerializeField] private float width = 1.6f;
        [SerializeField] private float reach = 3f;            // qué tan arriba de la boca te agarra
        [SerializeField] private float launchHeight = 6.5f;
        [SerializeField] private float period = 3f;
        [SerializeField] private float offset;
        [SerializeField] private float blowTime = 0.9f;
        [SerializeField] private Color color = new Color(0.8f, 0.95f, 1f, 0.55f);

        private float time;
        private Transform[] puffs;
        private bool launchedThisBlow;

        public void Configure(float ventWidth, float height, float cycle, float phase)
        {
            width = ventWidth;
            launchHeight = height;
            period = cycle;
            offset = phase;
        }

        private float Phase => Mathf.Repeat(time, period);
        private bool Blowing => Phase < blowTime;
        private bool Warning => Phase > period - 0.6f;

        private void Start()
        {
            time = offset;
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(transform, c, new Color(0.15f, 0.1f, 0.3f, 1f), -20, new Vector2(0f, -0.05f), new Vector2(width + 0.4f, 0.5f));
            CodeSprites.Add(transform, c, new Color(0.55f, 0.45f, 0.85f, 1f), -19, new Vector2(0f, 0f), new Vector2(width + 0.2f, 0.34f));
            CodeSprites.Add(transform, c, new Color(0.08f, 0.04f, 0.15f, 1f), -18, new Vector2(0f, 0.02f), new Vector2(width * 0.8f, 0.18f));
            puffs = new Transform[8];
            for (int i = 0; i < puffs.Length; i++)
                puffs[i] = CodeSprites.Add(transform, c, color, 30, Vector2.zero, new Vector2(0.5f, 0.5f), "Soplido").transform;
        }

        private void Update()
        {
            time += Time.deltaTime;
            float ph = Phase;
            for (int i = 0; i < puffs.Length; i++)
            {
                Transform p = puffs[i];
                if (Blowing)
                {
                    float k = Mathf.Repeat(ph * 3f + i / (float)puffs.Length, 1f);
                    p.localPosition = new Vector3(Mathf.Sin(i * 2.3f + k * 6f) * width * 0.35f, k * (launchHeight + 1f), 0f);
                    p.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, k);
                    p.gameObject.SetActive(true);
                }
                else if (Warning)
                {
                    p.localPosition = new Vector3((i - 3.5f) * width * 0.12f, 0.2f + Mathf.Repeat(time * 2f + i * 0.3f, 0.6f), 0f);
                    p.localScale = Vector3.one * 0.35f;
                    p.gameObject.SetActive(i % 2 == 0);
                }
                else p.gameObject.SetActive(false);
            }

            if (!Blowing) { launchedThisBlow = false; return; }
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive || launchedThisBlow) return;
            Vector2 f = player.Position, me = transform.position;
            if (Mathf.Abs(f.x - me.x) <= width * 0.5f + 0.2f && f.y >= me.y - 0.3f && f.y <= me.y + reach)
            {
                launchedThisBlow = true;
                player.Bounce(launchHeight, 15.8f * Mathf.Sqrt(launchHeight / 4f));
                Sfx.Effect("Aire_Impulso", 0.9f);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.7f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * reach * 0.5f, new Vector3(width, reach, 0f));
        }
    }
}
