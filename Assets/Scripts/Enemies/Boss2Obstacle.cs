using UnityEngine;

// Attach to the obstacle prefab. The attack state sets moveDirection and speed before spawning.
public class Boss2Obstacle : MonoBehaviour
{
    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public float   speed;
    [HideInInspector] public float   lifetime;

    void Update()
    {
        transform.position += moveDirection * (speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }
}
