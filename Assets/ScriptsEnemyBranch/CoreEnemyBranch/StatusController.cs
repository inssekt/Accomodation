using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks anything that takes control away from an actor: a timed stun (falling over)
/// and control locks held by hazards (being sucked into a machine).
/// </summary>
[DisallowMultipleComponent]
public class StatusController : MonoBehaviour
{
    private readonly HashSet<object> _controlLocks = new HashSet<object>();
    private Rigidbody2D _body;
    private float _stunEndTime;
    private bool _wasStunned;
    private bool _wasLocked;

    // Capture state: colliders and renderers are switched off while held.
    private object _captureOwner;
    private Collider2D[] _heldColliders;
    private bool[] _heldColliderStates;
    private Renderer[] _hiddenRenderers;
    private bool[] _hiddenRendererStates;

    public bool IsStunned => Time.time < _stunEndTime;
    public bool IsCaptured => _captureOwner != null;
    public bool IsControlLocked => IsStunned || _controlLocks.Count > 0;
    public float StunTimeRemaining => Mathf.Max(0f, _stunEndTime - Time.time);

    public event Action<float> Stunned;            // duration in seconds
    public event Action StunEnded;
    public event Action<bool> ControlLockChanged;  // true = control lost

    private void Awake() => _body = GetComponent<Rigidbody2D>();

    private void Update()
    {
        // Self-healing: if a hazard is destroyed while holding us, let go.
        if (_captureOwner is UnityEngine.Object captor && captor == null) ReleaseCapture();
        if (_controlLocks.Count > 0) _controlLocks.RemoveWhere(IsDestroyedOwner);

        bool stunned = IsStunned;
        if (_wasStunned && !stunned) StunEnded?.Invoke();
        _wasStunned = stunned;

        RefreshLockState();
    }

    private void OnDisable() => ReleaseCapture();

    /// <summary>Takes control away for a number of seconds. Longer stuns win over shorter ones.</summary>
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        _stunEndTime = Mathf.Max(_stunEndTime, Time.time + duration);
        _wasStunned = true;
        Stunned?.Invoke(duration);
        RefreshLockState();
    }

    /// <summary>Open-ended lock. The owner must call RemoveControlLock when finished.</summary>
    public void AddControlLock(object owner)
    {
        if (owner == null) return;
        _controlLocks.Add(owner);
        RefreshLockState();
    }

    public void RemoveControlLock(object owner)
    {
        if (owner == null) return;
        _controlLocks.Remove(owner);
        RefreshLockState();
    }

    /// <summary>
    /// Holds the actor for a hazard: locks control, switches off colliders (so nothing can
    /// hit or block them) and optionally hides them. Always pair with EndCapture.
    /// </summary>
    public bool BeginCapture(object owner, bool hideVisuals)
    {
        if (owner == null || _captureOwner != null) return false;

        _captureOwner = owner;
        AddControlLock(owner);

        _heldColliders = GetComponentsInChildren<Collider2D>();
        _heldColliderStates = new bool[_heldColliders.Length];
        for (int i = 0; i < _heldColliders.Length; i++)
        {
            _heldColliderStates[i] = _heldColliders[i].enabled;
            _heldColliders[i].enabled = false;
        }

        if (hideVisuals)
        {
            _hiddenRenderers = GetComponentsInChildren<Renderer>();
            _hiddenRendererStates = new bool[_hiddenRenderers.Length];
            for (int i = 0; i < _hiddenRenderers.Length; i++)
            {
                _hiddenRendererStates[i] = _hiddenRenderers[i].enabled;
                _hiddenRenderers[i].enabled = false;
            }
        }

        return true;
    }

    public void EndCapture(object owner)
    {
        if (_captureOwner == null || !ReferenceEquals(_captureOwner, owner)) return;
        ReleaseCapture();
    }

    /// <summary>Moves the actor instantly. Hazards use this while holding them.</summary>
    public void TeleportTo(Vector2 position)
    {
        if (_body != null)
        {
            _body.position = position;
            _body.SetVelocity(Vector2.zero);
        }
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    /// <summary>Clears every effect. Call this when respawning.</summary>
    public void ClearAll()
    {
        _stunEndTime = 0f;
        _controlLocks.Clear();
        ReleaseCapture();
        RefreshLockState();
    }

    private void ReleaseCapture()
    {
        if (_captureOwner == null) return;

        if (_heldColliders != null)
        {
            for (int i = 0; i < _heldColliders.Length; i++)
                if (_heldColliders[i] != null) _heldColliders[i].enabled = _heldColliderStates[i];
        }

        if (_hiddenRenderers != null)
        {
            for (int i = 0; i < _hiddenRenderers.Length; i++)
                if (_hiddenRenderers[i] != null) _hiddenRenderers[i].enabled = _hiddenRendererStates[i];
        }

        _controlLocks.Remove(_captureOwner);
        _captureOwner = null;
        _heldColliders = null;
        _heldColliderStates = null;
        _hiddenRenderers = null;
        _hiddenRendererStates = null;
        RefreshLockState();
    }

    private void RefreshLockState()
    {
        bool locked = IsControlLocked;
        if (locked == _wasLocked) return;
        _wasLocked = locked;
        ControlLockChanged?.Invoke(locked);
    }

    private static bool IsDestroyedOwner(object owner)
    {
        return owner is UnityEngine.Object unityObject && unityObject == null;
    }
}