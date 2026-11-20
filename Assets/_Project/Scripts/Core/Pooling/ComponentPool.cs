using System;
using UnityEngine;
using UnityEngine.Pool;

namespace HealingKnight.Core.Pooling
{
    /// <summary>
    /// Pool de objetos para cápsulas, proyectiles y efectos: se reutilizan en lugar de
    /// Instantiate/Destroy durante el combate (GDD 9.6).
    /// </summary>
    public class ComponentPool<T> where T : Component
    {
        private readonly ObjectPool<T> pool;
        private readonly T prefab;
        private readonly Transform parent;

        public int CountActive => pool.CountActive;

        public ComponentPool(T prefab, int defaultCapacity, int maxSize, Transform parent = null, Action<T> onCreate = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    T instance = UnityEngine.Object.Instantiate(this.prefab, this.parent);
                    instance.gameObject.SetActive(false);
                    onCreate?.Invoke(instance);
                    return instance;
                },
                actionOnGet: item => item.gameObject.SetActive(true),
                actionOnRelease: item => item.gameObject.SetActive(false),
                actionOnDestroy: item =>
                {
                    if (item != null) UnityEngine.Object.Destroy(item.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public T Get() => pool.Get();

        public void Release(T item)
        {
            if (item != null && item.gameObject.activeSelf) pool.Release(item);
        }

        public void Clear() => pool.Clear();
    }
}
