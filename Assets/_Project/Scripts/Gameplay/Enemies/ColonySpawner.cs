using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// H. pylori MADRE: una colonia grande y lenta que suelta H. pylori chiquitas cada cierto tiempo,
    /// hasta que la destruyes. Antes de soltar crías "se infla" (aviso) y tiembla.
    /// Las crías cuentan para la arena (ArenaZone.Adopt) y no aparecen si ya hay muchas vivas.
    /// Se mueve con su EnemyBrain normal (comportamiento Tank: lenta y pesada).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ColonySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth childPrefab;
        [SerializeField] private EnemyData childData;
        [SerializeField] private float interval = 5f;
        [SerializeField] private float warning = 0.8f;
        [SerializeField] private int perSpawn = 2;
        [SerializeField] private int maxAlive = 4;
        [Tooltip("Solo suelta crías si el jugador está a esta distancia o menos.")]
        [SerializeField] private float activeRange = 14f;
        [SerializeField] private Transform visual;

        private EnemyHealth health;
        private readonly List<EnemyHealth> children = new List<EnemyHealth>();
        private float timer;
        private Vector3 baseScale = Vector3.one;

        public void Configure(EnemyHealth prefab, EnemyData data, Transform art)
        {
            childPrefab = prefab;
            childData = data;
            visual = art;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            timer = interval * 0.6f;
            if (visual != null) baseScale = visual.localScale;
        }

        private void Update()
        {
            if (health == null || !health.IsAlive || childPrefab == null) return;
            PlayerController player = PlayerController.Current;
            if (player == null || Vector2.Distance(player.Position, transform.position) > activeRange) return;

            children.RemoveAll(c => c == null || !c.IsAlive);
            timer -= Time.deltaTime;

            // Aviso: se infla y tiembla antes de soltar crías.
            if (visual != null)
            {
                float k = timer < warning ? 1f - Mathf.Clamp01(timer / warning) : 0f;
                float shake = k > 0f ? Mathf.Sin(Time.time * 40f) * 0.04f * k : 0f;
                visual.localScale = baseScale * (1f + 0.15f * k);
                visual.localPosition = new Vector3(shake, visual.localPosition.y, 0f);
            }

            if (timer > 0f) return;
            timer = interval;
            if (children.Count >= maxAlive) return;

            int count = Mathf.Min(perSpawn, maxAlive - children.Count);
            for (int i = 0; i < count; i++) Spawn(i, count);
            Sfx.EffectAt("Colmena_Enjambre", transform.position, 0.8f);
        }

        private void Spawn(int i, int count)
        {
            Vector3 pos = transform.position + new Vector3((i - (count - 1) * 0.5f) * 0.8f, 0.8f, 0f);
            EnemyHealth child = Instantiate(childPrefab, pos, Quaternion.identity);
            if (childData != null) child.Configure(childData, null);
            EnemyBrain brain = child.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                if (childData != null) brain.Configure(childData);
                brain.MarkOffspring();
            }
            CharacterMotor2D motor = child.GetComponent<CharacterMotor2D>();
            if (motor != null) motor.SetVelocity(new Vector2((i - (count - 1) * 0.5f) * 4f + Random.Range(-1f, 1f), 5f));
            ArenaZone.Adopt(child);
            children.Add(child);
        }
    }
}
