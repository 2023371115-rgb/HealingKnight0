using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// VIGILANTE (Ojo): un macrófago del ojo que patrulla de un lado a otro con un CONO DE VISIÓN.
    /// Si te ve (y no estás escondido ni hay una pared en medio) se detiene, pone "!" y sube la inflamación.
    /// No es un enemigo: es una defensa del cuerpo, no se le dispara (y dispararle hace mucho ruido).
    /// </summary>
    public class Watcher : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float speed = 1.4f;
        [SerializeField] private float viewDistance = 7f;
        [SerializeField] private float viewAngle = 28f;
        [SerializeField] private float spotRate = 0.9f;
        [SerializeField] private Sprite art;

        private int facing = 1;
        private float pause;
        private Transform body, cone, alert;
        private SpriteRenderer coneRenderer;
        private static Sprite coneSprite;

        public void Configure(float from, float to, float walk, Sprite drawing) { minX = from; maxX = to; speed = walk; art = drawing; }

        private static Sprite Cone()
        {
            if (coneSprite != null) return coneSprite;
            const int w = 128, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1), v = Mathf.Abs(y / (float)(h - 1) - 0.5f) * 2f;
                    float a = v <= u ? (1f - u * 0.6f) : 0f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            coneSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w);   // 1 unidad de largo
            return coneSprite;
        }

        private void Start()
        {
            body = new GameObject("Macrofago").transform;
            body.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            if (art != null) CodeSprites.Art(body, art, 1.9f, 10, Vector2.zero);   // pivote abajo
            else
            {
                CodeSprites.Add(body, c, new Color(0.2f, 0.25f, 0.4f), 9, new Vector2(0f, 0.9f), new Vector2(1.9f, 1.75f));
                CodeSprites.Add(body, c, new Color(0.75f, 0.85f, 1f), 10, new Vector2(0f, 0.9f), new Vector2(1.75f, 1.6f));
                CodeSprites.Add(body, c, new Color(0.55f, 0.6f, 0.95f), 11, new Vector2(-0.3f, 0.7f), new Vector2(0.7f, 0.55f));     // núcleo
                CodeSprites.Add(body, c, Color.white, 12, new Vector2(0.35f, 1.15f), new Vector2(0.62f, 0.62f));                       // ojo
                CodeSprites.Add(body, c, new Color(0.15f, 0.2f, 0.4f), 13, new Vector2(0.45f, 1.15f), new Vector2(0.28f, 0.32f));
                for (int i = 0; i < 4; i++)
                    CodeSprites.Add(body, c, new Color(0.75f, 0.85f, 1f), 9, new Vector2(-0.9f + i * 0.6f, 0.1f), new Vector2(0.5f, 0.4f)); // patitas
            }
            cone = new GameObject("Vision").transform;
            cone.SetParent(transform, false);
            cone.localPosition = new Vector3(0f, 1.15f, 0f);
            coneRenderer = CodeSprites.Add(cone, Cone(), new Color(1f, 0.95f, 0.5f, 0.22f), 8, Vector2.zero, Vector2.one);
            float halfH = Mathf.Tan(viewAngle * Mathf.Deg2Rad) * viewDistance * 2f;
            coneRenderer.transform.localScale = new Vector3(viewDistance, halfH, 1f);
            alert = new GameObject("Alerta").transform;
            alert.SetParent(transform, false);
            alert.localPosition = new Vector3(0f, 2.4f, 0f);
            CodeSprites.Add(alert, CodeSprites.Square, new Color(1f, 0.25f, 0.25f), 30, new Vector2(0f, 0.15f), new Vector2(0.18f, 0.5f));
            CodeSprites.Add(alert, c, new Color(1f, 0.25f, 0.25f), 30, new Vector2(0f, -0.25f), new Vector2(0.2f, 0.2f));
            alert.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            StealthMeter meter = StealthMeter.Current;
            bool sees = p != null && p.Health.IsAlive && meter != null && CanSee(p) && !meter.PlayerHidden;
            alert.gameObject.SetActive(sees);
            coneRenderer.color = sees ? new Color(1f, 0.3f, 0.3f, 0.35f) : new Color(1f, 0.95f, 0.5f, 0.22f);

            if (sees)
            {
                meter.Spot(spotRate);
                if (Mathf.Abs(p.Position.x - transform.position.x) < 1.3f) meter.Noise(dt * 2f);   // te topaste con él
            }
            else
            {
                if (pause > 0f) pause -= dt;
                else
                {
                    Vector3 pos = transform.position;
                    pos.x += facing * speed * dt;
                    if (pos.x >= maxX) { pos.x = maxX; facing = -1; pause = 1.2f; }
                    if (pos.x <= minX) { pos.x = minX; facing = 1; pause = 1.2f; }
                    transform.position = pos;
                }
            }
            body.localScale = new Vector3(facing, 1f, 1f);
            cone.localScale = new Vector3(facing, 1f, 1f);
            body.localPosition = new Vector3(0f, sees ? 0f : Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.06f, 0f);
        }

        private bool CanSee(PlayerController p)
        {
            Vector2 eye = (Vector2)transform.position + Vector2.up * 1.15f;
            Vector2 target = p.Position + Vector2.up * (p.IsCrouching ? 0.4f : 0.9f);
            Vector2 d = target - eye;
            if (d.magnitude > viewDistance || Mathf.Sign(d.x) != facing) return false;
            float angle = Mathf.Abs(Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg);
            if (angle > viewAngle) return false;
            return Physics2D.Linecast(eye, target, 1 << PhysicsLayers.Ground).collider == null;
        }
    }
}
