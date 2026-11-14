using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// PROBIÓTICO: bacterias buenas (como las del yogur) que refuerzan la flora intestinal.
    /// Al tocarlo, la flora del colon se pone brava unos segundos y ataca a los enemigos que tenga cerca.
    /// Reaparece al rato.
    /// </summary>
    public class ProbioticPickup : MonoBehaviour
    {
        [SerializeField] private float duration = 10f;
        [SerializeField] private float radius = 0.8f;
        [SerializeField] private float respawnTime = 20f;
        [SerializeField, TextArea] private string message = "NODO: Probiótico. La flora buena se refuerza y ataca a los enemigos cercanos por unos segundos.";
        [SerializeField] private Transform visual;

        private Vector3 basePosition;
        private bool hidden;
        private float hiddenUntil;

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
            float t = Time.time;
            transform.position = basePosition + new Vector3(0f, Mathf.Sin(t * 2.4f) * 0.12f, 0f);
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.6f) * 10f);

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (Vector2.Distance(player.Position + Vector2.up * 0.9f, transform.position) > radius + 0.6f) return;
            FriendlyBacteria.Rally(duration);
            GameplayEvents.RaiseMessage(message);
            Sfx.Effect("Bomba_Ganada", 0.9f);
            hidden = true;
            hiddenUntil = Time.time + respawnTime;
            if (visual != null) visual.gameObject.SetActive(false);
        }
    }
}
