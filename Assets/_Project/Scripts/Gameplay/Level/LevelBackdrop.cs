using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Fondo de nivel hecho con UNA imagen fija (en lugar de la pared y los adornos de placeholder).
    /// Va pegado a la cámara para que nunca se vea el borde, pero se desliza un poco en sentido contrario
    /// conforme avanzas por el nivel: así parece que está muy lejos (efecto de profundidad / parallax).
    ///  - Al inicio del nivel se ve el lado izquierdo de la imagen; al final, el lado derecho.
    ///  - Se escala solo para cubrir la pantalla, aunque el monitor sea más ancho que 16:9.
    /// Corre después de la cámara (DefaultExecutionOrder) para no temblar.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class LevelBackdrop : MonoBehaviour
    {
        [Tooltip("Dónde empieza y termina el nivel (en X). Lo llena SceneSetup.")]
        [SerializeField] private float levelStartX = -10f;
        [SerializeField] private float levelEndX = 90f;

        [Tooltip("Cuánto más alto que la pantalla es el fondo (1.2 = 20 % de margen).")]
        [SerializeField] private float heightCover = 1.2f;
        [Tooltip("Qué parte del sobrante a los lados se usa para deslizar (0 = fijo, 1 = llega justo al borde).")]
        [Range(0f, 1f)]
        [SerializeField] private float slide = 0.9f;
        [Tooltip("Subir o bajar el fondo respecto al centro de la cámara (unidades).")]
        [SerializeField] private float verticalOffset = 0f;
        [Tooltip("Cuánto se mueve el fondo cuando la cámara sube (0 = pegado, 0.3 = se queda un poco atrás).")]
        [Range(0f, 1f)]
        [SerializeField] private float verticalParallax = 0.15f;

        private SpriteRenderer spriteRenderer;
        private Camera cam;
        private float baseCameraY;
        private bool hasBaseY;

        public void Configure(float startX, float endX)
        {
            levelStartX = startX;
            levelEndX = endX;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null || spriteRenderer.sprite == null) return;
            }
            if (!hasBaseY) { baseCameraY = cam.transform.position.y; hasBaseY = true; }

            // Tamaño: que cubra la pantalla con margen.
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            Vector2 size = spriteRenderer.sprite.bounds.size;
            float scale = Mathf.Max(viewH * heightCover / size.y, viewW * 1.05f / size.x);
            transform.localScale = new Vector3(scale, scale, 1f);

            // Deslizamiento: de +range (inicio) a -range (final).
            float range = Mathf.Max(0f, (size.x * scale - viewW) * 0.5f) * slide;
            Vector3 c = cam.transform.position;
            float progress = Mathf.InverseLerp(levelStartX, levelEndX, c.x);
            float x = c.x + Mathf.Lerp(range, -range, progress);

            float maxY = Mathf.Max(0f, (size.y * scale - viewH) * 0.5f);
            float y = c.y + verticalOffset - Mathf.Clamp((c.y - baseCameraY) * verticalParallax, -maxY, maxY);

            transform.position = new Vector3(x, y, 5f);
        }
    }
}
