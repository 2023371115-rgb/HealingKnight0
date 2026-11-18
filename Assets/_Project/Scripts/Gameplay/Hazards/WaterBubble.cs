using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// BURBUJA DE AGUA (arena de la rabia). Síntoma real de la rabia: la HIDROFOBIA, pánico y espasmos ante el agua.
    /// Dispárale (con lo que sea) y revienta en un chapuzón: las cadenas del jefe rabia que estén cerca se paralizan
    /// de miedo unos segundos (no avanzan ni disparan): ¡aprovecha para pegarles! Vuelve a aparecer después.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class WaterBubble : MonoBehaviour
    {
        [SerializeField] private float splashRadius = 4f;
        [SerializeField] private float scareTime = 3f;
        [SerializeField] private float respawn = 7f;
        [SerializeField] private Sprite art;

        private Transform visual;
        private Transform[] drops;
        private Vector2 home;
        private float hidden, splashT = -1f;
        private static bool explained;

        public void Configure(float radius, float seconds, Sprite drawing) { splashRadius = radius; scareTime = seconds; art = drawing; }

        private void Start()
        {
            home = transform.position;
            visual = new GameObject("Agua").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            if (art != null) CodeSprites.Art(visual, art, 1.1f, 22, Vector2.zero);
            else
            {
                CodeSprites.Add(visual, c, new Color(0.05f, 0.2f, 0.45f, 1f), 20, Vector2.zero, new Vector2(1.1f, 1.1f));
                CodeSprites.Add(visual, c, new Color(0.35f, 0.75f, 1f, 0.9f), 21, Vector2.zero, new Vector2(0.96f, 0.96f));
                CodeSprites.Add(visual, c, new Color(0.7f, 0.95f, 1f, 0.9f), 22, new Vector2(0f, -0.15f), new Vector2(0.6f, 0.35f));
                CodeSprites.Add(visual, c, Color.white, 23, new Vector2(-0.22f, 0.24f), new Vector2(0.22f, 0.16f));
            }
            drops = new Transform[8];
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = CodeSprites.Add(transform, c, new Color(0.45f, 0.8f, 1f, 0.9f), 24, Vector2.zero, new Vector2(0.25f, 0.3f), "Gota").transform;
                drops[i].gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateSplash(dt);
            if (hidden > 0f)
            {
                hidden -= dt;
                if (hidden <= 0f) visual.gameObject.SetActive(true);
                return;
            }
            transform.position = home + new Vector2(Mathf.Sin(Time.time * 0.9f + home.x) * 0.3f, Mathf.Sin(Time.time * 1.7f + home.x) * 0.25f);

            var shots = Projectile.Flying;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Projectile p = shots[i];
                if (p == null || p.SourceTeam != Team.Player) continue;
                if (Vector2.Distance(p.Position, transform.position) > 0.7f) continue;
                p.Consume();
                Splash();
                return;
            }
        }

        private void Splash()
        {
            Vector2 at = transform.position;
            visual.gameObject.SetActive(false);
            hidden = respawn;
            splashT = 0f;
            Sfx.EffectAt("Acido_Burbujeo", at, 1f);
            RabiesBoss boss = RabiesBoss.Current;
            int scared = boss != null ? boss.Scare(at, splashRadius, scareTime) : 0;
            if (scared > 0 && !explained)
            {
                explained = true;
                GameplayEvents.RaiseMessage("¡HIDROFOBIA! La rabia le tiene pánico al agua: se paralizó. ¡Pégale ahora!");
            }
        }

        private void UpdateSplash(float dt)
        {
            if (splashT < 0f) return;
            splashT += dt;
            for (int i = 0; i < drops.Length; i++)
            {
                float a = i * Mathf.PI * 2f / drops.Length;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) + 0.6f) * 5f;
                Vector2 p = v * splashT + Vector2.down * 9f * splashT * splashT;
                drops[i].localPosition = (Vector2)(home - (Vector2)transform.position) + p;
                drops[i].gameObject.SetActive(splashT < 0.7f);
            }
            if (splashT >= 0.7f) splashT = -1f;
        }
    }
}
