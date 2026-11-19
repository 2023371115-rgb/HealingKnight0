using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// LÓBULO FRONTAL (planear, decidir, recordar pasos): un circuito de neuronas cierra el paso.
    /// Al acercarte, las neuronas se encienden en un ORDEN (demostración). Tócalas en ese mismo orden para abrir la
    /// compuerta (como "Simón dice"). Si te equivocas te da un toque, se apagan todas y repite la demostración.
    /// </summary>
    public class SequencePuzzle : MonoBehaviour
    {
        [SerializeField] private Vector2[] neurons;
        [SerializeField] private int[] order;
        [SerializeField] private float gateX;
        [SerializeField] private float gateHeight = 9f;
        [SerializeField] private float triggerRange = 7f;
        [SerializeField, TextArea] private string message = "NODO: Lóbulo frontal: aquí se planea. Mira el orden en que se encienden las neuronas y tócalas igual.";
        [SerializeField] private Color idle = new Color(0.3f, 0.45f, 0.95f, 1f);
        [SerializeField] private Color show = new Color(1f, 0.92f, 0.35f, 1f);
        [SerializeField] private Color good = new Color(0.45f, 1f, 0.55f, 1f);
        [SerializeField] private Color bad = new Color(1f, 0.3f, 0.3f, 1f);

        private enum State { Waiting, Demo, Input, Solved }
        private State state = State.Waiting;
        private readonly List<SpriteRenderer> cores = new List<SpriteRenderer>();
        private readonly List<Transform> bodies = new List<Transform>();
        private int progress, demoIndex;
        private float timer, touchCooldown;
        private GameObject gate;
        private SpriteRenderer gateGlow;
        private bool said;

        public void Configure(Vector2[] positions, int[] sequence, float wallX)
        {
            neurons = positions;
            order = sequence;
            gateX = wallX;
        }

        private void Start()
        {
            Sprite c = CodeSprites.Circle;
            for (int i = 0; i < neurons.Length; i++)
            {
                Transform n = new GameObject("NeuronaFrontal" + i).transform;
                n.SetParent(transform, true);
                n.position = neurons[i];
                CodeSprites.Add(n, c, new Color(0.05f, 0.03f, 0.15f, 1f), 10, Vector2.zero, new Vector2(1.1f, 1.1f));
                cores.Add(CodeSprites.Add(n, c, idle, 11, Vector2.zero, new Vector2(0.92f, 0.92f)));
                CodeSprites.Add(n, c, new Color(1f, 1f, 1f, 0.6f), 12, new Vector2(-0.18f, 0.2f), new Vector2(0.26f, 0.16f));
                for (int d = 0; d < 4; d++)     // dendritas
                {
                    float a = d * 90f + 45f;
                    SpriteRenderer den = CodeSprites.Add(n, CodeSprites.Square, new Color(0.3f, 0.45f, 0.95f, 0.6f), 9, Vector2.zero, new Vector2(0.9f, 0.08f));
                    den.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                    den.transform.localPosition = Quaternion.Euler(0f, 0f, a) * new Vector3(0.75f, 0f, 0f);
                }
                bodies.Add(n);
            }

            // Compuerta de tejido (pared) que se abre al resolverlo.
            gate = new GameObject("CompuertaFrontal");
            gate.transform.SetParent(transform, true);
            gate.transform.position = new Vector3(gateX, transform.position.y, 0f);
            gate.layer = PhysicsLayers.Ground;
            var box = gate.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, gateHeight);
            box.offset = new Vector2(0f, gateHeight * 0.5f);
            CodeSprites.Add(gate.transform, CodeSprites.Square, new Color(0.06f, 0.03f, 0.15f, 1f), 3, new Vector2(0f, gateHeight * 0.5f), new Vector2(1.34f, gateHeight));
            CodeSprites.Add(gate.transform, CodeSprites.Square, new Color(0.28f, 0.16f, 0.5f, 1f), 4, new Vector2(0f, gateHeight * 0.5f), new Vector2(1.1f, gateHeight));
            gateGlow = CodeSprites.Add(gate.transform, CodeSprites.Square, new Color(1f, 0.92f, 0.35f, 0.5f), 5, new Vector2(0f, gateHeight * 0.5f), new Vector2(0.18f, gateHeight));
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (touchCooldown > 0f) touchCooldown -= dt;
            PlayerController player = PlayerController.Current;
            if (state == State.Solved || player == null) return;
            if (gateGlow != null) gateGlow.color = new Color(1f, 0.92f, 0.35f, 0.3f + 0.25f * Mathf.Sin(Time.time * 4f));

            switch (state)
            {
                case State.Waiting:
                    if (Mathf.Abs(player.Position.x - Center.x) < triggerRange)
                    {
                        if (!said && !string.IsNullOrEmpty(message)) { said = true; GameplayEvents.RaiseMessage(message); }
                        StartDemo(0.8f);
                    }
                    break;

                case State.Demo:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        if (demoIndex > 0) SetColor(order[demoIndex - 1], idle);
                        if (demoIndex >= order.Length) { state = State.Input; progress = 0; break; }
                        SetColor(order[demoIndex], show);
                        Note(order[demoIndex], 0.6f);
                        demoIndex++;
                        timer = 0.7f;
                    }
                    break;

                case State.Input:
                    CheckTouch(player);
                    break;
            }
        }

        private Vector2 Center
        {
            get
            {
                Vector2 sum = Vector2.zero;
                foreach (Vector2 v in neurons) sum += v;
                return neurons.Length > 0 ? sum / neurons.Length : (Vector2)transform.position;
            }
        }

        private void StartDemo(float wait)
        {
            state = State.Demo;
            demoIndex = 0;
            timer = wait;
            for (int i = 0; i < cores.Count; i++) SetColor(i, idle);
        }

        private void CheckTouch(PlayerController player)
        {
            if (touchCooldown > 0f) return;
            Vector2 body = player.Position + Vector2.up * 0.9f;
            for (int i = 0; i < neurons.Length; i++)
            {
                if (Vector2.Distance(body, neurons[i]) > 1.1f) continue;
                if (cores[i].color == good) return;   // ya estaba bien
                touchCooldown = 0.3f;
                if (i == order[progress])
                {
                    SetColor(i, good);
                    Note(i, 0.8f);
                    progress++;
                    if (progress >= order.Length) Solve();
                }
                else
                {
                    SetColor(i, bad);
                    Sfx.Effect("Secuencia_Error", 0.7f);
                    DamageInfo info = DamageInfo.Create(5f, DamageKind.Contact, Team.Enemy, neurons[i]);
                    info.knockback = 5f;
                    player.Health.ApplyDamage(info);
                    GameplayEvents.RaiseMessage("Orden equivocado. Mira otra vez.");
                    StartDemo(1.2f);
                }
                return;
            }
        }

        /// <summary>Cada neurona tiene su nota (Secuencia_Nota_0..4): así el orden también se aprende de oído.</summary>
        private void Note(int i, float volume) =>
            Sfx.PlayAt(Sfx.LoadFirst("Secuencia_Nota_" + (i % 5), "Neurona_Carga"), neurons[i], CombatAudio.Effects * volume, 0f);

        private void Solve()
        {
            state = State.Solved;
            Sfx.Effect("Secuencia_Correcta", 0.9f);
            GameplayEvents.RaiseMessage("¡Circuito completo! La compuerta se abre.");
            if (gate != null) Destroy(gate);
            foreach (SpriteRenderer r in cores) r.color = good;
        }

        private void SetColor(int i, Color c)
        {
            if (i < 0 || i >= cores.Count) return;
            cores[i].color = c;
            bodies[i].localScale = c == show || c == good ? Vector3.one * 1.15f : Vector3.one;
        }
    }
}
