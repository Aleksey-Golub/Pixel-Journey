using UnityEngine;

internal class Workbench : MonoBehaviour, IResourceConsumer
{
    [SerializeField] private ResourceConsumerView _view;
    [SerializeField] private Collider2D _collider;

    [SerializeField] private ResourceConfig _needResourceConfig;
    [SerializeField] private int _needResourceCount = 1;
    [SerializeField] private int _preferedConsumedValue = -1;
    [SerializeField] private Transform _transitionalResourceFinal;

    [SerializeField] private ScriptableObject _dropConfigMono;
    [SerializeField] private DropSettings _dropSettings = DropSettings.Default;
    [SerializeField] private int _dropCount = 1;

    private IDropObjectConfig _dropConfig;
    private int _currentNeedResourceCount;
    private int _currentPreUpload;
    private ResourceFactory _resourceFactory;
    private ToolFactory _toolFactory;

    public bool CanInteract => _currentNeedResourceCount != 0 && _currentPreUpload < _needResourceCount;
    public int PreferedConsumedValue => _preferedConsumedValue;
    public int FreeSpace => _needResourceCount - _currentPreUpload;

    public Vector3 TransitionalResourceFinalPosition => _transitionalResourceFinal.position;

    private void OnValidate()
    {
        if (_dropConfigMono is IDropObjectConfig config)
            _dropConfig = config;
        else
            _dropConfigMono = null;
    }

    private void Start()
    {
        var resourceFactory = ResourceFactory.Instance;
        var toolFactory = ToolFactory.Instance;
        Construct(resourceFactory, toolFactory);
        Init();
    }

    private void Construct(ResourceFactory resourceFactory, ToolFactory toolFactory)
    {
        _resourceFactory = resourceFactory;
        _toolFactory = toolFactory;
    }

    internal void Init()
    {
        _currentNeedResourceCount = _needResourceCount;
        _currentPreUpload = 0;

        _view.Init(_needResourceConfig.Sprite, _currentNeedResourceCount, _dropConfig.Sprite);
    }

    public ResourceConsumerNeeds GetNeeds()
    {
        return new ResourceConsumerNeeds()
        {
            ResourceType = _needResourceConfig.Type,
            CurrentNeedResourceCount = _currentNeedResourceCount
        };
    }

    public void Consume(int value)
    {
        _currentNeedResourceCount -= value;
        _view.ShowNeeds(_currentNeedResourceCount);

        if (_currentNeedResourceCount == 0)
        {
            _view.ShowHitAnimation();
            DropObject();
            Exhaust();
        }
    }

    public void ApplyPreUpload(int consumedValue)
    {
        _currentPreUpload += consumedValue;
    }

    private void Exhaust()
    {
        _view.ShowExhaust();

        Invoke(nameof(DisableCollider), 1f);
    }

    private void DisableCollider()
    {
        _collider.enabled = false;
    }

    private void DropObject()
    {
        _view.PlayDropResourceSound();

        var dropData = DropData.Get(transform.position, _dropSettings, _dropCount, out int notFittedInPacksCount);

        if (_dropConfig is ResourceConfig resourceConfig)
        {
            for (int i = 0; i < dropData.Count; i++)
            {
                Resource dropObject = _resourceFactory.Get(transform.position, Quaternion.identity);
                dropObject.Init(resourceConfig, dropData[i].ResourceInPackCount);

                dropObject.MoveAfterDrop(dropData[i]);
            }
        }
        else if (_dropConfig is ToolConfig toolConfig)
        {
            for (int i = 0; i < dropData.Count; i++)
            {
                Tool dropObject = _toolFactory.Get(transform.position, Quaternion.identity);
                dropObject.Init(toolConfig);

                dropObject.MoveAfterDrop(dropData[i]);
            }
        }
        else
        {
            Logger.LogError($"[Workbench] DropObject() error : 'Not implemented for {_dropConfig.GetType()}'");
        }
    }
}

