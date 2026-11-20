using System.Collections.Generic;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Rastro del dash: deja copias del caballero en azul clarito que se desvanecen (como los fantasmas de
    /// velocidad de las caricaturas). Copia los dibujos del muñeco tal como están en ese momento.
    /// </summary>
    public class PlayerDashTrail : MonoBehaviour
    {
        [SerializeField] private float every = 0.045f;
        [SerializeField] private float life = 0.22f;
        [SerializeField] private Color tint = new Color(0.55f, 0.9f, 1f, 0.55f);

        private PlayerController controller;
        private CharacterMotor2D motor;
        private float next;
        private readonly List<SpriteRenderer> parts = new List<SpriteRenderer>();

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            motor = GetComponent<CharacterMotor2D>();
        }

        private void Update()
        {
            if (controller == null || !controller.IsDashing) return;
            if (Time.time < next) return;
            next = Time.time + every;
            Spawn();
        }

        private void Spawn()
        {
            Transform source = motor != null && motor.Visual != null ? motor.Visual : transform;
            parts.Clear();
            source.GetComponentsInChildren(false, parts);
            if (parts.Count == 0) return;

            var ghost = new GameObject("RastroDash");
            var fade = ghost.AddComponent<Ghost>();
            fade.life = life;
            foreach (SpriteRenderer p in parts)
            {
                if (p == null || !p.enabled || p.sprite == null) continue;
                var copy = new GameObject(p.name);
                copy.transform.SetParent(ghost.transform, false);
                copy.transform.SetPositionAndRotation(p.transform.position, p.transform.rotation);
                copy.transform.localScale = p.transform.lossyScale;
                var r = copy.AddComponent<SpriteRenderer>();
                r.sprite = p.sprite;
                r.flipX = p.flipX;
                r.flipY = p.flipY;
                r.sortingLayerID = p.sortingLayerID;
                r.sortingOrder = p.sortingOrder - 20;
                r.color = tint;
                fade.renderers.Add(r);
            }
        }

        private class Ghost : MonoBehaviour
        {
            public float life = 0.2f;
            public readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            private float t;
            private float baseAlpha = -1f;

            private void Update()
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / life);
                foreach (SpriteRenderer r in renderers)
                {
                    if (r == null) continue;
                    if (baseAlpha < 0f) baseAlpha = r.color.a;
                    Color c = r.color;
                    c.a = baseAlpha * k;
                    r.color = c;
                }
                if (t >= life) Destroy(gameObject);
            }
        }
    }
}
