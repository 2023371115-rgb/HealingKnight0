using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>Hace que un elemento flote despacio arriba y abajo (y se ladee un poco). Para títulos e insignias.</summary>
    public class UIBob : MonoBehaviour
    {
        public float height = 10f;
        public float speed = 1.4f;
        public float tilt = 1.5f;
        private Vector2 basePos;
        private float seed;
        private RectTransform rt;

        private void Start()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            seed = Random.value * 6f;
        }

        private void Update()
        {
            float t = Time.unscaledTime * speed + seed;
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(t) * height);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * tilt);
        }
    }

    /// <summary>
    /// Fondo vivo: microbios del juego (los mismos dibujos de los niveles) que flotan despacio por la pantalla,
    /// girando, a distintas distancias (los lejanos más chicos, más lentos y más transparentes).
    /// </summary>
    public class FloatingMicrobes : MonoBehaviour
    {
        private class Bug
        {
            public RectTransform rt;
            public Vector2 velocity;
            public float spin, wobble, seed;
        }

        private readonly List<Bug> bugs = new List<Bug>();
        private RectTransform area;

        public void Init(Sprite[] sprites, int count)
        {
            area = (RectTransform)transform;
            if (sprites == null || sprites.Length == 0) return;
            for (int i = 0; i < count; i++)
            {
                Sprite sp = sprites[i % sprites.Length];
                if (sp == null) continue;
                float depth = Random.Range(0.35f, 1f);         // 1 = cerca
                Image img = CartoonUI.Image(transform, "Microbio", sp, new Color(1f, 1f, 1f, Mathf.Lerp(0.18f, 0.55f, depth)));
                img.preserveAspect = true;
                float size = Mathf.Lerp(70f, 190f, depth);
                Vector2 ratio = sp.rect.size / Mathf.Max(sp.rect.width, sp.rect.height);
                img.rectTransform.sizeDelta = ratio * size;
                img.rectTransform.anchoredPosition = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-600f, 600f));
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                bugs.Add(new Bug
                {
                    rt = img.rectTransform,
                    velocity = new Vector2(Random.Range(-25f, 25f), Random.Range(10f, 40f)) * depth,
                    spin = Random.Range(-14f, 14f),
                    wobble = Random.Range(10f, 30f),
                    seed = Random.value * 10f
                });
            }
        }

        private void Update()
        {
            if (area == null) return;
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
            float hw = Mathf.Max(1000f, area.rect.width * 0.5f + 150f), hh = Mathf.Max(600f, area.rect.height * 0.5f + 150f);
            foreach (Bug b in bugs)
            {
                Vector2 p = b.rt.anchoredPosition + b.velocity * dt + new Vector2(Mathf.Sin(t * 0.8f + b.seed) * b.wobble * dt, 0f);
                if (p.y > hh) p.y = -hh;
                if (p.x > hw) p.x = -hw; else if (p.x < -hw) p.x = hw;
                b.rt.anchoredPosition = p;
                b.rt.Rotate(0f, 0f, b.spin * dt);
            }
        }
    }

    /// <summary>Aparece la pantalla con un fundido desde negro.</summary>
    public class UIFadeIn : MonoBehaviour
    {
        public float duration = 0.5f;
        private Image img;
        private float t;

        private void Awake() => img = GetComponent<Image>();

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            if (img != null) img.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / duration));
            if (t >= duration) Destroy(gameObject);
        }
    }
}
