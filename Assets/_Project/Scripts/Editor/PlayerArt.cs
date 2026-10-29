using UnityEditor;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Arte del caballero (Assets/_Project/Art/Player), cortado en piezas para animarlo por código
    /// (como un muñeco articulado). Cada pieza tiene su PNG y su "_Color" (lo que pinta el medicamento, en blanco):
    ///
    ///   Torso                cabeza, torso y cadera          pivote: la cadera (articulación de la pierna de enfrente)
    ///   PiernaFrente_Muslo   muslo                           pivote: la cadera
    ///   PiernaFrente_Pie     rodilla, espinilla y pie        pivote: el centro de la rodilla
    ///   (la pierna de atrás usa el MISMO dibujo que la de enfrente, un poco inclinada: así las dos son
    ///    simétricas y completas; en el dibujo original la de atrás estaba tapada en parte)
    ///   Brazo                brazo con el cañón              pivote: el hombro (gira al apuntar)
    ///
    /// Esta clase deja la importación lista (Sprite, píxeles por unidad y pivotes) y da dónde va cada pieza.
    /// Las posiciones están en unidades, medidas desde el punto entre los pies.
    /// Si redibujas una pieza con el mismo tamaño y pose, no hay que tocar nada aquí.
    /// </summary>
    internal static class PlayerArt
    {
        public const string Folder = "Assets/_Project/Art/Player";

        /// <summary>Píxeles por unidad: el caballero mide 1645 px de los pies a la punta de la cresta = ~1.95 unidades.</summary>
        public const float PixelsPerUnit = 850f;

        public struct Piece
        {
            public string file;
            public Vector2 pivot;     // 0..1 dentro de la imagen, desde abajo a la izquierda
            public Vector2 position;  // dónde va el pivote, en unidades desde los pies
            public Piece(string file, Vector2 pivot, Vector2 position) { this.file = file; this.pivot = pivot; this.position = position; }
        }

        public static readonly Piece Torso = new Piece("Caballero_Torso", new Vector2(0.3784f, 0.0640f), new Vector2(0.0062f, 0.7662f));
        public static readonly Piece FrontThigh = new Piece("Caballero_PiernaFrente_Muslo", new Vector2(0.3385f, 0.8317f), new Vector2(0.0062f, 0.7662f));
        public static readonly Piece FrontShin = new Piece("Caballero_PiernaFrente_Pie", new Vector2(0.3104f, 0.8699f), new Vector2(0.0679f, 0.4548f));
        /// <summary>Cadera de la pierna de atrás (usa las piezas de la de enfrente).</summary>
        public static readonly Vector2 BackHip = new Vector2(-0.0813f, 0.7765f);

        /// <summary>Inclinación de la pierna de atrás en reposo (grados; negativo = hacia atrás), como en el dibujo.</summary>
        public const float BackLegRestAngle = -16.9f;

        public static readonly Piece Arm = new Piece("Caballero_Brazo", new Vector2(66.1f / 656f, 117.16f / 219f), new Vector2(-24.9f / 850f, 979.16f / 850f));

        /// <summary>Del hombro a la punta del cañón (en unidades): de ahí salen las cápsulas.</summary>
        public const float CannonLength = 588.9f / PixelsPerUnit;

        public static bool Available => Sprite(Torso) != null && Sprite(Arm) != null && Sprite(FrontThigh) != null;

        public static Sprite Sprite(Piece p) => Load(p.file, p.pivot);
        public static Sprite ColorSprite(Piece p) => Load(p.file + "_Color", p.pivot);

        private static Sprite Load(string name, Vector2 pivot)
        {
            string path = $"{Folder}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool needs = importer.textureType != TextureImporterType.Sprite
                         || importer.spriteImportMode != SpriteImportMode.Single
                         || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
                         || settings.spriteAlignment != (int)SpriteAlignment.Custom
                         || (settings.spritePivot - pivot).sqrMagnitude > 1e-6f;
            if (needs)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
