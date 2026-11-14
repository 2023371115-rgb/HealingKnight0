using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>Utilidades para crear carpetas, assets y asignar campos privados desde el Editor.</summary>
    internal static class SetupUtils
    {
        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Carga el asset si existe; si no, lo crea con 'init'. Nunca sobrescribe lo que el equipo ya editó.</summary>
        public static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ---- Asignar campos [SerializeField] privados ----

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) Debug.LogError($"[Setup] No existe el campo '{field}' en {so.targetObject.GetType().Name}.");
            return p;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector2(Object target, string field, Vector2 value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRect(Object target, string field, Rect value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.rectValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRefArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Objetos de escena / prefabs ----

        public static GameObject Child(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        public static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, Color color,
            Vector3 localPosition, Vector2 scale, int sortingOrder)
        {
            GameObject go = Child(parent, name, localPosition);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        public static T SavePrefab<T>(GameObject root, string path) where T : Component
        {
            EnsureFolder(Path.GetDirectoryName(path));
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved != null ? saved.GetComponent<T>() : null;
        }
    }
}
