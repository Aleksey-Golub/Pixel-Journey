﻿using Code.Services;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal class PlayerView : MonoBehaviour
{
    [SerializeField] private GameObject _mindCloud;
    [SerializeField] private Image _toolMindImage;
    [SerializeField] private Animator _animator;
    [SerializeField] private Vector3 _popupSpawnOffset = Vector3.up;
    [SerializeField] private TargetFollower _speedUpParticlesPrefab;

    private IPopupFactory _popupFactory;
    private int _dirXHash;
    private int _dirYHash;
    private int _velocityHash;
    private int _attackHash;

    private ParticleSystem _speedUpParticles;

    private Dictionary<ResourceType, GatheredResourcesPopupInfo> _gatheredResourcesPopups;

    internal event Action AttackDone;

    internal void Construct(IPopupFactory popupFactory)
    {
        _popupFactory = popupFactory;

        _dirXHash = Animator.StringToHash("DirX");
        _dirYHash = Animator.StringToHash("DirY");
        _velocityHash = Animator.StringToHash("Velocity");
        _attackHash = Animator.StringToHash("Attack");

        _gatheredResourcesPopups = new();
    }

    internal void PlayMove(Vector2 direction, float velocity) => PlayMove(direction.x, direction.y, velocity);

    internal void PlayMove(float dirX, float dirY, float velocity)
    {
        _animator.SetFloat(_dirXHash, dirX);
        _animator.SetFloat(_dirYHash, dirY);
        _animator.SetFloat(_velocityHash, velocity);
    }

    internal void PlayAttack()
    {
        _animator.SetTrigger(_attackHash);
    }

    [UsedImplicitly]
    internal void HitDone()
    {
        //Logger.Log($"HitDone rising {Time.frameCount}");

        AttackDone?.Invoke();
    }

    internal void ShowGatheringBlocked(Sprite sprite)
    {
        if (!_mindCloud.activeSelf)
            _mindCloud.SetActive(true);

        _toolMindImage.sprite = sprite;
    }

    internal void ShowGatheringUnblocked()
    {
        if (_mindCloud.activeSelf)
            _mindCloud.SetActive(false);
    }

    internal void ShowCollect(ResourceType type, int count)
    {
        int newCount;
        Popup popup;
        if (_gatheredResourcesPopups.TryGetValue(type, out GatheredResourcesPopupInfo popupInfo))
        {
            newCount = count + popupInfo.Count;
            _gatheredResourcesPopups[type] = new GatheredResourcesPopupInfo() { Popup = popupInfo.Popup, Count = newCount };
            popup = popupInfo.Popup;
        }
        else
        {
            Vector3 spawnPosition = transform.position + _popupSpawnOffset;
            Vector3 toPosition = spawnPosition + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1, 0);
            newCount = count;
            popup = _popupFactory.Get(spawnPosition, Quaternion.identity);
            popup.Init();
            popup.ReadyToDetach += OnPopupReady;
            _gatheredResourcesPopups.Add(type, new GatheredResourcesPopupInfo() { Popup = popup, Count = newCount });

            popup.MoveTo(toPosition);
        }

        popup.ShowText($"+{newCount}");
    }

    internal void ShowSpeedUp()
    {
        var follower = Instantiate(_speedUpParticlesPrefab, transform.position, Quaternion.identity);
        follower.SetTarget(transform);
        _speedUpParticles = follower.GetComponent<ParticleSystem>();
        _speedUpParticles.Play();
    }

    internal void HideSpeedUp()
    {
        _speedUpParticles.Stop();
        Destroy(_speedUpParticles.gameObject);
    }

    internal void BeforeTeleport()
    {
        if (_speedUpParticles != null)
        {
            _speedUpParticles.Stop();
        }
    }

    internal void AfterTeleport()
    {
        if (_speedUpParticles != null)
        {
            _speedUpParticles.transform.position = transform.position;
            _speedUpParticles.Play();
        }
    }

    private void OnPopupReady(Popup popup)
    {
        popup.ReadyToDetach -= OnPopupReady;

        foreach (var pair in _gatheredResourcesPopups)
        {
            if (pair.Value.Popup == popup)
            {
                _gatheredResourcesPopups.Remove(pair.Key);
                return;
            }
        }
    }

    private struct GatheredResourcesPopupInfo
    {
        public Popup Popup;
        public int Count;
    }
}
