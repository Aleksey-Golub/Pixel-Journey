﻿using System.Collections.Generic;
using UnityEngine;

namespace Code.Services
{
    internal class Pool<T> where T : MonoBehaviour, IPoolable
    {
        private readonly Queue<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly IRecyclableFactory _factory;
        private readonly IAudioService _audio;

        public Pool(T prefab, Transform parent, int initialPoolSize, IRecyclableFactory factory, IAudioService audio)
        {
            _prefab = prefab;
            _parent = parent;
            _factory = factory;
            _audio = audio;
            _pool = new Queue<T>(initialPoolSize);
            FillPool(initialPoolSize);
        }

        internal T Get(Vector3 position, Quaternion rotation)
        {
            if (_pool.TryDequeue(out var obj))
            {
                obj.gameObject.SetActive(true);
            }
            else
            {
                obj = Object.Instantiate(_prefab, _parent);
                obj.Construct(_factory, _audio);
            }

            obj.transform.SetPositionAndRotation(position, rotation);

            return obj;
        }

        internal void Recycle(T go)
        {
            go.gameObject.SetActive(false);

            _pool.Enqueue(go);
        }

        private void FillPool(int initialPoolSize)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var newGO = Object.Instantiate(_prefab, _parent);
                newGO.Construct(_factory, _audio);

                newGO.gameObject.SetActive(false);

                _pool.Enqueue(newGO);
            }
        }
    }

    public interface IPoolable
    {
        void Construct(IRecyclableFactory factory, IAudioService audio);
    }

    public interface IRecyclableFactory : IService
    {
        void Recycle(IPoolable poolable);
    }
}