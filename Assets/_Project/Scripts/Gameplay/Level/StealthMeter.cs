using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// SIGILO (Ojo). Biología: el ojo casi no deja entrar a las defensas ("inmunoprivilegio") porque una inflamación
    /// fuerte, la UVEÍTIS, puede dejarlo ciego. Aquí la barra de INFLAMACIÓN sube si un vigilante te ve, si pisas
    /// células que se encienden, si te toca un haz de luz o si disparas (hace ruido). Baja sola si nadie te ve.
    /// Si se llena: uveítis. Todo se oscurece salvo un círculo alrededor de ti unos segundos y llegan refuerzos.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class StealthMeter : MonoBehaviour
    {
        [SerializeField] private float decay = 0.12f;
        [SerializeField] private float shotNoise = 0.06f;
        [SerializeField] private float uveitisTime = 6f;
        [SerializeField] private EnemyHealth reinforcement;
        [SerializeField] private EnemyData reinforcementData;
        [SerializeField, TextArea] private string tutorial = "NODO: El ojo no tolera la inflamación. Que NO te vean: escóndete agachado ({abajo}) detrás de las cosas y dispara solo si hace falta.";

        public static StealthMeter Current { get; private set; }
        public float Level { get; private set; }
        public bool Alerted => uveitis > 0f;
        /// <summary>¿El jugador está escondido ahora (agachado tras algo, dentro de una mosca volante o en el punto ciego)?</summary>
        public bool PlayerHidden { get; private set; }
        /// <summary>El ojo está parpadeando (párpados cerrados): nadie ve nada.</summary>
        public bool Blinking { get; set; }

        private float lastSpotted = -10f, uveitis;
        private readonly HashSet<Projectile> heard = new HashSet<Projectile>();
        private Transform hud, fill;
        private SpriteRenderer veil, eye;
        private bool told;
        private float blindSpotUntil;

        public void Configure(EnemyHealth prefab, EnemyData data) { reinforcement = prefab; reinforcementData = data; }

        private void OnEnable() => Current = this;
        private void OnDisable() { if (Current == this) Current = null; }

        /// <summary>Alguien te está viendo: sube la barra 'perSecond' por segundo.</summary>
        public void Spot(float perSecond)
        {
            if (PlayerHidden || Alerted || Blinking) return;
            Level = Mathf.Min(1f, Level + perSecond * Time.deltaTime);
            lastSpotted = Time.time;
        }

        /// <summary>Ruido de una vez (pisar una célula, disparar).</summary>
        public void Noise(float amount)
        {
            if (Alerted) return;
            Level = Mathf.Min(1f, Level + amount);
            lastSpotted = Time.time;
        }

        public void MarkBlindSpot() => blindSpotUntil = Time.time + 0.2f;

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            EnsureHud();
            if (!told && p != null && !string.IsNullOrEmpty(tutorial)) { told = true; GameplayEvents.RaiseMessage(tutorial); }

            bool blind = Time.time < blindSpotUntil;
            PlayerHidden = p != null && (blind || HidingSpot.Covers(p));

            // Disparar hace ruido.
            foreach (Projectile s in Projectile.Flying)
                if (s != null && s.SourceTeam == Core.Damage.Team.Player && heard.Add(s)) Noise(shotNoise);
            heard.RemoveWhere(s => s == null || !Projectile.Flying.Contains(s));

            if (uveitis > 0f)
            {
                uveitis -= dt;
                if (uveitis <= 0f) { Level = 0.35f; GameplayEvents.RaiseMessage("La inflamación bajó. Vuelve a esconderte."); }
            }
            else
            {
                float rate = blind ? 0.6f : Time.time - lastSpotted > 1f ? decay : 0f;
                Level = Mathf.Max(0f, Level - rate * dt);
                if (Level >= 1f) StartUveitis(p);
            }

            // HUD.
            fill.localScale = new Vector3(Mathf.Max(0.001f, Level) * 3.8f, 0.22f, 1f);
            fill.localPosition = new Vector3(-1.9f + Level * 1.9f, 0f, 0f);
            Camera cam = Camera.main;
            if (cam != null) hud.localPosition = new Vector3(0f, cam.orthographicSize - 1.1f, 10f);
            eye.color = PlayerHidden ? new Color(0.5f, 0.6f, 0.7f) : Level > 0.6f ? new Color(1f, 0.35f, 0.35f) : Color.white;

            // Uveítis: todo oscuro menos un círculo alrededor de ti.
            float target = uveitis > 0f ? 0.95f : 0f;
            Color vc = veil.color;
            vc.a = Mathf.MoveTowards(vc.a, target, dt * 2f);
            veil.color = vc;
            if (p != null)
            {
                veil.transform.position = new Vector3(p.Position.x, p.Position.y + 0.9f, 0f);
                float size = 2.6f / 0.045f;
                veil.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        private void StartUveitis(PlayerController p)
        {
            uveitis = uveitisTime;
            Sfx.Effect("Sigilo_Detectado", 0.8f);
            GameplayEvents.RaiseMessage("¡UVEÍTIS! Te detectaron: el ojo se inflamó y casi no se ve. ¡Llegan defensas!");
            if (reinforcement == null || p == null) return;
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = new Vector3(p.Position.x + (i == 0 ? -6f : 6f), p.Position.y + 3f, 0f);
                EnemyHealth m = Instantiate(reinforcement, pos, Quaternion.identity);
                if (reinforcementData != null)
                {
                    m.Configure(reinforcementData, null);
                    EnemyBrain brain = m.GetComponent<EnemyBrain>();
                    if (brain != null) brain.Configure(reinforcementData);
                }
            }
        }

        private void EnsureHud()
        {
            if (hud != null) return;
            Camera cam = Camera.main;
            Transform parent = cam != null ? cam.transform : transform;
            hud = new GameObject("BarraInflamacion").transform;
            hud.SetParent(parent, false);
            Sprite sq = CodeSprites.Square, c = CodeSprites.Circle;
            CodeSprites.Add(hud, sq, new Color(0.1f, 0.03f, 0.08f, 0.85f), 300, Vector2.zero, new Vector2(4.1f, 0.42f));
            fill = CodeSprites.Add(hud, sq, new Color(1f, 0.3f, 0.35f, 1f), 301, Vector2.zero, new Vector2(0.01f, 0.22f)).transform;
            CodeSprites.Add(hud, c, new Color(0.1f, 0.03f, 0.08f, 1f), 302, new Vector2(-2.4f, 0f), new Vector2(0.8f, 0.55f));
            eye = CodeSprites.Add(hud, c, Color.white, 303, new Vector2(-2.4f, 0f), new Vector2(0.66f, 0.42f));
            CodeSprites.Add(hud, c, new Color(0.3f, 0.5f, 0.9f), 304, new Vector2(-2.4f, 0f), new Vector2(0.26f, 0.26f));
            CodeSprites.Add(hud, c, Color.black, 305, new Vector2(-2.4f, 0f), new Vector2(0.12f, 0.12f));
            veil = CodeSprites.Add(transform, DarknessZone.Hole(), new Color(0.12f, 0f, 0.02f, 0f), 500, Vector2.zero, Vector2.one, "Uveitis");
        }
    }
}
