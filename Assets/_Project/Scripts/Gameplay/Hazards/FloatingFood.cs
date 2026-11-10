using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Comida a medio digerir que FLOTA en el ácido y sirve de plataforma (de un solo sentido).
    ///  - Si el ácido está bajo (o seco), descansa en el piso ('restY').
    ///  - Si sube la marea, la comida sube con ella y te lleva encima: es la forma de no quemarte.
    ///  - Mientras flota se mece un poco (solo el dibujo; el piso donde te paras se queda plano).
    /// El objeto raíz marca la parte de ABAJO de la comida; el colisionador va a 'topHeight'.
    /// </summary>
    public class FloatingFood : MonoBehaviour
    {
        [SerializeField] private AcidLake lake;
        [Tooltip("Altura del fondo de la comida cuando no flota (el piso o el fondo del lago).")]
        [SerializeField] private float restY;
        [Tooltip("Alto del dibujo (unidades).")]
        [SerializeField] private float height = 1.4f;
        [Tooltip("Dónde está la parte que se pisa, medida desde abajo (unidades).")]
        [SerializeField] private float topHeight = 1.2f;
        [SerializeField] private float width = 1.6f;
        [Tooltip("Qué parte de la comida queda hundida en el ácido al flotar (0.35 = un tercio).")]
        [Range(0f, 0.9f)]
        [SerializeField] private float submerged = 0.35f;
        [Tooltip("Qué tan rápido sigue al ácido (más chico = más pesada, se queda atrás).")]
        [SerializeField] private float followTime = 0.25f;
        [SerializeField] private Transform visual;

        private float bottom, velocity, seed;
        private Vector3 visualBase;

        public void Configure(AcidLake acid, float rest, float foodHeight, float top, float foodWidth, Transform art)
        {
            lake = acid;
            restY = rest;
            height = foodHeight;
            topHeight = top;
            width = foodWidth;
            visual = art;
        }

        private void Start()
        {
            bottom = transform.position.y;
            seed = transform.position.x * 1.7f;
            if (visual != null) visualBase = visual.localPosition;
        }

        private void FixedUpdate()
        {
            float target = restY;
            if (lake != null && lake.Level > 0.05f) target = Mathf.Max(restY, lake.SurfaceY - height * submerged);

            float before = bottom;
            bottom = Mathf.SmoothDamp(bottom, target, ref velocity, followTime, Mathf.Infinity, Time.fixedDeltaTime);
            float delta = bottom - before;
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, bottom, p.z);

            // Lleva al jugador si está parado encima (sube y baja con la comida sin rebotar).
            PlayerController player = PlayerController.Current;
            if (player == null || Mathf.Abs(delta) < 0.0001f) return;
            Vector2 feet = player.Position;
            float top = before + topHeight;
            bool onTop = Mathf.Abs(feet.x - p.x) <= width * 0.5f + 0.1f && Mathf.Abs(feet.y - top) < 0.15f
                         && player.Motor.Velocity.y <= 0.5f;
            if (onTop)
            {
                Rigidbody2D body = player.Motor.Body;
                body.position += new Vector2(0f, delta);
            }
        }

        private void Update()
        {
            if (visual == null) return;
            bool floating = lake != null && lake.Level > 0.05f && bottom > restY + 0.02f;
            float k = floating ? 1f : 0f;
            float t = Time.time;
            visual.localPosition = visualBase + new Vector3(0f, Mathf.Sin(t * 2.1f + seed) * 0.03f * k, 0f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f + seed) * 3.5f * k);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            Vector3 p = transform.position;
            Gizmos.DrawLine(p + new Vector3(-width * 0.5f, topHeight, 0f), p + new Vector3(width * 0.5f, topHeight, 0f));
        }
    }
}
