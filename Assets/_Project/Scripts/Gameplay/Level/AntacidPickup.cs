using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Cápsula de OMEPRAZOL: es un medicamento real que baja la producción de ácido del estómago
    /// (se usa junto con antibióticos para curar la infección por H. pylori: la "triple terapia").
    /// Al tocarla, el ácido de todos los lagos baja y deja de quemar unos segundos: puedes caminar por el fondo
    /// y los chorros de ácido se apagan. Después la cápsula reaparece (por si la vuelves a necesitar).
    /// </summary>
    public class AntacidPickup : MonoBehaviour
    {
        [SerializeField] private float duration = 8f;
        [SerializeField] private float radius = 0.8f;
        [SerializeField] private float respawnTime = 14f;
        [SerializeField, TextArea] private string message = "NODO: Omeprazol. El estómago produce menos ácido por unos segundos: ¡aprovecha!";
        [SerializeField] private Transform visual;

        private Vector3 basePosition;
        private float hiddenUntil;
        private bool hidden;

        public void Configure(Transform art) => visual = art;

        private void Awake() => basePosition = transform.position;

        private void Update()
        {
            if (hidden)
            {
                if (Time.time < hiddenUntil) return;
                hidden = false;
                if (visual != null) visual.gameObject.SetActive(true);
            }

            // Flota y gira un poco para que se note que se puede agarrar.
            float t = Time.time;
            transform.position = basePosition + new Vector3(0f, Mathf.Sin(t * 2.5f) * 0.12f, 0f);
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.7f) * 15f);

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 center = player.Position + Vector2.up * 0.9f;
            if (Vector2.Distance(center, transform.position) > radius + 0.6f) return;

            AcidLake.Neutralize(duration);
            GameplayEvents.RaiseMessage(message);
            Sfx.Effect("Bomba_Ganada", 0.9f);
            hidden = true;
            hiddenUntil = Time.time + respawnTime;
            if (visual != null) visual.gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.4f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
