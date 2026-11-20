using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Efecto de un momento (rayo, nube de tos, destello): se desvanece en 'life' segundos, se puede ir moviendo
    /// de lado y luego se borra solo. Se agrega por código: go.AddComponent&lt;FadeAndDie&gt;().Configure(0.4f).
    /// </summary>
    public class FadeAndDie : MonoBehaviour
    {
        private float life = 0.4f, age, driftX, grow = 0.6f;
        private SpriteRenderer[] parts;
        private float[] alphas;
        private Vector3 baseScale;

        public void Configure(float seconds, float drift = 0f, float growth = 0.6f)
        {
            life = Mathf.Max(0.05f, seconds);
            driftX = drift;
            grow = growth;
        }

        private void Start()
        {
            parts = GetComponentsInChildren<SpriteRenderer>(true);
            alphas = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) alphas[i] = parts[i].color.a;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / life);
            transform.position += new Vector3(driftX * Time.deltaTime, 0f, 0f);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (1f + grow * k * 0.3f), baseScale.z) * (1f + grow * k * 0.2f);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                Color c = parts[i].color;
                c.a = alphas[i] * (1f - k);
                parts[i].color = c;
            }
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
