using UnityEngine;

// Fires 3 massive laser bolts with a brief charge-up pause before each shot.
// The "on/off" feel comes from the charge pause followed by the giant projectile.
public class Boss3PulseLaser : EnemyBaseState
{
    private int   _pulseCount     = 3;
    private float _chargeDuration = 0.55f;  // pause before each shot (laser "charging up")
    private float _bulletSpeed    = 7f;
    private float _bulletDamage   = 25f;
    private float _bulletScale    = 4f;
    private float _bulletLifetime = 5f;

    private int   _pulsesLeft;
    private float _timer;
    private bool  _waitingForCharge;
    private bool  _done;

    public override void EnterState(EnemyStateManager state)
    {
        _pulsesLeft       = _pulseCount;
        _timer            = 0f;
        _waitingForCharge = true;
        _done             = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        if (_waitingForCharge && _timer >= _chargeDuration)
        {
            _waitingForCharge = false;
            _timer            = 0f;
            FirePulse(state);
            _pulsesLeft--;

            if (_pulsesLeft <= 0)
            {
                _done = true;
                ((Boss3StateManager)state).TransitionToNextState();
            }
        }
        else if (!_waitingForCharge && _timer >= 0.15f)
        {
            // Short gap after the shot before next charge begins
            _waitingForCharge = true;
            _timer            = 0f;
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void FirePulse(EnemyStateManager state)
    {
        Vector3 toPlayer = (state.player.position - state.transform.position).normalized;

        Bullet b = new Bullet
        {
            position        = state.transform.position,
            direction       = toPlayer,
            speed           = _bulletSpeed,
            damage          = _bulletDamage,
            maxLifetime     = _bulletLifetime,
            collisionRadius = _bulletScale * 0.45f,
            canBeParried    = true,
            destroyOnParry  = true,
            movementType    = BulletMovementType.Straight,
            visualPrefab    = state.bulletData.groundSlamBulletPrefab,
            scale           = _bulletScale,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}
