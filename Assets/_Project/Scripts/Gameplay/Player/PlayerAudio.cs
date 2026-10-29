using HealingKnight.Gameplay.Audio;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Sonidos del jugador que no son de disparo: salto, doble salto, daño, muerte, escudo y bombas.
    /// PlayerController lo agrega solo si el prefab no lo tiene. Los archivos van en Audio/Resources/SFX:
    /// Jugador_Salto, Jugador_SaltoDoble, Jugador_Herido, Jugador_Muere, Escudo_Bloquea, Escudo_Parry,
    /// Escudo_Rompe, Bomba_Lanza, Bomba_Ganada.
    /// </summary>
    public class PlayerAudio : MonoBehaviour
    {
        private PlayerController controller;
        private PlayerShield shield;
        private PlayerBombs bombs;
        private int lastBombs = -1;
        private bool suppressBlock;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            shield = GetComponent<PlayerShield>();
            bombs = GetComponent<PlayerBombs>();
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Jumped += OnJumped;
                if (controller.Health != null)
                {
                    controller.Health.Damaged += OnDamaged;
                    controller.Health.Died += OnDied;
                }
            }
            if (shield != null)
            {
                shield.Parried += OnParried;
                shield.Blocked += OnBlocked;
                shield.Broke += OnBroke;
            }
            if (bombs != null) bombs.CountChanged += OnBombs;
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Jumped -= OnJumped;
                if (controller.Health != null)
                {
                    controller.Health.Damaged -= OnDamaged;
                    controller.Health.Died -= OnDied;
                }
            }
            if (shield != null)
            {
                shield.Parried -= OnParried;
                shield.Blocked -= OnBlocked;
                shield.Broke -= OnBroke;
            }
            if (bombs != null) bombs.CountChanged -= OnBombs;
        }

        private void LateUpdate() => suppressBlock = false;

        private void OnJumped(bool airJump) => Sfx.Effect(airJump ? "Jugador_SaltoDoble" : "Jugador_Salto", 0.8f);
        private void OnDamaged() => Sfx.Effect("Jugador_Herido");
        private void OnDied() => Sfx.Effect("Jugador_Muere");
        private void OnBroke() => Sfx.Effect("Escudo_Rompe");

        private void OnParried()
        {
            Sfx.Effect("Escudo_Parry");
            suppressBlock = true; // el parry también avisa "bloqueó": que no suenen los dos
        }

        private void OnBlocked()
        {
            if (!suppressBlock) Sfx.Effect("Escudo_Bloquea", 0.8f);
        }

        private void OnBombs(int count)
        {
            if (lastBombs >= 0)
            {
                if (count < lastBombs) Sfx.Effect("Bomba_Lanza", 0.8f);
                else if (count == lastBombs + 1) Sfx.Effect("Bomba_Ganada"); // ganada peleando (al recargar el nivel no suena)
            }
            lastBombs = count;
        }
    }
}
