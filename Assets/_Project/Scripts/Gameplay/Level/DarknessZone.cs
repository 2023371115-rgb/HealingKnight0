using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// LÓBULO OCCIPITAL (la parte del cerebro que procesa lo que ves): la infección apagó la vista.
    /// Dentro de [minX, maxX] todo se pone oscuro salvo un círculo pequeño alrededor del jugador.
    /// Cada 'flashEvery' segundos las neuronas disparan juntas y un DESTELLO ilumina todo un momento:
    /// memoriza el camino y avanza a oscuras.
    /// </summary>
    public class DarknessZone : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 30f;
        [SerializeField] private float lightRadius = 2.4f;
        [SerializeField] private float darkness = 0.94f;
        [SerializeField] private float flashEvery = 3.6f;
        [SerializeField] private float flashTime = 0.9f;
        [SerializeField, TextArea] private string message = "NODO: Lóbulo occipital: aquí se procesa la vista y la infección la apagó. Los destellos de las neuronas te muestran el camino.";

        private SpriteRenderer veil;
        private float amount, time;
        private bool said;
        private static Sprite holeSprite;

        public void Configure(float from, float to) { minX = from; maxX = to; }

        private void Start()
        {
            veil = CodeSprites.Add(transform, Hole(), Color.black, 500, Vector2.zero, Vector2.one, "Oscuridad");
            veil.color = new Color(0f, 0f, 0f, 0f);
        }

        public static Sprite Hole()
        {
            if (holeSprite != null) return holeSprite;
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;          // 0 centro, 1 orilla
                    // El hueco mide el 6 % del ancho; borde suave.
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.045f, 0.09f, d));
                    px[y * n + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            holeSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 unidad
            return holeSprite;
        }

        private void LateUpdate()
        {
            PlayerController player = PlayerController.Current;
            bool inside = player != null && player.Position.x >= minX && player.Position.x <= maxX;
            float dt = Time.deltaTime;
            float flash = 0f;
            if (inside)
            {
                time += dt;
                if (!said && !string.IsNullOrEmpty(message)) { said = true; GameplayEvents.RaiseMessage(message); }
                float t = Mathf.Repeat(time, flashEvery);
                if (t > flashEvery - flashTime)
                {
                    float k = (t - (flashEvery - flashTime)) / flashTime;    // 0..1
                    flash = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                }
                if (t - dt < flashEvery - flashTime && t >= flashEvery - flashTime) Sfx.Effect("Neurona_Descarga", 0.5f);
            }
            amount = Mathf.MoveTowards(amount, inside ? 1f : 0f, dt * 2f);
            if (veil == null || player == null) return;

            // El hueco (6 % del sprite) debe medir lightRadius: tamaño del sprite = lightRadius / 0.045.
            float size = lightRadius / 0.045f;
            veil.transform.position = new Vector3(player.Position.x, player.Position.y + 0.9f, 0f);
            veil.transform.localScale = new Vector3(size, size, 1f);
            float a = darkness * amount * (1f - flash * 0.85f);
            veil.color = new Color(0.02f, 0f, 0.05f, a);
        }
    }
}
