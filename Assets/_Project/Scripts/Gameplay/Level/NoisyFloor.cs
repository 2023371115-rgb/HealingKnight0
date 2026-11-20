using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// RETINA (Ojo): conos y bastones, las células que captan la luz. Si pisas uno apagado se ENCIENDE (te delata un
    /// poco: sube la inflamación). Pasa por encima saltando o por las plataformas.
    /// </summary>
    public class NoisyFloor : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float noise = 0.12f;

        private Transform[] cells;
        private float[] lit;

        public void Configure(float from, float to) { minX = from; maxX = to; }

        private void Start()
        {
            int n = Mathf.Max(1, Mathf.RoundToInt((maxX - minX) / 0.55f));
            cells = new Transform[n];
            lit = new float[n];
            for (int i = 0; i < n; i++)
            {
                bool cone = i % 3 == 0;   // conos (color) y bastones (blanco y negro)
                Transform t = new GameObject(cone ? "Cono" : "Baston").transform;
                t.SetParent(transform, false);
                t.position = new Vector3(minX + (i + 0.5f) * (maxX - minX) / n, transform.position.y, 0f);
                CodeSprites.Add(t, CodeSprites.Square, new Color(0.25f, 0.15f, 0.3f), -30, new Vector2(0f, 0.3f), new Vector2(0.24f, cone ? 0.6f : 0.75f));
                CodeSprites.Add(t, CodeSprites.Circle, cone ? new Color(0.9f, 0.5f, 0.3f) : new Color(0.75f, 0.75f, 0.85f), -29, new Vector2(0f, cone ? 0.62f : 0.76f), new Vector2(0.3f, 0.3f));
                cells[i] = t;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            int step = -1;
            if (p != null && p.Motor.IsGrounded && p.Position.x >= minX && p.Position.x <= maxX && p.Position.y < transform.position.y + 0.4f)
                step = Mathf.Clamp(Mathf.FloorToInt((p.Position.x - minX) / (maxX - minX) * cells.Length), 0, cells.Length - 1);
            for (int i = 0; i < cells.Length; i++)
            {
                bool near = step >= 0 && Mathf.Abs(i - step) <= 1;
                if (near && lit[i] <= 0f)
                {
                    lit[i] = 3f;
                    if (i == step) { StealthMeter.Current?.Noise(noise); Sfx.EffectAt("Piso_Ruidoso", cells[i].position, 0.5f); }
                }
                if (lit[i] > 0f) lit[i] -= dt;
                float k = Mathf.Clamp01(lit[i]);
                cells[i].localScale = Vector3.one * (1f + k * 0.25f);
                foreach (SpriteRenderer r in cells[i].GetComponentsInChildren<SpriteRenderer>())
                    if (r.sortingOrder == -29) r.color = Color.Lerp(i % 3 == 0 ? new Color(0.9f, 0.5f, 0.3f) : new Color(0.75f, 0.75f, 0.85f), new Color(1f, 1f, 0.6f), k);
            }
        }
    }
}
