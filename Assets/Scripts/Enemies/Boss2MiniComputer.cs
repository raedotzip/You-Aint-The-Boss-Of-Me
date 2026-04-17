using UnityEngine;

public class Boss2MiniComputer : MonoBehaviour
{
    public float maxHealth = 50f;

    [Tooltip("Drag the Boss2StateManager from the main computer here")]
    public Boss2StateManager boss2;

    private float _currentHealth;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0f)
        {
            boss2.OnMiniComputerDestroyed(this);
            Destroy(gameObject);
        }
    }
}
