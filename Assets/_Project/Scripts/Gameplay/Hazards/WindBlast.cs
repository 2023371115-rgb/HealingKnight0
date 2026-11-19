using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// RÁFAGA de aire que avienta al jugador: el ESTORNUDO de la nariz o la TOS de la tráquea.
    /// - Modo estornudo (useMeter): una barra de "comezón" se llena sola y más rápido con cada polen vivo cerca
    ///   (el polen irrita la nariz). Al llenarse: aviso ("¡A... a...!") y luego ¡ACHÚ!
    /// - Modo tos: pasa cada cierto tiempo.
    /// Durante la ráfaga el jugador sale volando en 'direction', salvo que esté resguardado (WindAnchor: pelos nasales,
    /// anillos de cartílago) o agachado detrás de algo. Solo afecta dentro de [minX, maxX].
    /// </summary>
    public class WindBlast : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 100f;
        [SerializeField] private bool useMeter = true;
        [SerializeField] private float baseRate = 0.035f;       // por segundo
        [SerializeField] private float perPollen = 0.05f;       // por segundo y por polen vivo
        [SerializeField] private EnemyData pollen;
        [SerializeField] private float interval = 7f;           // modo tos
        [SerializeField] private float warning = 1.6f;
        [SerializeField] private float duration = 1.1f;
        [SerializeField] private float windSpeed = 10f;
        [SerializeField] private int direction = -1;
        [SerializeField, TextArea] private string warnMessage = "¡A... a... aaa...!";
        [SerializeField, TextArea] private string blastMessage = "¡¡ACHÚ!! Agárrate de los pelos de la nariz.";
        [SerializeField] private Color meterColor = new Color(1f, 0.85f, 0.3f, 1f);

        private enum State { Charging, Warning, Blowing }
        private State state = State.Charging;
        private float meter, timer;
        private Transform hud, hudFill;
        private Transform[] streaks;
        private bool explained;

        public void Configure(float from, float to, bool sneeze, EnemyData pollenData, float period, int dir, string warnText, string blastText)
        {
            minX = from;
            maxX = to;
            useMeter = sneeze;
            pollen = pollenData;
            interval = period;
            direction = dir;
            warnMessage = warnText;
            blastMessage = blastText;
        }

        private void Start() => timer = interval;

        /// <summary>¿Hay una ráfaga soplando ahora? (para que los pelos se doblen).</summary>
        public static int Blowing { get; private set; }

        private void OnDisable() { if (state == State.Blowing) Blowing = Mathf.Max(0, Blowing - 1); }

        private bool PlayerInside(PlayerController p) => p != null && p.Health.IsAlive && p.Position.x >= minX && p.Position.x <= maxX;

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            bool inside = PlayerInside(player);
            EnsureVisuals();
            hud.gameObject.SetActive(inside && useMeter);

            switch (state)
            {
                case State.Charging:
                    if (!inside) break;
                    if (useMeter)
                    {
                        int count = 0;
                        if (pollen != null)
                            foreach (EnemyHealth e in EnemyHealth.Active)
                                if (e != null && e.IsAlive && e.Data == pollen && Mathf.Abs(e.transform.position.x - player.Position.x) < 16f) count++;
                        meter += (baseRate + perPollen * count) * dt;
                        if (meter >= 1f) StartWarning();
                    }
                    else
                    {
                        timer -= dt;
                        if (timer <= 0f) StartWarning();
                    }
                    break;

                case State.Warning:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        state = State.Blowing;
                        Blowing++;
                        timer = duration;
                        if (!string.IsNullOrEmpty(blastMessage) && !explained) { explained = true; GameplayEvents.RaiseMessage(blastMessage); }
                        Sfx.Effect(useMeter ? "Estornudo" : "Tos_Rafaga", 1f);
                        Shake(0.3f, duration);
                    }
                    break;

                case State.Blowing:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        Blowing = Mathf.Max(0, Blowing - 1);
                        state = State.Charging;
                        meter = 0f;
                        timer = interval;
                    }
                    break;
            }

            if (hudFill != null)
            {
                float k = state == State.Charging ? meter : 1f;
                hudFill.localScale = new Vector3(Mathf.Max(0.001f, k) * 3.8f, 0.22f, 1f);
                hudFill.localPosition = new Vector3(-1.9f + k * 1.9f, 0f, 0f);
                hud.localPosition = new Vector3(0f, HudY() + (state == State.Warning ? Mathf.Sin(Time.time * 40f) * 0.05f : 0f), 10f);
            }
            UpdateStreaks(dt);
        }

        private void StartWarning()
        {
            state = State.Warning;
            timer = warning;
            if (!string.IsNullOrEmpty(warnMessage)) GameplayEvents.RaiseMessage(warnMessage);
            Sfx.Effect(useMeter ? "Estornudo_Aviso" : "Tos_Aviso", 0.8f);
            Shake(0.06f, warning);
        }

        private void FixedUpdate()
        {
            if (state != State.Blowing) return;
            PlayerController player = PlayerController.Current;
            if (!PlayerInside(player) || player.Motor == null) return;
            if (WindAnchor.IsSheltered(player.Position)) return;
            float k = Mathf.Clamp01(timer / duration);
            float force = windSpeed * (0.4f + 0.6f * k);
            player.Motor.Body.position += new Vector2(direction * force * Time.fixedDeltaTime, 0f);
        }

        // ---------- dibujo ----------

        private float HudY()
        {
            Camera cam = Camera.main;
            return cam != null ? cam.orthographicSize - 1.1f : 4f;
        }

        private void EnsureVisuals()
        {
            if (hud != null) return;
            Camera cam = Camera.main;
            Transform parent = cam != null ? cam.transform : transform;
            hud = new GameObject("BarraComezon").transform;
            hud.SetParent(parent, false);
            Sprite sq = CodeSprites.Square, c = CodeSprites.Circle;
            CodeSprites.Add(hud, sq, new Color(0.15f, 0.04f, 0.1f, 0.85f), 300, Vector2.zero, new Vector2(4.1f, 0.42f));
            hudFill = CodeSprites.Add(hud, sq, meterColor, 301, Vector2.zero, new Vector2(0.01f, 0.22f)).transform;
            CodeSprites.Add(hud, c, new Color(0.15f, 0.04f, 0.1f, 1f), 302, new Vector2(-2.35f, 0f), new Vector2(0.7f, 0.7f));
            CodeSprites.Add(hud, c, new Color(1f, 0.6f, 0.7f, 1f), 303, new Vector2(-2.35f, 0f), new Vector2(0.56f, 0.5f));   // nariz
            CodeSprites.Add(hud, c, new Color(0.4f, 0.12f, 0.2f, 1f), 304, new Vector2(-2.45f, -0.08f), new Vector2(0.12f, 0.1f));
            CodeSprites.Add(hud, c, new Color(0.4f, 0.12f, 0.2f, 1f), 304, new Vector2(-2.25f, -0.08f), new Vector2(0.12f, 0.1f));
            hud.gameObject.SetActive(false);

            streaks = new Transform[14];
            for (int i = 0; i < streaks.Length; i++)
            {
                streaks[i] = CodeSprites.Add(parent, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.5f), 299, Vector2.zero, new Vector2(2.4f, 0.08f), "Rafaga").transform;
                streaks[i].gameObject.SetActive(false);
            }
        }

        private void UpdateStreaks(float dt)
        {
            if (streaks == null) return;
            Camera cam = Camera.main;
            float h = cam != null ? cam.orthographicSize : 5f, w = cam != null ? h * cam.aspect : 9f;
            bool on = state == State.Blowing && PlayerInside(PlayerController.Current);
            for (int i = 0; i < streaks.Length; i++)
            {
                Transform s = streaks[i];
                if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
                if (!on) continue;
                float y = -h + (i + 0.5f) * 2f * h / streaks.Length;
                float x = Mathf.Repeat(Time.time * 40f * -direction + i * 7.3f, 2f * w + 6f) - w - 3f;
                s.localPosition = new Vector3(-x * direction * -1f, y + Mathf.Sin(i * 3.1f) * 0.3f, 10f);
            }
        }

        private static void Shake(float strength, float time)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, time);
        }
    }

    /// <summary>Lugar donde el aire no te avienta (te agarras de los pelos nasales o te cubres tras un anillo de cartílago).</summary>
    public class WindAnchor : MonoBehaviour
    {
        private static readonly List<WindAnchor> all = new List<WindAnchor>();
        [SerializeField] private float halfWidth = 0.9f;
        [SerializeField] private float height = 3.5f;

        public void Configure(float width, float h) { halfWidth = width * 0.5f; height = h; }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public static bool IsSheltered(Vector2 feet)
        {
            foreach (WindAnchor a in all)
            {
                Vector2 p = a.transform.position;
                if (Mathf.Abs(feet.x - p.x) <= a.halfWidth && feet.y >= p.y - 0.3f && feet.y <= p.y + a.height) return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(halfWidth * 2f, height, 0f));
        }
    }

    /// <summary>
    /// PELOS NASALES: filtran el aire (atrapan polvo y polen). Se mecen solos y, cuando sopla el estornudo, se doblan.
    /// Párate entre ellos y el aire no te avienta (tienen un WindAnchor). Se dibujan por código (o 'art').
    /// </summary>
    public class NasalHair : MonoBehaviour
    {
        [SerializeField] private float height = 3.4f;
        [SerializeField] private Sprite art;
        [SerializeField] private Color hair = new Color(0.32f, 0.12f, 0.22f, 1f);
        [SerializeField] private Color tip = new Color(0.55f, 0.25f, 0.4f, 1f);

        private Transform[] strands;
        private float seed, bend;

        public void Configure(float h, Sprite drawing) { height = h; art = drawing; }

        private void Start()
        {
            seed = Random.value * 10f;
            gameObject.AddComponent<WindAnchor>().Configure(1.8f, height);
            strands = new Transform[art != null ? 1 : 4];
            for (int i = 0; i < strands.Length; i++)
            {
                Transform s = new GameObject("Pelo" + i).transform;
                s.SetParent(transform, false);
                float x = art != null ? 0f : (i - 1.5f) * 0.32f;
                s.localPosition = new Vector3(x, 0f, 0f);
                if (art != null) CodeSprites.Art(s, art, 2.1f, -30, Vector2.zero);   // pivote abajo
                else
                {
                    float h = height * (0.8f + (i % 2) * 0.2f);
                    CodeSprites.Add(s, CodeSprites.Square, hair, -31, new Vector2(0f, h * 0.5f), new Vector2(0.16f, h));
                    CodeSprites.Add(s, CodeSprites.Circle, tip, -30, new Vector2(0f, h), new Vector2(0.24f, 0.32f));
                }
                strands[i] = s;
            }
        }

        private void Update()
        {
            bend = Mathf.MoveTowards(bend, WindBlast.Blowing > 0 ? 1f : 0f, Time.deltaTime * 4f);
            for (int i = 0; i < strands.Length; i++)
            {
                float sway = Mathf.Sin(Time.time * 1.6f + seed + i * 0.8f) * 6f;
                strands[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(sway, 35f + Mathf.Sin(Time.time * 30f + i) * 4f, bend));
            }
        }
    }
}
