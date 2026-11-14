using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Sushi con ANISAKIS: el anisakis es un gusano parásito que de verdad llega al estómago escondido en pescado
    /// crudo mal preparado. Cuando te paras en el sushi (o pasas muy cerca), el sushi tiembla y sale el gusano.
    /// Solo pasa una vez. El sushi sigue sirviendo de plataforma (es una FloatingFood normal).
    /// </summary>
    [RequireComponent(typeof(FloatingFood))]
    public class AnisakisNest : MonoBehaviour
    {
        [SerializeField] private EnemyHealth wormPrefab;
        [SerializeField] private EnemyData wormData;
        [SerializeField] private float triggerRadius = 1.6f;
        [SerializeField] private float warning = 0.6f;
        [SerializeField, TextArea] private string message = "NODO: ¡Anisakis! Un parásito escondido en el pescado crudo. Antiparasitario.";
        [SerializeField] private Transform visual;

        private FloatingFood food;
        private bool triggered, spawned;
        private float timer;

        public void Configure(EnemyHealth prefab, EnemyData data, Transform art)
        {
            wormPrefab = prefab;
            wormData = data;
            visual = art;
        }

        private void Awake() => food = GetComponent<FloatingFood>();

        private void Update()
        {
            if (spawned) return;
            PlayerController player = PlayerController.Current;
            if (!triggered)
            {
                if (player == null) return;
                Vector2 top = new Vector2(transform.position.x, food.TopY);
                if (food.PlayerOnTop || Vector2.Distance(player.Position, top) < triggerRadius)
                {
                    triggered = true;
                    timer = warning;
                    Sfx.EffectAt("Lombriz_Sale", transform.position, 0.6f);
                }
                return;
            }

            // El sushi tiembla antes de que salga el gusano.
            timer -= Time.deltaTime;
            if (visual != null) visual.localPosition = new Vector3(Mathf.Sin(Time.time * 50f) * 0.05f, visual.localPosition.y, 0f);
            if (timer > 0f) return;
            if (visual != null) visual.localPosition = new Vector3(0f, visual.localPosition.y, 0f);
            Spawn();
        }

        private void Spawn()
        {
            spawned = true;
            if (wormPrefab == null) return;
            Vector3 pos = new Vector3(transform.position.x + 0.3f, food.TopY + 0.2f, 0f);
            EnemyHealth worm = Instantiate(wormPrefab, pos, Quaternion.identity);
            if (wormData != null) worm.Configure(wormData, null);
            EnemyBrain brain = worm.GetComponent<EnemyBrain>();
            if (brain != null && wormData != null) brain.Configure(wormData);
            CharacterMotor2D motor = worm.GetComponent<CharacterMotor2D>();
            if (motor != null) motor.SetVelocity(new Vector2(Random.Range(-1.5f, 1.5f), 7f));
            ArenaZone.Adopt(worm);
            if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
        }
    }
}
