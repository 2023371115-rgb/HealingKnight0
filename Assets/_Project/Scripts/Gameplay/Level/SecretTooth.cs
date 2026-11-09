using HealingKnight.Core.Flow;
using HealingKnight.Core.Progression;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Easter egg del nivel de la Boca: un diente con cara escondido en lo alto.
    /// Flota y se mece; si el jugador lo toca, brinca, gira, avisa y lo manda al mundo secreto
    /// (y lo registra como secreto: desde entonces el mundo aparece en el mapa del cuerpo).
    /// Se puede volver a tocar para regresar a ese mundo cuantas veces quieras.
    /// </summary>
    public class SecretTooth : MonoBehaviour
    {
        [SerializeField] private string secretId = "mundo_secreto";
        [Tooltip("Nivel al que lleva (LevelData con la escena del mundo secreto).")]
        [SerializeField] private LevelData targetLevel;
        [SerializeField, TextArea] private string firstMessage = "¡ME ENCONTRASTE! Te abro la puerta a otro mundo...";
        [SerializeField, TextArea] private string againMessage = "¿Otra vez por aquí? Vámonos.";
        [SerializeField] private float radius = 1.1f;
        [SerializeField] private float travelDelay = 2.2f;
        [SerializeField] private Transform visual;

        private Vector3 basePosition;
        private Vector3 baseScale = Vector3.one;
        private bool triggered;
        private float timer;

        private void Awake()
        {
            basePosition = transform.position;
            if (visual != null) baseScale = visual.localScale;
        }

        private void Update()
        {
            float t = Time.time;
            if (!triggered)
            {
                // Flota y se mece, como si estuviera de buenas.
                transform.position = basePosition + new Vector3(0f, Mathf.Sin(t * 2f) * 0.12f, 0f);
                if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.4f) * 8f);

                PlayerController player = PlayerController.Current;
                if (player != null && Vector2.Distance(player.Motor.Center, transform.position) <= radius) Trigger();
                return;
            }

            // Brinca, gira y crece antes del viaje.
            timer += Time.deltaTime;
            float k = Mathf.Clamp01(timer / travelDelay);
            transform.position = basePosition + new Vector3(0f, Mathf.Abs(Mathf.Sin(timer * 9f)) * 0.5f * (1f - k), 0f);
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, timer * 720f * k);
                visual.localScale = baseScale * (1f + k * 0.8f);
            }
            if (timer >= travelDelay)
            {
                enabled = false;
                if (targetLevel != null) GameSession.StartLevel(targetLevel);
            }
        }

        private void Trigger()
        {
            triggered = true;
            bool isNew = GameSession.FindSecret(secretId);
            GameplayEvents.RaiseMessage(isNew ? firstMessage : againMessage);
            Sfx.Effect("Diente_Secreto");
            Sfx.Effect("Escudo_Parry", 0.6f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.95f, 0.6f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
