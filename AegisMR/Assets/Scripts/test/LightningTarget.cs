using UnityEngine;

/// <summary>
/// Example target that can be damaged by lightning
/// </summary>
public class LightningTarget : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Visual Feedback")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;
    
    private Renderer targetRenderer;
    private Color originalColor;
    private float flashTimer = 0f;
    
    void Start()
    {
        currentHealth = maxHealth;
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            originalColor = targetRenderer.material.color;
        }
    }
    
    void Update()
    {
        // Handle damage flash
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0 && targetRenderer != null)
            {
                targetRenderer.material.color = originalColor;
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Visual feedback
        if (targetRenderer != null)
        {
            targetRenderer.material.color = damageColor;
            flashTimer = flashDuration;
        }
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log($"{gameObject.name} was destroyed by lightning!");
        Destroy(gameObject);
    }
}
