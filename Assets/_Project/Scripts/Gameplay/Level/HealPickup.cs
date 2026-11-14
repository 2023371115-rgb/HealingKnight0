using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// SUERO ORAL (sales de rehidratación): el tratamiento real contra la diarrea del rotavirus, porque repone el agua
    /// y las sales que se pierden. En el juego es un sobre/botella que flota y te cura al tocarlo (una sola vez).
    /// </summary>
    public class HealPickup : MonoBehaviour
    {
        [SerializeField] private float amount = 35f;
        [Tooltip("Bombas extra (el apéndice: tejido de defensa).")]
        [SerializeField] private int bombs = 0;
        [SerializeField] private float radius = 0.8f;
        [SerializeField, TextArea] private string message = "NODO: Suero oral. Repone el agua y las sales que se pierden con la diarrea. +Vida";
        [SerializeField] private Transform visual;

        private Vector3 basePosition;
        private bool taken;

        public void Configure(Transform art, float heal, int extraBombs = 0, string text = null)
        {
            visual = art;
            amount = heal;
            bombs = extraBombs;
            if (!string.IsNullOrEmpty(text)) message = text;
        }

        private void Awake() => basePosition = transform.position;

        private void Update()
        {
            if (taken) return;
            float t = Time.time;
            transform.position = basePosition + new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.12f, 0f);
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f) * 8f);

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (Vector2.Distance(player.Position + Vector2.up * 0.9f, transform.position) > radius + 0.6f) return;

            taken = true;
            player.Health.Heal(amount);
            if (bombs > 0)
            {
                PlayerBombs b = player.GetComponent<PlayerBombs>();
                if (b != null) b.Add(bombs);
            }
            GameplayEvents.RaiseMessage(message);
            Sfx.Effect("Bomba_Ganada", 0.9f);
            gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
