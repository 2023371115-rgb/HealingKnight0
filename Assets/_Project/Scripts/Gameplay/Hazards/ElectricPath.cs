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
    /// CAMINO ELÉCTRICO del corazón (nodo sinusal → haz de His): una tira brillante por donde corre un IMPULSO en
    /// cada latido. Si estás parado en la tira cuando pasa el impulso, ¡lo SURFEAS! (te avienta hacia adelante).
    /// A los enemigos que estén encima les da toque.
    /// </summary>
    public class ElectricPath : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, y;
        [SerializeField] private float speed = 18f;
        [SerializeField] private float boost = 15f;

        private Transform pulse;
        private float x;
        private bool running, told;
        private int lastBeat = -1;

        public void Configure(float startX, float endX, float pathY)
        {
            fromX = startX; toX = endX; y = pathY;
        }

        private void OnEnable() => Heartbeat.Beat += OnBeat;
        private void OnDisable() => Heartbeat.Beat -= OnBeat;

        private void Start()
        {
            float w = toX - fromX;
            CodeSprites.Add(transform, CodeSprites.Square, new Color(1f, 0.9f, 0.4f, 0.35f), -45, new Vector2(w * 0.5f + fromX - transform.position.x, y - transform.position.y + 0.06f), new Vector2(w, 0.16f), "Camino");
            pulse = new GameObject("Impulso").transform;
            pulse.SetParent(transform, false);
            CodeSprites.Add(pulse, CodeSprites.Glow, new Color(1f, 0.95f, 0.5f, 0.8f), 20, Vector2.zero, new Vector2(2f, 1.2f), "Brillo");
            CodeSprites.Add(pulse, CodeSprites.Circle, new Color(1f, 1f, 0.8f), 21, Vector2.zero, new Vector2(0.6f, 0.4f), "Chispa");
            pulse.gameObject.SetActive(false);
        }

        private void OnBeat(int count)
        {
            if (running || count == lastBeat) return;
            lastBeat = count;
            running = true;
            x = fromX;
            if (pulse != null) pulse.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!running || pulse == null) return;
            float dt = Time.deltaTime;
            float before = x;
            x += speed * dt;
            pulse.position = new Vector3(x, y + 0.15f, 0f);
            if (x > toX) { running = false; pulse.gameObject.SetActive(false); return; }

            PlayerController player = PlayerController.Current;
            if (player != null && player.Health.IsAlive && player.Motor.IsGrounded)
            {
                Vector2 p = player.Position;
                if (Mathf.Abs(p.y - y) < 0.4f && p.x >= before - 0.3f && p.x <= x + 0.3f)
                {
                    player.Motor.SetVelocity(new Vector2(boost, 7f));
                    Sfx.EffectAt("Jugador_Dash", p, 0.7f);
                    if (!told)
                    {
                        told = true;
                        GameplayEvents.RaiseMessage("¡Surfeaste el IMPULSO ELÉCTRICO del corazón! Así viaja la señal que hace latir al músculo.");
                    }
                }
            }
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector2 ep = e.transform.position;
                if (Mathf.Abs(ep.y - y) < 1.2f && ep.x >= before - 0.3f && ep.x <= x + 0.3f)
                    e.ApplyDamage(DamageInfo.Create(15f, DamageKind.Environment, Team.Player, ep));
            }
        }
    }
}
