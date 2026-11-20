using UnityEditor;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Ajustes de importación de la música adaptativa (Audio/Resources/MusicAdaptive/...):
    ///  - "Decompress On Load": el audio se descomprime al cargar el nivel. Así las 6 capas pueden empezar
    ///    en el mismo instante exacto (con audio en "streaming" se pueden desfasar unos milisegundos).
    ///  - Vorbis calidad 70 %: en memoria pesa poco más; en disco sigue comprimido.
    ///  - Precargado: listo para sonar en cuanto se carga la escena.
    /// Se aplica solo al importar; no hay que hacer nada a mano.
    /// </summary>
    public class MusicAdaptiveImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/MusicAdaptive/")) return;
            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
        }
    }
}
