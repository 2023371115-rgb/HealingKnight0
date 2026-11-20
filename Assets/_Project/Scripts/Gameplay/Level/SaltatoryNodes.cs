using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// CONDUCCIÓN SALTATORIA: en los axones con mielina, el impulso no recorre todo el cable, "salta" de un
    /// NODO DE RANVIER (los huecos entre las vainas de mielina) al siguiente. Por eso es tan rápido.
    /// Aquí los nodos son plataformas que se encienden en orden, de izquierda a derecha: solo el nodo encendido
    /// (y el anterior, un momento) son sólidos. Sigue al impulso saltando de nodo en nodo.
    /// Cuando llega al último descansa un poco y vuelve a empezar.
    /// </summary>
    public class SaltatoryNodes : MonoBehaviour
    {
        [SerializeField] private Vector2[] nodes;
        [SerializeField] private float step = 0.9f;
        [SerializeField] private float rest = 1.6f;
        [SerializeField] private float width = 1.5f;
        [SerializeField] private Color lit = new Color(1f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color dark = new Color(0.3f, 0.35f, 0.6f, 0.45f);
        [SerializeField] private Color myelin = new Color(0.6f, 0.5f, 0.95f, 0.55f);

        private readonly List<BoxCollider2D> boxes = new List<BoxCollider2D>();
        private readonly List<SpriteRenderer> cores = new List<SpriteRenderer>();
        private readonly List<Transform> halos = new List<Transform>();
        private float time;
        private int lastActive = -2;

        public void Configure(Vector2[] points, float stepTime, float nodeWidth)
        {
            nodes = points;
            step = stepTime;
            width = nodeWidth;
        }

        private float Period => nodes.Length * step + rest;

        private void Start()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                var go = new GameObject("NodoRanvier" + i);
                go.transform.SetParent(transform, true);
                go.transform.position = nodes[i];
                boxes.Add(CodeSprites.OneWay(go, width));
                Sprite c = CodeSprites.Circle;
                halos.Add(CodeSprites.Add(go.transform, c, new Color(lit.r, lit.g, lit.b, 0.3f), -12, new Vector2(0f, -0.15f), new Vector2(width + 1f, 1.2f)).transform);
                CodeSprites.Add(go.transform, c, new Color(0.08f, 0.06f, 0.2f, 1f), -11, new Vector2(0f, -0.2f), new Vector2(width + 0.14f, 0.55f));
                cores.Add(CodeSprites.Add(go.transform, c, dark, -10, new Vector2(0f, -0.2f), new Vector2(width, 0.42f)));

                // Vaina de mielina entre este nodo y el siguiente (adorno: muestra el "cable").
                if (i + 1 < nodes.Length)
                {
                    Vector2 a = nodes[i], b = nodes[i + 1];
                    Vector2 mid = (a + b) * 0.5f;
                    float len = Vector2.Distance(a, b) - width - 0.3f;
                    if (len > 0.3f)
                    {
                        SpriteRenderer m = CodeSprites.Add(transform, c, myelin, -13, Vector2.zero, new Vector2(len, 0.5f), "Mielina");
                        m.transform.position = new Vector3(mid.x, mid.y - 0.2f, 0f);
                        m.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                    }
                }
            }
        }

        private void Update()
        {
            if (nodes == null || nodes.Length == 0) return;
            // Solo corre si el jugador está cerca (para que la primera vez empiece desde el primer nodo).
            PlayerController player = PlayerController.Current;
            bool near = player != null && player.Position.x > nodes[0].x - 8f && player.Position.x < nodes[nodes.Length - 1].x + 4f;
            if (near) time += Time.deltaTime;
            float t = Mathf.Repeat(time, Period);
            int active = t < nodes.Length * step ? Mathf.FloorToInt(t / step) : -1;
            float within = active >= 0 ? (t - active * step) / step : 0f;

            for (int i = 0; i < nodes.Length; i++)
            {
                bool on = i == active;
                bool trailing = i == active - 1 && within < 0.35f;      // el anterior aguanta un poquito
                bool upcoming = i == active + 1 && within > 0.6f;       // el siguiente empieza a brillar (aviso)
                bool restingFirst = active < 0 && i == 0;               // en el descanso el primero queda encendido
                bool solid = on || trailing || restingFirst;
                if (boxes[i].enabled != solid) boxes[i].enabled = solid;
                Color col = solid ? lit : upcoming ? Color.Lerp(dark, lit, 0.5f) : dark;
                cores[i].color = Color.Lerp(cores[i].color, col, 1f - Mathf.Exp(-20f * Time.deltaTime));
                halos[i].gameObject.SetActive(on || restingFirst);
                if (on) halos[i].localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 14f) * 0.1f);
            }
            if (active != lastActive)
            {
                if (active >= 0 && near) Sfx.EffectAt("Neurona_Descarga", nodes[active], 0.35f);
                lastActive = active;
            }
        }
    }
}
