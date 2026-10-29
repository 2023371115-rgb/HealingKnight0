using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.CameraSystem
{
    /// <summary>
    /// Cámara de run and gun: sigue al jugador en X, mantiene una altura base y solo sube
    /// cuando el jugador trepa plataformas altas. Respeta los límites del nivel y de la arena activa.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameCamera : MonoBehaviour
    {
        [SerializeField] private float followSmoothing = 6f;
        [Tooltip("Cuánto se adelanta la cámara hacia donde mira el jugador.")]
        [SerializeField] private float lookAhead = 1.5f;
        [Tooltip("Altura base de la cámara (centro de la pantalla).")]
        [SerializeField] private float fixedY = 2.5f;
        [Tooltip("La cámara sube si el jugador está más arriba que esto respecto a la altura base.")]
        [SerializeField] private float riseThreshold = 1.5f;
        [SerializeField] private float verticalSmoothing = 3f;

        private Camera cam;
        private float shakeTimer;
        private float shakeStrength;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        private void LateUpdate()
        {
            PlayerController player = PlayerController.Current;
            float targetX = transform.position.x;
            float targetY = fixedY;
            if (player != null)
            {
                targetX = player.Position.x + player.Motor.Facing * lookAhead;
                if (player.Position.y > fixedY - 2.5f + riseThreshold)
                    targetY = player.Position.y + 2.5f - riseThreshold;
            }

            float halfWidth = cam.orthographicSize * cam.aspect;
            float min = WalkableArea.MinX + halfWidth;
            float max = WalkableArea.MaxX - halfWidth;
            targetX = min <= max ? Mathf.Clamp(targetX, min, max) : (WalkableArea.MinX + WalkableArea.MaxX) * 0.5f;

            float x = Mathf.Lerp(transform.position.x, targetX, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
            float y = Mathf.Lerp(transform.position.y, targetY, 1f - Mathf.Exp(-verticalSmoothing * Time.deltaTime));
            Vector3 pos = new Vector3(x, y, transform.position.z);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                pos += (Vector3)(Random.insideUnitCircle * shakeStrength);
            }

            transform.position = pos;
        }

        public void Shake(float strength = 0.15f, float duration = 0.15f)
        {
            shakeStrength = strength;
            shakeTimer = duration;
        }

        /// <summary>Coloca la cámara de golpe (al cargar o reaparecer).</summary>
        public void SnapTo(float x)
        {
            transform.position = new Vector3(x, fixedY, transform.position.z);
        }
    }
}
