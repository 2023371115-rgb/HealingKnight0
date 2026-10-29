using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Anima al caballero por piezas (muñeco articulado), sin dibujos extra:
    ///  - Caminar: los muslos se balancean en sentido contrario y la rodilla se dobla cuando la pierna regresa;
    ///    el cuerpo baja un poquito en cada paso.
    ///  - Saltar (subiendo): pierna de enfrente recogida, la de atrás estirada hacia atrás.
    ///  - Caer: piernas un poco abiertas y rodillas flojas.
    ///  - Quieto: respira (el torso sube y baja muy poco).
    /// Los ángulos son en grados; positivo = hacia adelante (hacia donde mira).
    /// </summary>
    public class PlayerRigAnimator : MonoBehaviour
    {
        [Header("Piezas (las arma PrefabSetup)")]
        [SerializeField] private Transform upper;      // torso + brazo (sube y baja al caminar)
        [SerializeField] private Transform backThigh;
        [SerializeField] private Transform backKnee;
        [SerializeField] private Transform frontThigh;
        [SerializeField] private Transform frontKnee;

        [Header("Caminar")]
        [Tooltip("Cuánto se abren las piernas al caminar.")]
        [SerializeField] private float strideAngle = 22f;
        [Tooltip("Cuánto se dobla la rodilla al regresar la pierna.")]
        [SerializeField] private float kneeBend = 40f;
        [Tooltip("Pasos por unidad recorrida (más = pasos más cortos y rápidos).")]
        [SerializeField] private float stepsPerUnit = 0.9f;
        [SerializeField] private float stepBob = 0.035f;

        [Header("Aire")]
        [SerializeField] private Vector2 jumpFront = new Vector2(35f, -70f);   // (muslo, rodilla)
        [SerializeField] private Vector2 jumpBack = new Vector2(-18f, -30f);
        [SerializeField] private Vector2 fallFront = new Vector2(14f, -22f);
        [SerializeField] private Vector2 fallBack = new Vector2(-12f, -14f);

        [Tooltip("Qué tan rápido pasa de una pose a otra.")]
        [SerializeField] private float blend = 16f;

        private PlayerController controller;
        private CharacterMotor2D motor;
        private float phase;
        private float bt, bk, ft, fk, bob;
        private Vector3 upperBase;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            motor = GetComponent<CharacterMotor2D>();
            if (upper != null) upperBase = upper.localPosition;
        }

        private void LateUpdate()
        {
            if (motor == null || frontThigh == null) return;
            float dt = Time.deltaTime;
            Vector2 v = motor.Velocity;

            float tBt, tBk, tFt, tFk, tBob;
            bool dead = controller != null && controller.Health != null && !controller.Health.IsAlive;

            if (dead)
            {
                tBt = tBk = tFt = tFk = 0f; tBob = 0f;
            }
            else if (!motor.IsGrounded)
            {
                bool rising = v.y > 0.5f;
                Vector2 f = rising ? jumpFront : fallFront;
                Vector2 b = rising ? jumpBack : fallBack;
                tFt = f.x; tFk = f.y; tBt = b.x; tBk = b.y; tBob = 0f;
            }
            else if (Mathf.Abs(v.x) > 0.3f)
            {
                // Avanza el ciclo según la distancia recorrida (así los pies no "patinan").
                phase += Mathf.Abs(v.x) * dt * stepsPerUnit * Mathf.PI * 2f;
                float s = Mathf.Sin(phase);
                float c = Mathf.Cos(phase);
                tFt = strideAngle * s;
                tBt = -strideAngle * s;
                tFk = -kneeBend * Mathf.Max(0f, -c);   // la rodilla se dobla mientras la pierna regresa
                tBk = -kneeBend * Mathf.Max(0f, c);
                tBob = -stepBob * Mathf.Abs(c);         // baja cuando las piernas se cruzan
            }
            else
            {
                phase = 0f;
                tBt = tBk = tFt = tFk = 0f;
                tBob = Mathf.Sin(Time.time * 2.2f) * 0.008f; // respira
            }

            float k = 1f - Mathf.Exp(-blend * dt);
            bt = Mathf.Lerp(bt, tBt, k); bk = Mathf.Lerp(bk, tBk, k);
            ft = Mathf.Lerp(ft, tFt, k); fk = Mathf.Lerp(fk, tFk, k);
            bob = Mathf.Lerp(bob, tBob, k);

            backThigh.localRotation = Quaternion.Euler(0f, 0f, bt);
            frontThigh.localRotation = Quaternion.Euler(0f, 0f, ft);
            if (backKnee != null) backKnee.localRotation = Quaternion.Euler(0f, 0f, bk);
            if (frontKnee != null) frontKnee.localRotation = Quaternion.Euler(0f, 0f, fk);
            if (upper != null) upper.localPosition = upperBase + new Vector3(0f, bob, 0f);
        }
    }
}
