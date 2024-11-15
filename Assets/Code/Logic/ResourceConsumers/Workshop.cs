using Code.Infrastructure;
using Code.Services;
using UnityEngine;
using Code.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

[SelectionBase]
public class Workshop : SingleUseConsumerBase<ResourceConsumerView>
{
    [SerializeField] private SpawnGameObjectData[] _spawnDatas;

    private IGameFactory _gameFactory;

    #region EDITOR
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        foreach (SpawnGameObjectData data in _spawnDatas)
        {
            Handles.Label(data.Point.position, data.GameObjectId);
        }
    }
#endif
    #endregion

    private void Start()
    {
        var audio = AllServices.Container.Single<IAudioService>();
        var effectFactory = AllServices.Container.Single<IEffectFactory>();
        var gameFactory = AllServices.Container.Single<IGameFactory>();

        Construct(audio, effectFactory, gameFactory);
        Init();
    }

    public void Construct(IAudioService audio, IEffectFactory effectFactory, IGameFactory gameFactory)
    {
        Construct();
        _gameFactory = gameFactory;

        View.Construct(audio, effectFactory);
    }

    protected override Sprite GetGenerateObjSprite() => null;

    protected override void DropObject()
    {
        View.PlayDropResourceSound();
        View.ShowHitEffect();

        foreach (SpawnGameObjectData data in _spawnDatas)
            _gameFactory.GetGameObject(data.GameObjectId, at: data.Point.position);
    }

    public override void WriteToProgress(GameProgress progress)
    {
        //throw new System.NotImplementedException();
    }

    public override void ReadProgress(GameProgress progress)
    {
        //throw new System.NotImplementedException();
    }

    protected override void Accept(ICreatedByIdGameObjectVisitor visitor) => visitor.Visit(this);
}
