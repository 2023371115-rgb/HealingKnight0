using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>HAZ DE LUZ (Ojo): la luz que entra por la pupila barre de un lado a otro. Si te alumbra y no estás escondido, te ven.</summary>
    public class LightBeam : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float period = 6f;
        [SerializeField] private float width = 1.6f;
        [SerializeField] private float height = 10f;

        private Transform beam;
        private float currentX;
        private static readonly System.Collections.Generic.List<LightBeam> Active = new System.Collections.Generic.List<LightBeam>();

        public void Configure(float from, float to, float cycle) { minX = from; maxX = to; period = cycle; }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        /// <summary>¿Algún haz de luz alumbra ese punto ahora? (El Toxoplasma se deslumbra con él.)</summary>
        public static bool Lights(Vector2 point)
        {
            foreach (LightBeam b in Active)
                if (b != null && b.beam != null && Mathf.Abs(point.x - b.currentX) <= b.width * 0.5f
                    && point.y >= b.transform.position.y - 0.5f && point.y <= b.transform.position.y + b.height) return true;
            return false;
        }

        private void Start()
        {
            beam = new GameObject("Haz").transform;
            beam.SetParent(transform, false);
            CodeSprites.Add(beam, CodeSprites.Square, new Color(1f, 0.97f, 0.75f, 0.28f), 45, new Vector2(0f, height * 0.5f), new Vector2(width, height));
            CodeSprites.Add(beam, CodeSprites.Square, new Color(1f, 1f, 0.9f, 0.35f), 46, new Vector2(0f, height * 0.5f), new Vector2(width * 0.4f, height));
            CodeSprites.Add(beam, CodeSprites.Circle, new Color(1f, 1f, 0.8f, 0.45f), 46, new Vector2(0f, 0.1f), new Vector2(width * 1.6f, 0.5f));
        }

        private void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time / period * Mathf.PI * 2f);
            float x = Mathf.Lerp(minX, maxX, k);
            currentX = x;
            beam.position = new Vector3(x, transform.position.y, 0f);
            PlayerController p = PlayerController.Current;
            StealthMeter m = StealthMeter.Current;
            if (p == null || m == null || Mathf.Abs(p.Position.x - x) > width * 0.5f) return;
            m.Spot(1.6f);
        }
    }
}
