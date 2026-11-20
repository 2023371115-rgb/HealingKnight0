using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Texto dentro del nivel (letreros y avisos de botón) con la letra del juego (Titan One).
    /// La letra se convierte a TextMeshPro una sola vez al empezar (igual que hace el menú).
    /// </summary>
    public static class WorldText
    {
        private static readonly Dictionary<Font, TMP_FontAsset> cache = new Dictionary<Font, TMP_FontAsset>();

        /// <summary>Letra que usan los textos que no traen una propia (la pone el HUD).</summary>
        public static Font DefaultFont { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cache.Clear();
            DefaultFont = null;
        }

        public static TMP_FontAsset FontAsset(Font font)
        {
            if (font == null) font = DefaultFont;
            if (font == null) return null;
            if (cache.TryGetValue(font, out TMP_FontAsset asset) && asset != null) return asset;
            asset = TMP_FontAsset.CreateFontAsset(font);
            cache[font] = asset;
            return asset;
        }

        /// <summary>Texto centrado en 'center' (local) que se ajusta solo al tamaño 'box' (unidades del mundo).</summary>
        public static TextMeshPro Create(Transform parent, string text, Vector2 center, Vector2 box, Color color, int order, Font font = null, float maxSize = 8f)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var tmp = go.AddComponent<TextMeshPro>();
            TMP_FontAsset asset = FontAsset(font);
            if (asset != null) tmp.font = asset;
            tmp.rectTransform.sizeDelta = box;
            tmp.text = text;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.5f;
            tmp.fontSizeMax = maxSize;
            tmp.richText = true;
            tmp.sortingOrder = order;
            return tmp;
        }
    }
}
