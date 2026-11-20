using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// CÉLULA SIN OXÍGENO en la pared de un capilar (tramo 4). Está AZUL y triste; si pasas cerca un momento le llevas
    /// oxígeno, se pone ROSITA y te cura un poquito. Si se lo llevas a casi todas, premio (vida y una bomba).
    /// Biología: en los capilares los glóbulos rojos sueltan el oxígeno a las células y se llevan el CO2.
    /// </summary>
    public class OxygenCell : MonoBehaviour
    {
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private float needed = 0.35f;
        [SerializeField] private float heal = 3f;

        private static readonly List<OxygenCell> all = new List<OxygenCell>();
        private static int done;
        private static bool rewarded;

        private float progress;
        private bool oxygenated;
        private SpriteRenderer body, outline;
        private Transform face, smile, frown, sparkle;
        private float t;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { all.Clear(); done = 0; rewarded = false; }

        private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        private void OnDisable() => all.Remove(this);

        private void Start()
        {
            var art = new GameObject("Dibujo").transform;
            art.SetParent(transform, false);
            outline = CodeSprites.Add(art, CodeSprites.Circle, new Color(0.1f, 0.15f, 0.35f), 1, Vector2.zero, new Vector2(1.05f, 0.95f), "Contorno");
            body = CodeSprites.Add(art, CodeSprites.Circle, new Color(0.45f, 0.6f, 0.95f), 2, Vector2.zero, new Vector2(0.92f, 0.82f), "Celula");
            CodeSprites.Add(art, CodeSprites.Circle, new Color(0.3f, 0.35f, 0.7f), 3, new Vector2(0.16f, 0.12f), new Vector2(0.3f, 0.26f), "Nucleo");
            face = new GameObject("Cara").transform;
            face.SetParent(art, false);
            CodeSprites.Add(face, CodeSprites.Circle, Color.black, 4, new Vector2(-0.18f, 0.08f), Vector2.one * 0.09f, "Ojo");
            CodeSprites.Add(face, CodeSprites.Circle, Color.black, 4, new Vector2(0.02f, 0.08f), Vector2.one * 0.09f, "Ojo");
            frown = CodeSprites.Add(face, CodeSprites.Square, Color.black, 4, new Vector2(-0.08f, -0.14f), new Vector2(0.2f, 0.05f), "Boca").transform;
            frown.localRotation = Quaternion.Euler(0f, 0f, 12f);
            smile = new GameObject("Sonrisa").transform;
            smile.SetParent(face, false);
            CodeSprites.Add(smile, CodeSprites.Square, Color.black, 4, new Vector2(-0.15f, -0.12f), new Vector2(0.12f, 0.05f), "Boca").transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            CodeSprites.Add(smile, CodeSprites.Square, Color.black, 4, new Vector2(0f, -0.12f), new Vector2(0.12f, 0.05f), "Boca").transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            smile.gameObject.SetActive(false);
            sparkle = CodeSprites.Add(art, CodeSprites.Glow, new Color(1f, 0.75f, 0.85f, 0f), 0, Vector2.zero, Vector2.one * 2.2f, "Brillo").transform;
        }

        private void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            t += dt;
            // Respira despacito (más contenta cuando ya tiene oxígeno).
            float breathe = 1f + Mathf.Sin(t * (oxygenated ? 3f : 1.4f)) * 0.04f;
            body.transform.parent.localScale = Vector3.one * breathe;
            if (oxygenated)
            {
                SpriteRenderer glow = sparkle.GetComponent<SpriteRenderer>();
                Color g = glow.color;
                g.a = Mathf.MoveTowards(g.a, 0f, dt * 0.5f);
                glow.color = g;
                return;
            }

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 ship = player.Position + new Vector2(0f, 0.6f) * player.Size;
            bool near = Vector2.Distance(ship, transform.position) < radius;
            progress = Mathf.Clamp(progress + (near ? dt : -dt * 0.5f), 0f, needed);
            // Se va pintando de rosa mientras le llevas oxígeno.
            float k = progress / needed;
            body.color = Color.Lerp(new Color(0.45f, 0.6f, 0.95f), new Color(1f, 0.6f, 0.7f), k);
            if (progress >= needed) Oxygenate(player);
        }

        private void Oxygenate(PlayerController player)
        {
            oxygenated = true;
            body.color = new Color(1f, 0.62f, 0.72f);
            outline.color = new Color(0.5f, 0.15f, 0.25f);
            frown.gameObject.SetActive(false);
            smile.gameObject.SetActive(true);
            SpriteRenderer glow = sparkle.GetComponent<SpriteRenderer>();
            glow.color = new Color(1f, 0.75f, 0.85f, 0.7f);
            player.Health.Heal(heal);
            done++;
            Sfx.Effect("Recoger_Vida", 0.5f);
            int total = Mathf.Max(done, all.Count);
            SpeedBanner.Show("OXÍGENO " + done + "/" + total, new Color(1f, 0.7f, 0.8f), 0.9f);
            if (done == 1)
                GameplayEvents.RaiseMessage("¡Eso! Le llevaste OXÍGENO y se puso rosita. Así trabajan los glóbulos rojos en los capilares.");
            if (!rewarded && done >= Mathf.CeilToInt(total * 0.8f) && total >= 5)
            {
                rewarded = true;
                player.Health.Heal(20f);
                if (player.Bombs != null) player.Bombs.Add(1);
                GameplayEvents.RaiseMessage("¡Casi todas las células tienen oxígeno! Premio: +vida y +1 bomba.");
            }
        }
    }
}
