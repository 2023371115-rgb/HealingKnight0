using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Bloqueo que se trata con medicamento (GDD 5.12, Pulmones):
    ///  - Mucosidad: tapa el paso y, si la tocas, te frena a la mitad. Mucolítico (efectivo) o antihistamínico (parcial).
    ///  - Inflamación: tejido hinchado que cierra el pasillo. Antiinflamatorio hasta que se contraiga.
    /// La vida y la efectividad las maneja EnemyHealth (con un EnemyData cuyo 'threat' es Mucosidad o Inflamacion);
    /// al llegar a 0 se disuelve y el paso queda libre. Mientras pierde vida, el dibujo se encoge.
    /// Estructura esperada: raíz con BoxCollider2D (capa Ground, es la pared), EnemyHealth, Hurtbox y este script;
    /// hijo "Visual" con los sprites (con el pivote abajo para que se encoja hacia el piso).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class TreatableObstacle : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [Tooltip("Tamaño del dibujo con la vida casi en 0 (1 = no se encoge).")]
        [SerializeField, Range(0.2f, 1f)] private float minScale = 0.55f;
        [Tooltip("Hace que se encoja también a lo ancho (inflamación) o solo se vea más delgado (mucosidad).")]
        [SerializeField] private bool shrinkWidth = true;

        [Header("Frenar al tocarlo (mucosidad)")]
        [SerializeField] private bool slowsOnTouch;
        [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.5f;
        [SerializeField] private float touchPadding = 0.3f;

        private EnemyHealth health;
        private BoxCollider2D wall;
        private Vector3 visualBaseScale = Vector3.one;
        private float wobbleSeed;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            wall = GetComponent<BoxCollider2D>();
            if (visual != null) visualBaseScale = visual.localScale;
            wobbleSeed = Random.value * 10f;
        }

        private void Update()
        {
            if (!health.IsAlive) return;

            if (visual != null)
            {
                float k = Mathf.Lerp(minScale, 1f, health.Health01);
                float wobble = 1f + Mathf.Sin(Time.time * 2.5f + wobbleSeed) * 0.02f; // late como tejido vivo
                visual.localScale = new Vector3(
                    visualBaseScale.x * (shrinkWidth ? k : Mathf.Lerp(0.8f, 1f, health.Health01)) * wobble,
                    visualBaseScale.y * k,
                    visualBaseScale.z);
            }

            if (slowsOnTouch && wall != null) SlowPlayerIfTouching();
        }

        private void SlowPlayerIfTouching()
        {
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null) return;

            Bounds b = wall.bounds;
            Rect area = new Rect(b.min.x - touchPadding, b.min.y, b.size.x + touchPadding * 2f, b.size.y);
            if (box.OverlapsRect(area)) player.ApplySlow(slowMultiplier, 0.2f);
        }
    }
}
