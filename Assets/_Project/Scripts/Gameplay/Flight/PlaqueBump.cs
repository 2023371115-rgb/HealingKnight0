using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// PLACA DE COLESTEROL pegada a la pared de una arteria (aterosclerosis): un bulto sólido que ESTRECHA el paso.
    /// - Se rompe a balazos: con cada golpe se hace más chico (hasta un mínimo) y al acabarle la vida desaparece.
    ///   El ANTIINFLAMATORIO le hace más (sus debilidades están en su EnemyData).
    /// - Donde dos placas casi se tocan solo cabes ENCOGIDO.
    /// El dibujo y el colisionador (domo) se arman en SceneSetup; aquí solo se encoge con la vida y late con el corazón.
    /// Biología: el colesterol LDL se mete en la pared de la arteria, la inflama y forma una placa que estorba a la sangre.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class PlaqueBump : MonoBehaviour
    {
        [SerializeField] private float width = 3f;
        [SerializeField] private float height = 2.4f;
        [Tooltip("Cuelga de la pared de arriba (el objeto está girado 180°).")]
        [SerializeField] private bool fromTop;
        [Tooltip("Lo más chica que se hace antes de romperse del todo.")]
        [SerializeField] private float minScale = 0.4f;
        [SerializeField, TextArea] private string message;

        private EnemyHealth health;
        private Hurtbox box;
        private float shown = 1f;
        private bool told;

        public void Configure(float plaqueWidth, float plaqueHeight, bool hangsFromTop, string firstMessage)
        {
            width = plaqueWidth;
            height = plaqueHeight;
            fromTop = hangsFromTop;
            message = firstMessage;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            box = GetComponent<Hurtbox>();
            health.Died += OnDied;
        }

        /// <summary>Al romperse ya no estorba (aunque se siga desvaneciendo).</summary>
        private void OnDied(EnemyHealth _)
        {
            Collider2D solid = GetComponent<Collider2D>();
            if (solid != null) solid.enabled = false;
        }

        private void Update()
        {
            if (health == null || !health.IsAlive) return;
            float target = Mathf.Lerp(minScale, 1f, health.Health01);
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
            float beat = 1f + Heartbeat.SurgeNow * 0.03f;
            transform.localScale = new Vector3(beat, shown * beat, 1f);
            float h = height * shown;
            if (box != null) box.SetBox(new Vector2(0f, (fromTop ? -h : h) * 0.5f), new Vector2(width * 0.9f, h));

            FlightRoute route = FlightRoute.Current;
            if (!told && route != null && route.View.xMax > transform.position.x - width * 0.5f + 1f)
            {
                told = true;
                if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
            }
        }
    }
}
