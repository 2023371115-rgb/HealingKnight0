using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Flight;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// VÁLVULA DEL CORAZÓN (a pie): dos valvas (una del techo y una del piso) que se abren justo DESPUÉS de cada
    /// latido y se cierran antes del siguiente. Cerradas son una pared. Si se cierran contigo en medio te avientan
    /// para atrás. Con arritmia se abren a destiempo.
    /// Biología: las válvulas dejan pasar la sangre hacia un solo lado; el "lub-dub" del latido es el ruido al cerrarse.
    /// </summary>
    public class BeatValve : MonoBehaviour
    {
        [SerializeField] private float height = 9f;
        [Tooltip("Parte del latido en la que está abierta (0..1), justo después del 'lub'.")]
        [SerializeField] private float openPart = 0.55f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private string label;

        private BoxCollider2D wall;
        private Transform upper, lower;
        private bool wasOpen, told;
        private float angle;

        public void Configure(float wallHeight, float openFraction, string firstMessage)
        {
            height = wallHeight; openPart = openFraction; label = firstMessage;
        }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(0.9f, height);
            wall.offset = new Vector2(0f, height * 0.5f);
            upper = Leaf("ValvaArriba", height, -1f);
            lower = Leaf("ValvaAbajo", 0f, 1f);
        }

        private Transform Leaf(string name, float y, float dir)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, y, 0f);
            float len = height * 0.52f;
            var leaf = new GameObject("Hoja").transform;
            leaf.SetParent(pivot, false);
            leaf.localPosition = new Vector3(0f, dir * len * 0.5f, 0f);
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(0.4f, 0.05f, 0.1f), 5, Vector2.zero, new Vector2(0.95f, len + 0.2f), "Orilla");
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(1f, 0.72f, 0.75f), 6, Vector2.zero, new Vector2(0.75f, len), "Valva");
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(1f, 0.95f, 0.95f, 0.6f), 7, new Vector2(-0.12f, 0f), new Vector2(0.2f, len * 0.6f), "Brillo");
            return pivot;
        }

        private void Update()
        {
            if (wall == null) return;
            float phase = Heartbeat.Current != null ? Heartbeat.PhaseNow : Mathf.Repeat(Time.time / 0.85f, 1f);
            bool open = phase > 0.04f && phase < 0.04f + openPart;
            float k = open ? 1f : 0f;
            float a = Mathf.Lerp(0f, 78f, k);
            // Se abren hacia adelante (como una puerta de vaivén que solo abre para un lado).
            angle = Mathf.Lerp(angle, a, 1f - Mathf.Exp(-30f * Time.deltaTime));
            if (upper != null) upper.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (lower != null) lower.localRotation = Quaternion.Euler(0f, 0f, -angle);
            wall.enabled = !open;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            float dx = player.Position.x - transform.position.x;
            if (!told && Mathf.Abs(dx) < 8f && !string.IsNullOrEmpty(label))
            {
                told = true;
                GameplayEvents.RaiseMessage(label);
            }
            if (wasOpen && !open && Mathf.Abs(dx) < 0.75f && player.Position.y < transform.position.y + height)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, transform.position);
                player.Health.ApplyDamage(info);
                player.Motor.Teleport(new Vector2(transform.position.x - 1.2f, player.Position.y));
            }
            wasOpen = open;
        }
    }
}
