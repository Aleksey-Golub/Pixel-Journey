using System.Collections.Generic;
using Code.Services;
using Code.UI;
using Code.UI.Services;
using UnityEngine;

namespace Code.Infrastructure
{
    public class GameFactory : IGameFactory
    {
        public List<ISavedProgressReader> ProgressReaders { get; } = new List<ISavedProgressReader>();
        public List<ISavedProgressWriter> ProgressWriters { get; } = new List<ISavedProgressWriter>();

        private readonly IAssetProvider _assets;
        private readonly IConfigsService _configs;
        private readonly IPersistentProgressService _progressService;
        private readonly IUIMediator _uiMediator;
        private readonly IAudioService _audio;
        private readonly IInputService _input;
        private readonly IPopupFactory _popupFactory;
        private readonly IResourceFactory _resourceFactory;
        private readonly IEffectFactory _effectFactory;
        private readonly IDropCountCalculatorService _dropCountCalculatorService;
        private readonly ITransitionalResourceFactory _transitionalResourceFactory;
        private GameObject _heroGameObject;

        public GameFactory(
            IAssetProvider assets,
            IConfigsService configs,
            IPersistentProgressService persistentProgressService,
            IUIMediator uiMediator,
            IAudioService audio,
            IInputService input,
            IPopupFactory popupFactory,
            ITransitionalResourceFactory transitionalResourceFactory,
            IResourceFactory resourceFactory,
            IEffectFactory effectFactory,
            IDropCountCalculatorService dropCountCalculatorService
            )
        {
            _assets = assets;
            _configs = configs;
            _progressService = persistentProgressService;
            _uiMediator = uiMediator;
            _audio = audio;
            _input = input;
            _popupFactory = popupFactory;
            _resourceFactory = resourceFactory;
            _effectFactory = effectFactory;
            _dropCountCalculatorService = dropCountCalculatorService;
            _transitionalResourceFactory = transitionalResourceFactory;
        }

        public GameObject CreateHero(GameObject at)
        {
            _heroGameObject = InstantiateRegistered(AssetPath.HERO_PATH, at.transform.position);
            _heroGameObject.GetComponent<Player>().Construct(_input, _uiMediator.GetPlayerInventoryView(), _configs, _popupFactory, _transitionalResourceFactory, _progressService);

            return _heroGameObject;
        }

        public Hud CreateHud()
        {
            Hud hud = InstantiateRegistered(AssetPath.HUD_PATH).GetComponent<Hud>();

            hud.PlayerInventoryView.Construct(_configs);

            foreach (OpenWindowButton openWindowButton in hud.GetComponentsInChildren<OpenWindowButton>())
                openWindowButton.Construct(_uiMediator, _audio);

            return hud;
        }

        public ResourceSource CreateResourceSource(ResourceSourceType type, Vector3 at)
        {
            ResourceSourceMatcher rSourceMatcher = _configs.GetMatcherFor(type);
            ResourceSource resourceSource = InstantiateRegistered(rSourceMatcher.Template, at);

            resourceSource.Construct(_resourceFactory, _dropCountCalculatorService, _audio, _effectFactory);

            return resourceSource;
        }

        public ResourceStorage CreateResourceStorage(ResourceStorageType type, Vector3 at)
        {
            ResourceStorageMatcher rStorageMatcher = _configs.GetMatcherFor(type);
            ResourceStorage resourceStorage = InstantiateRegistered(rStorageMatcher.Template, at);

            resourceStorage.Construct(_resourceFactory, _progressService, _audio, _effectFactory);

            return resourceStorage;
        }

        public SimpleObject CreateSimpleObject(SimpleObjectType type, Vector3 at)
        {
            SimpleObjectMatcher simpleObjectMatcher = _configs.GetMatcherFor(type);
            SimpleObject simpleObject = InstantiateRegistered(simpleObjectMatcher.Template, at);

            switch (type)
            {
                case SimpleObjectType.SellBoard:
                    (simpleObject as SellBoard).Construct(_uiMediator, _configs);
                    break;
                case SimpleObjectType.UpgradeBoard:
                    (simpleObject as UpgradeBoard).Construct(_uiMediator, _configs, _progressService);
                    break;
                case SimpleObjectType.None:
                default:
                    break;
            }

            return simpleObject;
        }

        public void Cleanup()
        {
            ProgressReaders.Clear();
            ProgressWriters.Clear();
        }

        public void RegisterProgressWatchers(GameObject gameObject)
        {
            foreach (ISavedProgressReader progressReader in gameObject.GetComponentsInChildren<ISavedProgressReader>())
                ProgressReaders.Add(progressReader);

            foreach (ISavedProgressWriter progressWriter in gameObject.GetComponentsInChildren<ISavedProgressWriter>())
                ProgressWriters.Add(progressWriter);
        }

        private GameObject InstantiateRegistered(string prefabPath, Vector3 at)
        {
            GameObject gameObject = _assets.Instantiate(path: prefabPath, at: at);
            RegisterProgressWatchers(gameObject);

            return gameObject;
        }

        private T InstantiateRegistered<T>(T prefab) where T : MonoBehaviour
        {
            T monoDehaviour = Object.Instantiate<T>(prefab);
            RegisterProgressWatchers(monoDehaviour.gameObject);

            return monoDehaviour;
        }

        private T InstantiateRegistered<T>(T prefab, Vector3 at) where T : MonoBehaviour
        {
            T monoDehaviour = Object.Instantiate<T>(prefab, at, Quaternion.identity);
            RegisterProgressWatchers(monoDehaviour.gameObject);

            return monoDehaviour;
        }

        private GameObject InstantiateRegistered(string prefabPath)
        {
            GameObject gameObject = _assets.Instantiate(path: prefabPath);
            RegisterProgressWatchers(gameObject);

            return gameObject;
        }
    }
}