using HealingKnight.Gameplay.Audio;
using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Campo neuroprotector que deja la bomba de anticonvulsivo: durante unos segundos
    /// las neuronas sanas dentro del círculo no reciben daño de nadie (ni tuyo ni de los enemigos).
    /// </summary>
    public class NeuroprotectionField : MonoBehaviour
    {
        private static readonly List<NeuroprotectionField> active = new List<NeuroprotectionField>();

        private float radius;
        private float remaining;
        private float duration;
        private SpriteRenderer sprite;
        private Color color;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();

        public static bool Protects(Vector2 point)
        {
            for (int i = 0; i < active.Count; i++)
            {
                NeuroprotectionField f = active[i];
                if (f != null && Vector2.Distance(point, f.transform.position) <= f.radius) return true;
            }
            return false;
        }

        public static NeuroprotectionField Create(Vector2 position, float radius, float seconds, Sprite circle, Color tint)
        {
            Sfx.EffectAt("Campo_Neuroprotector", position);
            var go = new GameObject("CampoNeuroprotector");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var field = go.AddComponent<NeuroprotectionField>();
            field.radius = radius;
            field.duration = field.remaining = seconds;
            field.color = tint;
            if (circle != null)
            {
                field.sprite = go.AddComponent<SpriteRenderer>();
                field.sprite.sprite = circle;
                field.sprite.sortingOrder = 25;
                go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            }
            return field;
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Update()
        {
            remaining -= Time.deltaTime;
            if (sprite != null)
            {
                // Burbuja dorada que late y parpadea cuando está por acabarse.
                float k = remaining / Mathf.Max(0.01f, duration);
                float blink = remaining < 2f ? (Mathf.Sin(Time.time * 18f) > 0f ? 1f : 0.4f) : 1f;
                Color c = color;
                c.a = (0.12f + Mathf.Sin(Time.time * 4f) * 0.03f) * blink * Mathf.Lerp(0.6f, 1f, k);
                sprite.color = c;
            }
            if (remaining <= 0f) Destroy(gameObject);
        }
    }
}
