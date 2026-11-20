using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// LÓBULO TEMPORAL (oído y música): las plataformas aparecen AL RITMO. Hay dos grupos (A y B) que se turnan en
    /// cada compás: cuando uno está sólido el otro es solo una silueta. Medio tiempo antes del cambio la silueta
    /// parpadea (aviso). Salta siguiendo el ritmo: "un, dos, un, dos".
    /// </summary>
    public class BeatPlatforms : MonoBehaviour
    {
        [SerializeField] private Vector3[] groupA;     // x, y (superficie), ancho
        [SerializeField] private Vector3[] groupB;
        [SerializeField] private float beat = 1.25f;
        [SerializeField] private Color colorA = new Color(0.4f, 0.85f, 1f, 1f);
        [SerializeField] private Color colorB = new Color(1f, 0.5f, 0.85f, 1f);

        private class Pad { public BoxCollider2D box; public SpriteRenderer fill, line; public int group; public Transform note; }
        private readonly List<Pad> pads = new List<Pad>();
        private int lastBeat = -1;

        public void Configure(Vector3[] a, Vector3[] b, float beatTime)
        {
            groupA = a;
            groupB = b;
            beat = beatTime;
        }

        private void Start()
        {
            Create(groupA, 0, colorA);
            Create(groupB, 1, colorB);
        }

        private void Create(Vector3[] list, int group, Color color)
        {
            if (list == null) return;
            foreach (Vector3 v in list)
            {
                var go = new GameObject(group == 0 ? "RitmoA" : "RitmoB");
                go.transform.SetParent(transform, true);
                go.transform.position = new Vector3(v.x, v.y, 0f);
                var pad = new Pad { box = CodeSprites.OneWay(go, v.z), group = group };
                Sprite c = CodeSprites.Circle;
                pad.line = CodeSprites.Add(go.transform, c, new Color(0.06f, 0.03f, 0.15f, 1f), -11, new Vector2(0f, -0.2f), new Vector2(v.z + 0.16f, 0.6f));
                pad.fill = CodeSprites.Add(go.transform, c, color, -10, new Vector2(0f, -0.2f), new Vector2(v.z, 0.46f));
                // Notita musical encima (se lee "es del ritmo").
                pad.note = new GameObject("Nota").transform;
                pad.note.SetParent(go.transform, false);
                pad.note.localPosition = new Vector3(0f, -0.2f, 0f);
                CodeSprites.Add(pad.note, c, new Color(1f, 1f, 1f, 0.85f), -9, new Vector2(-0.1f, 0f), new Vector2(0.24f, 0.18f));
                CodeSprites.Add(pad.note, CodeSprites.Square, new Color(1f, 1f, 1f, 0.85f), -9, new Vector2(0.02f, 0.2f), new Vector2(0.05f, 0.4f));
                pads.Add(pad);
            }
        }

        private void Update()
        {
            float t = Time.timeSinceLevelLoad / beat;
            int index = Mathf.FloorToInt(t);
            float within = t - index;
            int solidGroup = index % 2;
            bool warn = within > 0.6f;

            if (index != lastBeat)
            {
                lastBeat = index;
                PlayerController p = PlayerController.Current;
                if (p != null && pads.Count > 0 && Mathf.Abs(p.Position.x - pads[0].box.transform.position.x) < 25f)
                    Sfx.Effect("Pulso_Ritmo", 0.35f);
            }

            foreach (Pad pad in pads)
            {
                bool solid = pad.group == solidGroup;
                if (pad.box.enabled != solid) pad.box.enabled = solid;
                float alpha = solid ? 1f : warn && Mathf.Sin(within * 60f) > 0f ? 0.6f : 0.18f;
                if (solid && warn) alpha = 0.75f + 0.25f * Mathf.Sin(within * 40f);
                Color f = pad.fill.color; f.a = alpha; pad.fill.color = f;
                Color l = pad.line.color; l.a = solid ? 1f : alpha * 0.8f; pad.line.color = l;
                float pulse = solid ? 1f + Mathf.Max(0f, 0.25f - within) * 0.6f : 0.9f;
                pad.fill.transform.localScale = new Vector3(pad.fill.transform.localScale.x, 0.46f * pulse, 1f);
                pad.note.localScale = Vector3.one * (solid ? 1f + Mathf.Max(0f, 0.3f - within) : 0.7f);
            }
        }
    }
}
