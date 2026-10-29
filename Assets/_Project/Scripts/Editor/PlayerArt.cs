using UnityEditor;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Arte del caballero (Assets/_Project/Art/Player). Son 4 PNG:
    ///   Caballero_Cuerpo        cuerpo sin el brazo (pivote: entre los pies)
    ///   Caballero_Cuerpo_Color  lo que cambia de color en el cuerpo (cresta, ojos, círculo del casco, articulaciones), en blanco
    ///   Caballero_Brazo         brazo con el cañón, horizontal hacia la derecha (pivote: el hombro, para que gire al apuntar)
    ///   Caballero_Brazo_Color   lo que cambia de color en el cañón, en blanco
    /// Las piezas "_Color" van encima y el código les pone el color del medicamento (SpriteRenderer.color).
    ///
    /// Esta clase deja la importación lista sola (Sprite, PPU y pivotes). Si cambias los PNG por otros del mismo
    /// tamaño y pose, no hay que tocar nada; si cambias la pose, ajusta los números de abajo.
    /// </summary>
    internal static class PlayerArt
    {
        public const string Folder = "Assets/_Project/Art/Player";

        /// <summary>Píxeles por unidad: el caballero mide 1645 px de los pies a la punta de la cresta = ~1.95 unidades.</summary>
        public const float PixelsPerUnit = 850f;

        // Pivotes (0..1 dentro de cada imagen, desde la esquina inferior izquierda).
        private static readonly Vector2 BodyPivot = new Vector2(316f / 849f, 12f / 1663f);
        private static readonly Vector2 ArmPivot = new Vector2(66.1f / 656f, 117.16f / 219f);

        /// <summary>Dónde va el hombro, medido desde el pivote del cuerpo (en unidades).</summary>
        public static readonly Vector2 Shoulder = new Vector2(-24.9f / PixelsPerUnit, 979.16f / PixelsPerUnit);

        /// <summary>Del hombro a la punta del cañón (en unidades): de ahí salen las cápsulas.</summary>
        public const float CannonLength = 588.9f / PixelsPerUnit;

        public static Sprite Body => Load("Caballero_Cuerpo", BodyPivot);
        public static Sprite BodyColor => Load("Caballero_Cuerpo_Color", BodyPivot);
        public static Sprite Arm => Load("Caballero_Brazo", ArmPivot);
        public static Sprite ArmColor => Load("Caballero_Brazo_Color", ArmPivot);

        public static bool Available => Body != null && Arm != null;

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
