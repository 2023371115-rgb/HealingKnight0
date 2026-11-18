using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Punto donde se engancha el HILO DENTAL (Boca). Puede ser:
    ///  - un gancho fijo en un diente (para columpiarte), o
    ///  - algo que se ARRANCA de un jalón (placa entre dientes, placas del jefe Sarro): 'ripTime' > 0.
    /// Se dibuja por código (un aro brillante) salvo que 'drawRing' sea falso (el dueño lo dibuja).
    /// </summary>
    public class FlossHook : MonoBehaviour
    {
        public static readonly List<FlossHook> All = new List<FlossHook>();

        [SerializeField] private float ripTime;
        [SerializeField] private bool drawRing = true;
        [SerializeField] private float ropeLength = 3.6f;

        private Transform ring;
        private float pulled;

        /// <summary>Se dispara cuando el jalón lo arranca.</summary>
        public event Action<FlossHook> Ripped;

        public bool Rippable => ripTime > 0f;
        public float RopeLength => ropeLength;
        public bool Attached { get; private set; }

        public void Configure(float rip, bool ring, float length)
        {
            ripTime = rip;
            drawRing = ring;
            ropeLength = length;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() { All.Remove(this); Attached = false; }

        private void Start()
        {
            if (!drawRing) return;
            ring = new GameObject("Gancho").transform;
            ring.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(ring, c, new Color(1f, 1f, 1f, 0.25f), 8, Vector2.zero, new Vector2(1.1f, 1.1f));
            CodeSprites.Add(ring, c, new Color(0.15f, 0.25f, 0.4f, 1f), 9, Vector2.zero, new Vector2(0.62f, 0.62f));
            CodeSprites.Add(ring, c, new Color(0.55f, 0.95f, 1f, 1f), 10, Vector2.zero, new Vector2(0.48f, 0.48f));
            CodeSprites.Add(ring, c, new Color(0.15f, 0.25f, 0.4f, 1f), 11, Vector2.zero, new Vector2(0.24f, 0.24f));
            CodeSprites.Add(ring, c, Color.white, 12, new Vector2(-0.1f, 0.1f), new Vector2(0.1f, 0.08f));
        }

        private void Update()
        {
            if (ring != null) ring.localScale = Vector3.one * (Attached ? 1.15f : 1f + Mathf.Sin(Time.time * 4f + transform.position.x) * 0.06f);
        }

        public void SetAttached(bool on)
        {
            Attached = on;
            if (!on) pulled = 0f;
        }

        /// <summary>Lo llama el hilo cada cuadro que está jalando; devuelve true si ya se arrancó.</summary>
        public bool Pull(float dt)
        {
            if (!Rippable) return false;
            pulled += dt;
            transform.localPosition += (Vector3)(UnityEngine.Random.insideUnitCircle * 0.02f);
            if (pulled < ripTime) return false;
            Ripped?.Invoke(this);
            return true;
        }
    }
}
