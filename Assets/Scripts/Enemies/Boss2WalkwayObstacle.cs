using UnityEngine;

// Patrol obstacle placed in walkways leading to mini computers.
// Moves back and forth along its forward axis to block the player's path.
// Speed and travel distance are set by Boss2StateManager when spawned.
public class Boss2WalkwayObstacle : MonoBehaviour
{
    [Tooltip("How far (meters) the obstacle travels before reversing direction")]
    public float travelDistance = 9f;

    [Tooltip("Movement speed in m/s — 0 keeps it stationary")]
    public float speed = 2f;

    [Tooltip("Seconds before this obstacle auto-destroys")]
    public float lifetime = 7f;

    private float _traveled;
    private float _timeAlive;
    private int   _dirSign = 1;

    void Update()
    {
        _timeAlive += Time.deltaTime;
        if (_timeAlive >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (speed <= 0f) return;

        float step          = speed * Time.deltaTime;
        transform.position += transform.forward * (_dirSign * step);
        _traveled          += step;

        if (_traveled >= travelDistance)
        {
            _traveled = 0f;
            _dirSign *= -1;
        }
    }
}
