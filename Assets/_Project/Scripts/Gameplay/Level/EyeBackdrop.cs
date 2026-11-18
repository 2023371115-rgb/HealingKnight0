using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// El fondo del OJO está vivo:
    ///  - El IRIS (con la pupila de luz) mira hacia donde vas y de vez en cuando voltea a otro lado. Si la inflamación
    ///    sube se pone nervioso (tiembla).
    ///  - El ojo PARPADEA cada ~10 s: los párpados cierran la pantalla un momento. Mientras está cerrado NADIE TE VE
    ///    (vigilantes ni haces de luz): ¡aprovecha para cruzar! Biología: parpadeamos unas 15 veces por minuto para
    ///    mantener el ojo húmedo y limpio.
    /// Va en el objeto "Fondo" (LevelBackdrop) del nivel del Ojo; 'iris' es un hijo con el dibujo del iris.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public class EyeBackdrop : MonoBehaviour
    {
        [SerializeField] private Transform iris;
        [SerializeField] private float lookRange = 0.45f;     // en unidades del dibujo (antes de escalar)
        [SerializeField] private Vector2 blinkEvery = new Vector2(8f, 12f);
        [SerializeField] private Color lid = new Color(0.78f, 0.36f, 0.42f, 1f);
        [SerializeField] private Color lidLine = new Color(0.25f, 0.06f, 0.12f, 1f);
        [SerializeField, TextArea] private string firstBlink = "¡El ojo PARPADEÓ! Mientras tiene los párpados cerrados, nadie te ve: aprovecha para cruzar.";

        private Vector3 irisHome;
        private Vector2 look, glance;
        private float glanceTimer, blinkTimer, blinkT = -1f;
        private Transform top, bottom;
        private bool told;

        public void Configure(Transform irisTransform) => iris = irisTransform;

        private void Start()
        {
            if (iris != null) irisHome = iris.localPosition;
            blinkTimer = Random.Range(blinkEvery.x, blinkEvery.y) * 0.6f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            UpdateIris(dt);
            UpdateBlink(dt);
        }

        private void UpdateIris(float dt)
        {
            if (iris == null) return;
            PlayerController p = PlayerController.Current;
            Vector2 target = Vector2.zero;
            if (p != null)
            {
                Vector2 d = p.Position - (Vector2)iris.position;
                target = d.sqrMagnitude > 0.01f ? d.normalized : Vector2.zero;
            }
            glanceTimer -= dt;
            if (glanceTimer <= 0f)
            {
                glanceTimer = Random.Range(2.5f, 5f);
                glance = Random.value < 0.4f ? Random.insideUnitCircle : Vector2.zero;   // a veces voltea a otro lado
            }
            if (glance != Vector2.zero && glanceTimer > 1.5f) target = glance;
            look = Vector2.Lerp(look, target, 1f - Mathf.Exp(-3f * dt));

            StealthMeter m = StealthMeter.Current;
            float nervous = m != null ? Mathf.Clamp01((m.Level - 0.5f) * 2f) : 0f;
            Vector2 shake = nervous > 0f ? Random.insideUnitCircle * 0.04f * nervous : Vector2.zero;
            iris.localPosition = irisHome + (Vector3)(look * lookRange + shake);
            float pupil = 1f + Mathf.Sin(Time.time * 1.3f) * 0.03f - nervous * 0.05f;
            iris.localScale = new Vector3(pupil, pupil, 1f);
        }

        private void UpdateBlink(float dt)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            if (top == null) BuildLids(cam.transform);

            float k = 0f;
            if (blinkT < 0f)
            {
                blinkTimer -= dt;
                if (blinkTimer <= 0f) { blinkT = 0f; Sfx.Effect("Alveolo_Desinfla", 0.25f); }
            }
            else
            {
                blinkT += dt;
                const float close = 0.18f, hold = 0.55f, open = 0.25f;
                if (blinkT < close) k = blinkT / close;
                else if (blinkT < close + hold) k = 1f;
                else if (blinkT < close + hold + open) k = 1f - (blinkT - close - hold) / open;
                else { blinkT = -1f; blinkTimer = Random.Range(blinkEvery.x, blinkEvery.y); }
                if (k >= 1f && !told && !string.IsNullOrEmpty(firstBlink)) { told = true; GameplayEvents.RaiseMessage(firstBlink); }
            }
            k = Mathf.SmoothStep(0f, 1f, k);
            if (StealthMeter.Current != null) StealthMeter.Current.Blinking = k > 0.75f;

            float h = cam.orthographicSize, w = h * cam.aspect;
            float lidH = h * 1.3f;
            // Orilla del párpado: de fuera de pantalla (k = 0) al centro (k = 1).
            float edge = Mathf.Lerp(h + 0.6f, 0f, k);
            top.localPosition = new Vector3(0f, edge + lidH * 0.5f, 10f);
            bottom.localPosition = new Vector3(0f, -edge - lidH * 0.5f, 10f);
            top.localScale = bottom.localScale = new Vector3(w * 2.2f, 1f, 1f);
            top.gameObject.SetActive(k > 0f);
            bottom.gameObject.SetActive(k > 0f);
        }

        private void BuildLids(Transform cam)
        {
            float h = Camera.main.orthographicSize * 1.3f;
            top = Lid(cam, "ParpadoArriba", h, 1);
            bottom = Lid(cam, "ParpadoAbajo", h, -1);
        }

        private Transform Lid(Transform parent, string name, float height, int side)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            Sprite sq = CodeSprites.Square, c = CodeSprites.Circle;
            // La piel del párpado, con su orilla oscura y pestañas del lado que cierra.
            CodeSprites.Add(t, sq, lid, 450, Vector2.zero, new Vector2(1f, height));
            CodeSprites.Add(t, sq, lidLine, 451, new Vector2(0f, -side * height * 0.5f), new Vector2(1f, 0.18f));
            CodeSprites.Add(t, sq, new Color(1f, 0.7f, 0.75f, 0.5f), 451, new Vector2(0f, -side * (height * 0.5f - 0.5f)), new Vector2(1f, 0.12f));
            for (int i = 0; i < 24; i++)
            {
                SpriteRenderer lash = CodeSprites.Add(t, sq, lidLine, 452, new Vector2(-0.5f + (i + 0.5f) / 24f, -side * (height * 0.5f + 0.18f)), new Vector2(0.006f, 0.45f));
                lash.transform.localRotation = Quaternion.Euler(0f, 0f, side * (i - 12) * 1.5f);
            }
            return t;
        }

        private void OnDisable()
        {
            if (StealthMeter.Current != null) StealthMeter.Current.Blinking = false;
        }
    }
}
