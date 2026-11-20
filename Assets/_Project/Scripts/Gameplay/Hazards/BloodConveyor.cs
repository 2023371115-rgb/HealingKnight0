using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// BANDA DE GLÓBULOS (pulpa roja del bazo): el piso es un río lento de glóbulos rojos que te lleva hacia un lado
    /// (como banda transportadora). Ir en contra cuesta; a favor vas rapidísimo.
    /// Biología: en la pulpa roja la sangre pasa despacito para que el bazo revise cada glóbulo.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BloodConveyor : MonoBehaviour
    {
        [SerializeField] private float minX, maxX = 20f, floorY;
        [SerializeField] private float pushSpeed = 3f;
        [SerializeField] private int direction = 1;

        private readonly List<Transform> discs = new List<Transform>();
        private float offset;

        public void Configure(float from, float to, float groundY, float push, int dir)
        {
            minX = from; maxX = to; floorY = groundY; pushSpeed = push; direction = dir >= 0 ? 1 : -1;
        }

        private void Start()
        {
            // Glóbulos acostados que ruedan sobre el piso.
            for (float x = minX + 0.4f; x < maxX; x += 1.1f)
            {
                Transform d = new GameObject("Globulo").transform;
                d.SetParent(transform, false);
                CodeSprites.Add(d, CodeSprites.Circle, new Color(0.35f, 0.02f, 0.06f), -58, Vector2.zero, new Vector2(1f, 0.42f), "Contorno");
                CodeSprites.Add(d, CodeSprites.Circle, new Color(0.86f, 0.18f, 0.22f), -57, Vector2.zero, new Vector2(0.9f, 0.34f), "Disco");
                CodeSprites.Add(d, CodeSprites.Circle, new Color(0.65f, 0.08f, 0.12f), -56, Vector2.zero, new Vector2(0.4f, 0.12f), "Centro");
                discs.Add(d);
            }
            // Flechitas en el piso que dicen hacia dónde va.
            for (float x = minX + 1.5f; x < maxX - 1f; x += 4f)
                for (int s = -1; s <= 1; s += 2)
                {
                    SpriteRenderer a = CodeSprites.Add(transform, CodeSprites.Square, new Color(1f, 0.8f, 0.8f, 0.5f), -55,
                        new Vector2(x - transform.position.x, floorY - transform.position.y - 0.25f + s * 0.07f), new Vector2(0.32f, 0.06f), "Flecha");
                    a.transform.localRotation = Quaternion.Euler(0f, 0f, s * 35f * direction);
                }
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || !player.Health.IsAlive) return;
            Vector2 f = player.Position;
            if (f.x >= minX && f.x <= maxX && f.y < floorY + 0.3f && player.Motor.IsGrounded)
                player.Motor.Body.position += new Vector2(direction * pushSpeed * Time.fixedDeltaTime, 0f);
        }

        private void Update()
        {
            offset = Mathf.Repeat(offset + pushSpeed * Time.deltaTime, 1.1f);
            for (int i = 0; i < discs.Count; i++)
            {
                float x = minX + 0.4f + i * 1.1f + direction * offset;
                if (x > maxX) x -= maxX - minX;
                if (x < minX) x += maxX - minX;
                discs[i].position = new Vector3(x, floorY + 0.12f, 0f);
                discs[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + i) * 6f);
            }
        }
    }
}
