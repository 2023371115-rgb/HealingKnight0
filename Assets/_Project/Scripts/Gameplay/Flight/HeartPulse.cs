using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Adorno que se mueve con el latido (Heartbeat):
    /// - Squeeze: el músculo del ventrículo se aprieta (se encoge hacia 'squeeze').
    /// - Valve: una valva gira de cerrada a abierta cuando la sangre sale (las válvulas se abren al latir).
    /// - Throb: la pared de la arteria se ensancha un poquito con cada pulso (por eso sientes el pulso en la muñeca).
    /// </summary>
    public class HeartPulse : MonoBehaviour
    {
        public enum Mode { Squeeze, Valve, Throb }

        [SerializeField] private Mode mode = Mode.Throb;
        [SerializeField] private float amount = 0.08f;
        [Tooltip("Valva: grados que gira al abrirse.")]
        [SerializeField] private float openAngle = 70f;

        private Vector3 baseScale, basePosition;
        private Quaternion baseRotation;

        public void Configure(Mode pulseMode, float strength, float angle = 70f)
        {
            mode = pulseMode;
            amount = strength;
            openAngle = angle;
        }

        private void Start()
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        private void Update()
        {
            float s = Heartbeat.SurgeNow;
            switch (mode)
            {
                case Mode.Squeeze:
                    transform.localScale = new Vector3(baseScale.x * (1f - s * amount), baseScale.y * (1f - s * amount * 1.4f), baseScale.z);
                    break;
                case Mode.Valve:
                    // Se abre rápido con el empujón y se cierra antes del siguiente latido.
                    float open = Mathf.Clamp01(s * 1.6f);
                    transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, openAngle * open);
                    break;
                case Mode.Throb:
                    transform.localPosition = basePosition + transform.up * (s * amount);
                    break;
            }
        }
    }
}
