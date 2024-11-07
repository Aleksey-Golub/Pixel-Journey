﻿using System.Collections.Generic;
using UnityEngine;
using Code.Infrastructure;

namespace Code.Services
{
    internal class ResourceFactory : IResourceFactory
    {
        private readonly Pool<Resource> _pool;
        private readonly List<Resource> _droppedResources;
        private readonly IAudioService _audio;
        private readonly IPersistentProgressService _progressService;

        public IReadOnlyList<Resource> DroppedResources => _droppedResources;

        public ResourceFactory(IAudioService audio, IAssetProvider assets, IPersistentProgressService progressService)
        {
            _audio = audio;
            _progressService = progressService;

            Transform container = CreateContainer();
            Resource resourcePrefab = assets.Load<Resource>(AssetPath.RESOURCE_PREFAB_PATH);
            int poolSize = 10;

            _pool = new Pool<Resource>(resourcePrefab, container, poolSize);
            _droppedResources = new List<Resource>();
        }

        public Resource Get(Vector3 position, Quaternion rotation)
        {
            Resource res = _pool.Get(position, rotation);

            if (!res.IsConstructed)
                res.Construct(this, _audio, _progressService);
            
            res.UniqueId.GenerateId();

            _droppedResources.Add(res);

            return res;
        }

        public void Recycle(IPoolable resource)
        {
            _droppedResources.Remove(resource as Resource);

            _pool.Recycle(resource as Resource);
        }

        private Transform CreateContainer()
        {
            var go = new GameObject("Resource Factory Container");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.transform;
        }
    }
}