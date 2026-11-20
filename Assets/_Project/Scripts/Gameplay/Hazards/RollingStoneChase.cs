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
    /// PIEDRA DEL RIÑÓN que rueda detrás de ti por el uréter: cuando pasas 'startX' aparece y te persigue. Si te
    /// alcanza duele y te avienta. Se rompe al llegar a 'endX'.
    /// </summary>
    public class RollingStoneChase : MonoBehaviour
    {
        [SerializeField] private float startX, endX, floorY;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private float damage = 16f;

        private Transform stone;
        private float x, spin, hurtCooldown;
        private bool running, done;

        public void Configure(float fromX, float toX, float groundY, float chaseSpeed)
        {
            startX = fromX; endX = toX; floorY = groundY; speed = chaseSpeed;
        }

        private void Start()
        {
            stone = new GameObject("Piedra").transform;
            stone.SetParent(transform, false);
            CodeSprites.Add(stone, CodeSprites.Circle, new Color(0.3f, 0.22f, 0.1f), 40, Vector2.zero, Vector2.one * radius * 2.1f, "Contorno");
            CodeSprites.Add(stone, CodeSprites.Circle, new Color(0.82f, 0.7f, 0.42f), 41, Vector2.zero, Vector2.one * radius * 2f, "Piedra");
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51f * Mathf.Deg2Rad;
                CodeSprites.Add(stone, CodeSprites.Circle, new Color(0.65f, 0.55f, 0.3f), 42, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * 0.6f, Vector2.one * 0.6f, "Cristal");
            }
            stone.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (done || stone == null) return;
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            if (!running)
            {
                if (player.Position.x < startX) return;
                running = true;
                x = startX - 10f;
                stone.gameObject.SetActive(true);
                GameplayEvents.RaiseMessage("¡Una PIEDRA del riñón (un cálculo) rueda por el URÉTER detrás de ti! ¡CORRE!");
            }
            if (player.Position.x < x - 4f) x = player.Position.x - 10f;
            x += speed * dt;
            spin -= speed * dt / radius * Mathf.Rad2Deg;
            stone.position = new Vector3(x, floorY + radius, 0f);
            stone.localRotation = Quaternion.Euler(0f, 0f, spin);
            if (x > endX)
            {
                done = true;
                stone.gameObject.SetActive(false);
                Sfx.EffectAt("Coagulo_Disuelve", new Vector2(x, floorY), 1f);
                GameplayEvents.RaiseMessage("¡La piedra se atoró y se hizo pedazos! (Por eso duelen tanto los cálculos: tapan el uréter.)");
                return;
            }
            if (hurtCooldown > 0f) { hurtCooldown -= dt; return; }
            if (!player.Health.IsAlive) return;
            if (Vector2.Distance(player.Motor.Center, stone.position) < radius + 0.4f)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, stone.position);
                info.knockback = 10f;
                player.Health.ApplyDamage(info);
                hurtCooldown = 1f;
            }
        }
    }
}
