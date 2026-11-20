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
}
