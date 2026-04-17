using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }
    [SerializeField] private float hitCooldown = 0.5f; // seconds

    
    // Add a reference to the UI script
    public HealthBarUI playerHealthBar;
    private float lastHitTime = -Mathf.Infinity;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Initialize the bar on start
        if (playerHealthBar != null)
            playerHealthBar.UpdateHealthPercentage(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        // Check cooldown
        if (Time.time < lastHitTime + hitCooldown)
            return;
    
        lastHitTime = Time.time;
    
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        
        // Update UI
        if (playerHealthBar != null)
            playerHealthBar.UpdateHealthPercentage(currentHealth, maxHealth);
    
        if (currentHealth <= 0f)
            OnDeath();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // Update UI
        if (playerHealthBar != null)
            playerHealthBar.UpdateHealthPercentage(currentHealth, maxHealth);
    }

    private void OnDeath()
    {
        // Debug.Log("Player died");
    }
}