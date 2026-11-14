using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// NIDO que suelta enemigos cuando pasas cerca, una y otra vez (con un máximo vivos a la vez).
    /// Se usa para las ÚLCERAS de la pared del colon: la Entamoeba (ameba) se mete en la pared y hace úlceras
    /// con forma de botella; de ahí salen amebas nuevas. Antes de soltar una, la úlcera burbujea (aviso).
    /// Se dibuja por código (cráter rojo oscuro con orilla) o con un sprite en 'art'.
    /// </summary>
    public class EnemyNest : MonoBehaviour
    {
        [SerializeField] private EnemyHealth prefab;
        [SerializeField] private EnemyData data;
        [SerializeField] private float range = 7f;
        [SerializeField] private float interval = 7f;
        [SerializeField] private int maxAlive = 1;
        [SerializeField] private float warning = 0.8f;
        [SerializeField] private Sprite art;
        [SerializeField] private float artWidth = 2f;

        private readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        private float timer = 1.5f;
        private Transform visual;

        public void Configure(EnemyHealth enemyPrefab, EnemyData enemyData, float spawnInterval, int max, Sprite drawing)
        {
            prefab = enemyPrefab;
            data = enemyData;
            interval = spawnInterval;
            maxAlive = max;
            art = drawing;
        }

        private void Start() => Build();

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive || prefab == null) return;
            alive.RemoveAll(e => e == null || !e.IsAlive);
            bool near = Mathf.Abs(player.Position.x - transform.position.x) <= range;
            if (!near || alive.Count >= maxAlive) { Pulse(0f); return; }

            timer -= Time.deltaTime;
            Pulse(timer < warning ? 1f - timer / warning : 0f);
            if (timer > 0f) return;
            timer = interval;
            Spawn();
        }

        private void Pulse(float k)
        {
            if (visual == null) return;
            float s = 1f + Mathf.Sin(Time.time * 30f) * 0.05f * k + k * 0.08f;
            visual.localScale = new Vector3(s, s, 1f);
        }

        private void Spawn()
        {
            EnemyHealth e = Instantiate(prefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
            if (data != null) e.Configure(data, null);
            EnemyBrain brain = e.GetComponent<EnemyBrain>();
            if (brain != null && data != null) brain.Configure(data);
            CharacterMotor2D motor = e.GetComponent<CharacterMotor2D>();
            if (motor != null) motor.SetVelocity(new Vector2(Random.Range(-1f, 1f), 7f));
            ArenaZone.Adopt(e);
            alive.Add(e);
            Sfx.EffectAt("Lombriz_Sale", transform.position, 0.6f);
        }

        private void Build()
        {
            visual = new GameObject("Ulcera").transform;
            visual.SetParent(transform, false);
            if (art != null) { CodeSprites.Art(visual, art, artWidth, -4, Vector2.zero); return; }
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(visual, c, new Color(0.25f, 0.05f, 0.08f, 1f), -6, new Vector2(0f, 0.05f), new Vector2(artWidth + 0.14f, 0.62f));
            CodeSprites.Add(visual, c, new Color(0.85f, 0.38f, 0.42f, 1f), -5, new Vector2(0f, 0.05f), new Vector2(artWidth, 0.5f));
            CodeSprites.Add(visual, c, new Color(0.35f, 0.06f, 0.1f, 1f), -4, new Vector2(0f, 0.02f), new Vector2(artWidth * 0.62f, 0.3f));
            CodeSprites.Add(visual, c, new Color(0.6f, 0.75f, 0.45f, 0.8f), -3, new Vector2(artWidth * 0.12f, 0.04f), new Vector2(0.25f, 0.12f));
        }
    }
}
