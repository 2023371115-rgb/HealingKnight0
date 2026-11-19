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

        private static Sprite glow, ring, beam, vignette;

        /// <summary>Mancha de luz suave (centro opaco que se desvanece hacia la orilla). 1 unidad, centrada.</summary>
        public static Sprite Glow => glow != null ? glow : (glow = Make(64, 64, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            float k = Mathf.Clamp01(1f - d);
            return k * k;
        }));

        /// <summary>Aro delgado y suave (orilla de burbuja, onda de un "plop"). 1 unidad, centrado.</summary>
        public static Sprite Ring => ring != null ? ring : (ring = Make(64, 64, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            float x = (d - 0.86f) / 0.09f;
            return d > 1f ? 0f : Mathf.Exp(-x * x);
        }));

        /// <summary>Rayo de luz: nace arriba (pivote arriba al centro) y se desvanece hacia abajo y a los lados. 1 x 1.</summary>
        public static Sprite Beam => beam != null ? beam : (beam = Make(64, 64, new Vector2(0.5f, 1f), (u, v) =>
        {
            float side = Mathf.Clamp01(1f - Mathf.Abs(u));
            float along = Mathf.Clamp01((v + 1f) * 0.5f);   // 1 arriba, 0 abajo
            return side * side * Mathf.Pow(along, 1.4f);
        }));

        /// <summary>Viñeta: transparente al centro y oscura en las orillas (para dar profundidad al fondo). 1 x 1.</summary>
        public static Sprite Vignette => vignette != null ? vignette : (vignette = Make(64, 64, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u * 0.85f + v * v);
            float k = Mathf.Clamp01((d - 0.55f) / 0.75f);
            return Mathf.Pow(k, 1.5f);
        }));

        /// <summary>Crea un sprite cuadrado de 'w' x 'h' píxeles (mide 1 x 1 unidad si w = h) con la transparencia que diga 'alpha'(u, v), u y v de -1 a 1.</summary>
        private static Sprite Make(int w, int h, Vector2 pivot, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f)));
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, w);
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
    
        /// <summary>Vuelve 'go' una plataforma de un solo sentido de 'width' de ancho con la superficie en su origen.</summary>
        public static BoxCollider2D OneWay(GameObject go, float width, float thickness = 0.35f)
        {
            go.layer = Movement.PhysicsLayers.Ground;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, thickness);
            box.offset = new Vector2(0f, -thickness * 0.5f);
            box.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            return box;
        }

        /// <summary>¿Están los pies del jugador parados sobre una plataforma de ancho 'width' con superficie en 'top'?</summary>
        public static bool Standing(Player.PlayerController p, Vector2 top, float width)
        {
            if (p == null || p.Motor == null || !p.Health.IsAlive) return false;
            Vector2 f = p.Position;
            return Mathf.Abs(f.x - top.x) <= width * 0.5f + 0.15f && f.y >= top.y - 0.2f && f.y <= top.y + 0.15f && p.Motor.Velocity.y <= 0.5f;
        }
    }
}
