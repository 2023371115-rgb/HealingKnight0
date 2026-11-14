using UnityEngine;

namespace HealingKnight.Gameplay
{
    /// <summary>
    /// Figuras básicas para dibujar por código (círculo y cuadro de 1 unidad) y un atajo para poner una capa de color.
    /// Las usan los objetos que todavía no tienen sprite (o que se dibujan así a propósito, como líquidos).
    /// </summary>
    public static class CodeSprites
    {
        private static Sprite circle, square;

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 64;
                var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 30f));
                    }
                tex.SetPixels(px);
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                return circle;
            }
        }

        public static Sprite Square
        {
            get
            {
                if (square != null) return square;
                var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                return square;
            }
        }

        /// <summary>Capa de color hija de 'parent' (posición y tamaño locales en unidades).</summary>
        public static SpriteRenderer Add(Transform parent, Sprite sprite, Color color, int order, Vector2 pos, Vector2 size, string name = "Capa")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        /// <summary>Un dibujo (sprite) escalado para medir 'width' de ancho.</summary>
        public static SpriteRenderer Art(Transform parent, Sprite sprite, float width, int order, Vector2 pos, string name = "Dibujo")
        {
            float k = sprite != null && sprite.bounds.size.x > 0.001f ? width / sprite.bounds.size.x : 1f;
            return Add(parent, sprite, Color.white, order, pos, new Vector2(k, k), name);
        }
    }
}
