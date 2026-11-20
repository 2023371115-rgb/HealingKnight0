using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
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
