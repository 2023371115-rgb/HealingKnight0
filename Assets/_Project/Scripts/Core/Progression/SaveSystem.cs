using System;
using System.IO;
using UnityEngine;

namespace HealingKnight.Core.Progression
{
    /// <summary>Guarda y carga el progreso como JSON en la carpeta de datos del usuario.</summary>
    public static class SaveSystem
    {
        private const string FileName = "healingknight_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new SaveData();

                string json = File.ReadAllText(FilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] No se pudo leer el guardado, se empieza de cero: {e.Message}");
                return new SaveData();
            }
        }

        public static void Write(SaveData data)
        {
            if (data == null) return;
            try
            {
                data.version = SaveData.CurrentVersion;
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] No se pudo guardar: {e.Message}");
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] No se pudo borrar el guardado: {e.Message}");
            }
        }
    }
}
