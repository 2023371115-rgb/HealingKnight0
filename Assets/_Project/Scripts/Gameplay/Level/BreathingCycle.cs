using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// TODO EL PULMÓN RESPIRA. Ciclo de 5.5 s sincronizado con los alvéolos (BreathingPlatform):
    ///  - INHALA (3.5 s, mientras los alvéolos se inflan y están inflados): el aire entra y te jala HACIA ADELANTE.
    ///  - EXHALA (2 s, mientras se desinflan): el aire sale y te empuja HACIA ATRÁS (más fuerte).
    /// Solo dentro de [minX, maxX]. Detrás de un WindAnchor no te empuja. Las nubes de CO₂ se mueven con el aire.
    /// Dibuja rayitas de aire en la cámara para que se lea hacia dónde sopla.
    /// </summary>
    public class BreathingCycle : MonoBehaviour
    {
        public const float Period = 5.5f;
        public const float InhaleTime = 3.5f;

        [SerializeField] private float minX;
        [SerializeField] private float maxX = 200f;
        [SerializeField] private float inhaleSpeed = 1.6f;
        [SerializeField] private float exhaleSpeed = 3.4f;

        private Transform[] streaks;
        private int lastStage = -1;

        /// <summary>+1 inhalando (hacia adelante), -1 exhalando (hacia atrás). Para nubes y burbujas.</summary>
        public static float Flow { get; private set; }
        public static bool Inhaling => Phase < InhaleTime;
        private static float Phase => Mathf.Repeat(Time.timeSinceLevelLoad, Period);

        public void Configure(float from, float to) { minX = from; maxX = to; }

        private void OnDisable() => Flow = 0f;

        private bool Inside(PlayerController p) => p != null && p.Health.IsAlive && p.Position.x >= minX && p.Position.x <= maxX;

        private void Update()
        {
            float ph = Phase;
            // Rampa suave al cambiar (para que no dé tirón).
            float k = ph < InhaleTime ? Mathf.Clamp01(ph / 0.4f) * Mathf.Clamp01((InhaleTime - ph) / 0.3f)
                                      : -Mathf.Clamp01((ph - InhaleTime) / 0.3f) * Mathf.Clamp01((Period - ph) / 0.3f);
            Flow = k;

            int stage = ph < InhaleTime ? 0 : 1;
            PlayerController player = PlayerController.Current;
            bool inside = Inside(player);
            if (stage != lastStage)
            {
                if (lastStage >= 0 && inside) Sfx.Effect(stage == 0 ? "Alveolo_Infla" : "Alveolo_Desinfla", 0.35f);
                lastStage = stage;
            }
            DrawStreaks(inside);
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (!Inside(player) || player.Motor == null) return;
            if (WindAnchor.IsSheltered(player.Position)) return;
            float speed = Flow > 0f ? Flow * inhaleSpeed : Flow * exhaleSpeed;
            player.Motor.Body.position += new Vector2(speed * Time.fixedDeltaTime, 0f);
        }

        private void DrawStreaks(bool on)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            if (streaks == null)
            {
                streaks = new Transform[10];
                for (int i = 0; i < streaks.Length; i++)
                    streaks[i] = CodeSprites.Add(cam.transform, CodeSprites.Circle, new Color(0.8f, 1f, 1f, 0.22f), 298, Vector2.zero, new Vector2(1.6f, 0.06f), "Aire").transform;
            }
            float h = cam.orthographicSize, w = h * cam.aspect;
            float dir = Flow >= 0f ? 1f : -1f, speed = Mathf.Abs(Flow) * (Flow > 0f ? 8f : 16f);
            for (int i = 0; i < streaks.Length; i++)
            {
                Transform s = streaks[i];
                bool show = on && Mathf.Abs(Flow) > 0.2f;
                if (s.gameObject.activeSelf != show) s.gameObject.SetActive(show);
                if (!show) continue;
                Vector3 p = s.localPosition;
                if (p.z < 1f) p = new Vector3(Random.Range(-w, w), Random.Range(-h, h), 10f);
                p.x += dir * speed * Time.deltaTime;
                if (p.x > w + 1f) p.x = -w - 1f;
                if (p.x < -w - 1f) p.x = w + 1f;
                s.localPosition = p;
            }
        }
    }
}
