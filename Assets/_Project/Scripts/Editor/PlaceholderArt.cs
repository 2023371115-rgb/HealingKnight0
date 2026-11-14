using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Genera sprites blancos sencillos (cuadro, círculo, cápsula) para armar todo en gris
    /// mientras se dibuja el arte final. Se tiñen con el color del SpriteRenderer.
    /// </summary>
    internal static class PlaceholderArt
    {
        public const string Folder = "Assets/_Project/Art/Placeholders";
        private const int PixelsPerUnit = 64;

        public static Sprite Square => Get("Square", 64, 64, (x, y, w, h) => 1f, new Vector2(0.5f, 0f));
        public static Sprite SquareCentered => Get("SquareCentered", 64, 64, (x, y, w, h) => 1f, new Vector2(0.5f, 0.5f));
        public static Sprite Circle => Get("Circle", 64, 64, Ellipse, new Vector2(0.5f, 0.5f));
        public static Sprite CircleBottom => Get("CircleBottom", 64, 64, Ellipse, new Vector2(0.5f, 0f));
        public static Sprite Capsule => Get("Capsule", 64, 32, RoundedBar, new Vector2(0.5f, 0.5f));

        private delegate float Shape(int x, int y, int w, int h);

        private static float Ellipse(int x, int y, int w, int h)
        {
            float nx = (x + 0.5f) / w * 2f - 1f;
            float ny = (y + 0.5f) / h * 2f - 1f;
            float d = nx * nx + ny * ny;
            return Mathf.Clamp01((1f - d) * 12f);
        }

        private static float RoundedBar(int x, int y, int w, int h)
        {
            float r = h * 0.5f;
            float cx = Mathf.Clamp(x + 0.5f, r, w - r);
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01((r - d) * 1.5f);
        }

        private static Sprite Get(string name, int width, int height, Shape shape, Vector2 pivot)
        {
            string path = $"{Folder}/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            SetupUtils.EnsureFolder(Folder);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(shape(x, y, width, height)) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
