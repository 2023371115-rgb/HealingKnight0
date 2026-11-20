using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>Barra simple (vida, escudo): cambia el ancho del relleno.</summary>
    public class UIBar : MonoBehaviour
    {
        private Image fill;
        private float target = 1f;
        private float shown = 1f;

        public void Init(Image fillImage)
        {
            fill = fillImage;
            Apply(1f);
        }

        public void SetValue(float value01, bool instant = false)
        {
            target = Mathf.Clamp01(value01);
            if (instant)
            {
                shown = target;
                Apply(shown);
            }
        }

        public void SetColor(Color color)
        {
            if (fill != null) fill.color = color;
        }

        private void Update()
        {
            if (Mathf.Approximately(shown, target)) return;
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 2.5f);
            Apply(shown);
        }

        private void Apply(float v)
        {
            if (fill == null) return;
            fill.rectTransform.anchorMax = new Vector2(v, 1f);
        }
    }
}
