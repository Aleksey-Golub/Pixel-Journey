﻿using Code.Services;
using Code.UI.Services;
using System.Collections;

namespace Code.Infrastructure
{
    public class BootstrapState : IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly AllServices _services;

        public BootstrapState(GameStateMachine stateMachine, SceneLoader sceneLoader, AllServices services, ICoroutineRunner coroutineRunner, IUpdater updater)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _services = services;

            RegisterServices(coroutineRunner, updater);
        }

        public void Enter() =>
          _sceneLoader.Load(Scenes.INITIAL, onLoaded: OnSceneLoaded);

        public void Exit()
        {
        }

        private void RegisterServices(ICoroutineRunner coroutineRunner, IUpdater updater)
        {
            _services.RegisterSingle<IGameStateMachine>(_stateMachine);
            _services.RegisterSingle<ICoroutineRunner>(coroutineRunner);
            _services.RegisterSingle<IUpdater>(updater);

            RegisterAdsService();
            _services.RegisterSingle<ITimeService>(new TimeService());
            RegisterLocalizationService();
            _services.RegisterSingle<IAssetProvider>(new AssetProvider());
            RegisterConfigService();
            RegisterInputService();
            _services.RegisterSingle<IPersistentProgressService>(new PersistentProgressService());
            _services.RegisterSingle<IAppSettingsService>(new AppSettingsService());
            RegisterAudioService(coroutineRunner);

            _services.RegisterSingle<IDropCountCalculatorService>(new DropCountCalculatorService(
                _services.Single<IPersistentProgressService>(),
                _services.Single<IConfigsService>()));

            _services.RegisterSingle<IPopupFactory>(new PopupFactory(
                _services.Single<IAssetProvider>()));
            _services.RegisterSingle<IResourceFactory>(new ResourceFactory(
                _services.Single<IAudioService>(),
                _services.Single<IAssetProvider>(),
                _services.Single<IPersistentProgressService>()
                ));
            RegisterResourceMergeService();
            _services.RegisterSingle<IToolFactory>(new ToolFactory(
                _services.Single<IAudioService>(),
                _services.Single<IAssetProvider>(),
                _services.Single<IPersistentProgressService>()
                ));
            _services.RegisterSingle<ITransitionalResourceFactory>(new TransitionalResourceFactory(
                _services.Single<IAudioService>(),
                _services.Single<IAssetProvider>()));
            _services.RegisterSingle<IEffectFactory>(new EffectFactory(
                _services.Single<IConfigsService>()));

            _services.RegisterSingle<ISaveLoadAppSettingsService>(new SaveLoadAppSettingsService(
                _services.Single<IAppSettingsService>(),
                _services.Single<IAudioService>()));

            _services.RegisterSingle<IUIFactory>(new UIFactory(
              _services.Single<IAssetProvider>(),
              _services.Single<IConfigsService>(),
              _services.Single<IPersistentProgressService>(),
              _services.Single<IAudioService>(),
              _services.Single<ISaveLoadAppSettingsService>(),
              _services.Single<IAdsService>(),
              _services.Single<IUpdater>()
              ));

            _services.RegisterSingle<IUIMediator>(new UIMediator(_services.Single<IUIFactory>()));

            _services.RegisterSingle<IGameFactory>(new GameFactory(
              _services.Single<IAssetProvider>(),
              _services.Single<IConfigsService>(),
              _services.Single<IPersistentProgressService>(),
              _services.Single<IUIMediator>(),
              _services.Single<IAudioService>(),
              _services.Single<IInputService>(),
              _services.Single<IPopupFactory>(),
              _services.Single<ITransitionalResourceFactory>(),
              _services.Single<IResourceFactory>(),
              _services.Single<IToolFactory>(),
              _services.Single<IEffectFactory>(),
              _services.Single<IDropCountCalculatorService>(),
              _services.Single<IAdsService>()
              ));

            _services.RegisterSingle<ISaveLoadService>(new SaveLoadService(
              _services.Single<IPersistentProgressService>(),
              _services.Single<IGameFactory>(),
              _services.Single<IResourceFactory>(),
              _services.Single<IToolFactory>()
              ));
        }

        private void RegisterResourceMergeService()
        {
            IResourceMergeService rms = new ResourceMergeService(_services.Single<IResourceFactory>());
            rms.Load();
            _services.RegisterSingle(rms);

            _services.Single<IUpdater>().Updatables.Add(rms);
        }

        private void RegisterAudioService(ICoroutineRunner coroutineRunner)
        {
            IAudioService audio = new AudioService(coroutineRunner);
            audio.Load();
            _services.RegisterSingle(audio);
        }

        private void RegisterInputService()
        {
            IInputService input = GetInputService();
            input.Init();
            _services.RegisterSingle<IInputService>(input);
        }

        private void RegisterConfigService()
        {
            IConfigsService configs = new ConfigsService();
            configs.Load();
            _services.RegisterSingle(configs);
        }

        private void RegisterAdsService()
        {
            IAdsService adsService = new AdsService();
            adsService.Initialize();
            _services.RegisterSingle<IAdsService>(adsService);
        }

        private void RegisterLocalizationService()
        {
            LService.Load();

            //ILocalizationService localization = new LocalizationService();
            //localization.Load();
            //_services.RegisterSingle(localization);
        }

        private static IInputService GetInputService()
        {
            if (UnityEngine.Application.isMobilePlatform)
                return new MobileInput();
            else
                return new DesktopInput();
        }

        private void OnSceneLoaded()
        {
            _services.Single<ICoroutineRunner>().StartCoroutine(OnSceneLoadedCor());
        }

        private IEnumerator OnSceneLoadedCor()
        {
            while (!PlatformLayer.IsInitialized)
            {
                yield return null;
            }

            Logger.LogWarning($"[BootstrapState] PlatformLayer is isInitialized on {PlatformLayer.PlatformName}");

            var ads = AllServices.Container.Single<IAdsService>();
            Logger.Log($"[BootstrapState] available ads: sticky= {ads.IsStickyAvailable()}, preload= {ads.IsPreloaderAvailable()}, fullscreen= {ads.IsFullscreenAvailable()}, rewarded= {ads.IsRewardedAvailable()}");

            _stateMachine.Enter<LoadAppSettingsState>();
        }
    }
}