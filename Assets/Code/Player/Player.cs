using System;
using UnityEngine;

internal class Player : MonoBehaviour, IDisposable
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Collider2D _collider2D;
    [SerializeField] private PlayerView _view;
    [SerializeField] private float _speed = 1;
    [SerializeField] private float _attackDelay = 5;

    [SerializeField] float _radius = 1f;
    [SerializeField] float _distanse = 1f;

    private float _attackTimer = 0;

    private Vector2 _direction = new Vector2(0, -1);

    private Collider2D[] _buffer;

    #region EDITOR_ONLY

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(GetOverlapCircleCenter(), _radius);
    }
#endif
    #endregion

    private void Start()
    {
        Construct();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void Construct()
    {
        _buffer = new Collider2D[10];

        _view.Construct();
        _view.AttackDone += OnHitDone;
    }

    public void Dispose()
    {
        _view.AttackDone -= OnHitDone;
    }

    private void FixedUpdate()
    {
        if (HasMoveInput())
        {
            float xMovement = Input.GetAxisRaw("Horizontal");
            float yMovement = Input.GetAxisRaw("Vertical");

            Vector2 direction = new Vector2(xMovement, yMovement).normalized;
            Vector2 startPos = _rb.position;
            Vector2 offset = direction * _speed * Time.fixedDeltaTime;

            _rb.MovePosition(startPos + offset);
            _view.PlayMove(direction.x, direction.y, direction.magnitude);

            _direction = direction;
        }
        else
        {
            _view.PlayMove(_direction.x, _direction.y, 0);
        }

        _attackTimer += Time.fixedDeltaTime;
        if (_attackTimer >= _attackDelay)
        {
            Vector2 center = GetOverlapCircleCenter();

            if (Physics2D.OverlapCircleNonAlloc(center, _radius, _buffer) > 0)
            {
                int sCount = 0;
                foreach (Collider2D collider in _buffer)
                {
                    if (collider == null)
                        continue;

                    if (collider.TryGetComponent<Player>(out _))
                        continue;

                    if (collider.TryGetComponent(out ResourceSource s))
                    {
                        sCount++;
                    }
                }

                if (sCount > 0)
                {
                    _attackTimer = 0;
                    _view.PlayAttack();
                }

                _buffer.Refresh();
            }
        }
    }

    private Vector2 GetOverlapCircleCenter()
    {
        Vector2 center = (Vector2)transform.position + _collider2D.offset;
        center += _direction * _distanse;
        return center;
    }

    private bool HasMoveInput()
    {
        return
            Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.DownArrow)
            || Input.GetKey(KeyCode.LeftArrow)
            || Input.GetKey(KeyCode.RightArrow)
            || Input.GetKey(KeyCode.UpArrow)
            ;
    }

    private void OnHitDone()
    {
        Vector2 center = GetOverlapCircleCenter();

        if (Physics2D.OverlapCircleNonAlloc(center, _radius, _buffer) > 0)
        {
            foreach (Collider2D collider in _buffer)
            {
                if (collider == null)
                    continue;

                if (collider.TryGetComponent<Player>(out _))
                    continue;

                if (collider.TryGetComponent(out ResourceSource s))
                {
                    s.Interact();
                }
            }

            _buffer.Refresh();
        }
    }
}
