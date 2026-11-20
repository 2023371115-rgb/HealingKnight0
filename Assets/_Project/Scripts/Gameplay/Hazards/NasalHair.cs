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
