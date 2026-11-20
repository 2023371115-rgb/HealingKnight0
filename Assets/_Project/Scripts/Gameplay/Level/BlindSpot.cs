using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>PUNTO CIEGO (Ojo): donde sale el nervio óptico no hay conos ni bastones. Aquí nadie te ve y la barra baja rápido.</summary>
    public class BlindSpot : MonoBehaviour
    {
        [SerializeField] private float width = 6f;
        [SerializeField, TextArea] private string message = "PUNTO CIEGO: aquí sale el nervio óptico y no hay células que vean. Nadie te detecta. Respira.";
        private bool said;

        public void Configure(float w) => width = w;

        private void Start()
        {
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.95f, 0.85f, 0.55f, 0.18f), -60, new Vector2(0f, 2.5f), new Vector2(width, 5.5f));
        }

        private void Update()
        {
            PlayerController p = PlayerController.Current;
            StealthMeter m = StealthMeter.Current;
            if (p == null || m == null || Mathf.Abs(p.Position.x - transform.position.x) > width * 0.5f) return;
            m.MarkBlindSpot();
            if (!said) { said = true; GameplayEvents.RaiseMessage(message); }
        }
    }
}
